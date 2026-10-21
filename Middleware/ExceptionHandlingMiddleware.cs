using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // The response may already be in flight (e.g. a failure while streaming
        // a file); we cannot change the status code at that point.
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response already started, unable to write error response");
            return;
        }

        var (statusCode, title, detail) = ex switch
        {
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "You are not allowed to perform this operation."),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not found",
                "The requested resource was not found."),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "The resource was modified by another request. Please retry."),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "The operation conflicts with existing data."),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                "The request could not be processed."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred. Please try again later.")
        };

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var response = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(response, JsonOptions);
    }
}
