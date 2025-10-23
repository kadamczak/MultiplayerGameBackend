using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Middleware;

public class ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger) : IMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException notFound)
        {
            logger.LogWarning(notFound.Message);
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = notFound.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ConflictException conflict)
        {
            logger.LogWarning(conflict.Message);
    
            await WriteValidationProblemAsync(context, new ValidationProblemDetails(conflict.Errors!)
            {
                Title = "One or more conflicts occurred.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ForbidException forbid)
        {
            logger.LogWarning(forbid.Message);
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = "Access forbidden",
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = "Something went wrong.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    private static Task WriteProblemAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        var json = JsonSerializer.Serialize(problem, JsonOptions);
        return context.Response.WriteAsync(json);
    }
    
    private static Task WriteValidationProblemAsync(HttpContext context, ValidationProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        var json = JsonSerializer.Serialize(problem, JsonOptions);
        return context.Response.WriteAsync(json);
    }
}
