using System.Net;
using System.Text.Json;
using FluentValidation;
using HRMS.Application.Common;
using HRMS.Application.Common.Exceptions;

namespace HRMS.API.Middleware;

/// <summary>
/// Catches every exception thrown below it in the pipeline and turns it into a
/// consistent ApiResponse&lt;object&gt; JSON body with the right status code, so
/// controllers never need try/catch blocks.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                validationEx.Errors.Select(e => e.ErrorMessage).ToList()),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound, notFoundEx.Message, null),

            BadRequestException badRequestEx => (
                HttpStatusCode.BadRequest, badRequestEx.Message, null),

            ConflictException conflictEx => (
                HttpStatusCode.Conflict, conflictEx.Message, null),

            UnauthorizedAppException unauthorizedEx => (
                HttpStatusCode.Unauthorized, unauthorizedEx.Message, null),

            ForbiddenAppException forbiddenEx => (
                HttpStatusCode.Forbidden, forbiddenEx.Message, null),

            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (List<string>?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("{ExceptionType} while processing {Method} {Path}: {Message}",
                exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.FailResponse(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
