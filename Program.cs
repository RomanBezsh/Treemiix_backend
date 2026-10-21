using System.Text;
using System.Globalization;
using System.IO;
using System.Reflection;
using CloneAmazonBack.Data;
using CloneAmazonBack.Middleware;
using CloneAmazonBack.Services;
using CloneAmazonBack.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Asp.Versioning;
using System.Threading.RateLimiting;

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
        .ReadFrom.Configuration(context.Configuration)
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

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // CORS - restricted origins from configuration
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000", "http://localhost:5173"];
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
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

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
            .AddRedis(redisOptions.ConnectionString);
    }
    else
    {
        // Null cache service when Redis is disabled
        builder.Services.AddScoped<ICacheService, NullCacheService>();
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
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
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
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = globalPermitLimit,
                    Window = TimeSpan.FromSeconds(globalWindowSeconds),
                    QueueLimit = 0
                }));
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

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    var app = builder.Build();

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

    // Security headers middleware (CSP, HSTS, etc.)
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
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
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Health checks endpoint
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

    // API Version prefix middleware - add /v1 prefix if missing for API routes
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path) && path.StartsWith("/api/") && !path.StartsWith("/api/v"))
        {
            context.Request.Path = "/api/v1" + path.Substring(4);
        }
        await next();
    });

    app.UseAuthorization();
    app.MapControllers();

    // Health checks endpoint
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
        await DbSeeder.SeedRolesAsync(dbContext, configuration);
        // Принудительное заполнение товарами
        await DbSeeder.SeedProductsAsync(dbContext);
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