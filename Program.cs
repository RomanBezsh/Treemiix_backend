using System.Text;
using System.Globalization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using CloneAmazonBack.Data;
using CloneAmazonBack.Middleware;
using CloneAmazonBack.Services;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using StackExchange.Redis;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// Serilog bootstrap logger (before host builds)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Services(services)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Application", "CloneAmazonBack")
        .WriteTo.Console(new CompactJsonFormatter())
        .WriteTo.File(
            path: "logs/log-.json",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            formatter: new CompactJsonFormatter(),
            flushToDiskInterval: TimeSpan.FromSeconds(1))
        .WriteTo.File(
            path: "logs/errors/log-.json",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 90,
            formatter: new CompactJsonFormatter(),
            restrictedToMinimumLevel: LogEventLevel.Error,
            flushToDiskInterval: TimeSpan.FromSeconds(1)));

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. " +
            "Set it in appsettings.json or the ConnectionStrings__DefaultConnection environment variable.");
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3)));

    // CORS - restricted origins from configuration. The deployed frontend is part of the
    // fallback list so a missing/typoed Cors:AllowedOrigins value cannot break the SPA.
    var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    var corsOrigins = configuredCorsOrigins is { Length: > 0 }
        ? configuredCorsOrigins
        : ["http://localhost:3000", "http://localhost:5173", "https://treemiix.vercel.app"];
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, tags: ["ready"]);

    // Redis Cache
    var redisOptions = builder.Configuration.GetSection("Redis").Get<RedisCacheOptions>() ?? new RedisCacheOptions();
    if (redisOptions.Enabled && !string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = ConfigurationOptions.Parse(redisOptions.ConnectionString, true);
            config.AbortOnConnectFail = false;
            config.ConnectRetry = 3;
            config.ConnectTimeout = 5000;
            return ConnectionMultiplexer.Connect(config);
        });
        builder.Services.Configure<RedisCacheOptions>(builder.Configuration.GetSection("Redis"));
        builder.Services.AddScoped<ICacheService, CacheService>();
        
        // Add Redis health check
        builder.Services.AddHealthChecks()
            .AddRedis(redisOptions.ConnectionString, tags: ["ready"]);
    }
    else
    {
        // Null cache service when Redis is disabled
        builder.Services.AddScoped<ICacheService, NullCacheService>();
    }

    // Email delivery (confirmation codes and password reset).
    var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
    builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
    builder.Services.AddHttpClient("email", client => client.Timeout = TimeSpan.FromSeconds(30));

    var useResend = string.Equals(
        emailOptions.Provider,
        "Resend",
        StringComparison.OrdinalIgnoreCase);

    var emailConfigured =
        emailOptions.Enabled &&
        !string.IsNullOrWhiteSpace(emailOptions.FromAddress) &&
        (useResend
            ? !string.IsNullOrWhiteSpace(emailOptions.ApiKey)
            : !string.IsNullOrWhiteSpace(emailOptions.Host));

    if (emailConfigured)
    {
        if (useResend)
            builder.Services.AddScoped<IEmailSender, ResendEmailSender>();
        else
            builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
    }
    else if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
    }
    else
    {
        builder.Services.AddScoped<IEmailSender, UnconfiguredEmailSender>();

        if (!builder.Environment.IsDevelopment())
        {
            Log.Warning(
                "Email delivery is not configured. Email confirmation and password reset emails will not be sent.");
        }
    }

    if (builder.Configuration.GetValue("Auth:RequireEmailConfirmation", false) && !emailConfigured)
    {
        throw new InvalidOperationException(
            "Auth:RequireEmailConfirmation is enabled but Email:* is not configured. " +
            "Users would not be able to confirm their email.");
    }

    // Security: Validate JWT key length at startup
    var jwtKeyConfig = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKeyConfig) || jwtKeyConfig.Length < 32)
    {
        throw new InvalidOperationException("JWT Key must be at least 32 characters long. Configure 'Jwt:Key' in appsettings or environment variables.");
    }

    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var jwtKey = Encoding.UTF8.GetBytes(jwtKeyConfig!);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        var rateLimiting = builder.Configuration.GetSection("RateLimiting");

        var authPermitLimit = rateLimiting.GetValue("AuthPermitLimit", 10);
        var authWindowSeconds = rateLimiting.GetValue("AuthWindowSeconds", 60);
        var globalPermitLimit = rateLimiting.GetValue("GlobalPermitLimit", 100);
        var globalWindowSeconds = rateLimiting.GetValue("GlobalWindowSeconds", 60);

        options.AddFixedWindowLimiter("auth", limiterOptions =>
        {
            limiterOptions.PermitLimit = authPermitLimit;
            limiterOptions.Window = TimeSpan.FromSeconds(authWindowSeconds);
            limiterOptions.QueueLimit = 0;
        });

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
            {
                // Media responses are immutable and cached by the browser / image optimizer,
                // and a single catalog page can legitimately request hundreds of them.
                // A shared 100/min bucket would turn images into 429s, so they are not limited.
                if (httpContext.Request.Path.StartsWithSegments("/media") ||
                    httpContext.Request.Path.StartsWithSegments("/uploads"))
                    return RateLimitPartition.GetNoLimiter("media");

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermitLimit,
                        Window = TimeSpan.FromSeconds(globalWindowSeconds),
                        QueueLimit = 0
                    });
            });
    });

    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<ISellerService, SellerService>();
    builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
    builder.Services.AddScoped<IGiftCardService, GiftCardService>();
    builder.Services.AddScoped<ICartItemService, CartItemService>();
    builder.Services.AddScoped<IOrderItemService, OrderItemService>();
    builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
    builder.Services.AddScoped<IProductQuestionService, ProductQuestionService>();
    builder.Services.AddScoped<IProductAnswerService, ProductAnswerService>();
    builder.Services.AddScoped<IQuestionVoteService, QuestionVoteService>();
    builder.Services.AddScoped<IUserAddressService, UserAddressService>();
    builder.Services.AddScoped<IUserProfileService, UserProfileService>();
    builder.Services.AddScoped<IUserRoleService, UserRoleService>();
    builder.Services.AddScoped<IProductGalleryService, ProductGalleryService>();
    builder.Services.AddScoped<IProductVideoService, ProductVideoService>();
    builder.Services.AddScoped<IProductAttributeValueService, ProductAttributeValueService>();
    builder.Services.AddScoped<IPromoCodeProductService, PromoCodeProductService>();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // IHttpClientFactory is required by UploadController (SSRF-safe media import).
    builder.Services.AddHttpClient("media-import", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            // Redirects could be used to bypass the SSRF host check.
            AllowAutoRedirect = false
        });

    // Media is stored in PostgreSQL, so the request body limit matches the 5 MB
    // per-file cap enforced by UploadController (fail fast instead of buffering 200 MB).
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 6L * 1024 * 1024;
    });

    // API Versioning (version is optional; sent via the "api-version" header
    // so that existing /api/... routes stay backwards compatible).
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new HeaderApiVersionReader("api-version");
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
    });

    var app = builder.Build();

    // Make configuration problems visible in the very first log lines.
    app.Logger.LogInformation(
        "Environment: {Environment}; CORS allowed origins: {Origins}",
        app.Environment.EnvironmentName,
        string.Join(", ", corsOrigins));

    // Serilog request logging (adds correlation ID, timing, etc.)
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            }
        };
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Respect X-Forwarded-* headers coming from reverse proxies (Render, nginx, Docker).
    // Without this the rate limiter sees the proxy IP and puts every client into one partition.
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);

    // Security headers middleware (CSP, HSTS, etc.)
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // CSP - restrictive but allows Swagger UI in Development
        var csp = app.Environment.IsDevelopment()
            ? "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https:;"
            : "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
        context.Response.Headers["Content-Security-Policy"] = csp;

        if (!app.Environment.IsDevelopment())
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        await next();
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseHttpsRedirection();

    // Serve uploaded media from wwwroot (/media, /uploads).
    app.UseStaticFiles();

    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health checks endpoints
    // PaaS health checks (Render included) probe "/" by default; answer 200 instead of 404.
    app.MapGet("/", () => Results.Ok(new { service = "CloneAmazonBack", status = "ok" }));
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (configuration.GetValue("Database:MigrateOnStartup", false))
        {
            await dbContext.Database.MigrateAsync();
        }

        await DbSeeder.SeedRolesAsync(dbContext, configuration);

        // Demo catalog data must never be written to a production database.
        if (app.Environment.IsDevelopment() &&
            configuration.GetValue("Database:SeedDemoData", true))
        {
            await DbSeeder.SeedDemoDataAsync(dbContext);
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}