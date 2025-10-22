using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Middleware;

public class ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (NotFoundException notFound)
        {
            logger.LogWarning(notFound.Message);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync(notFound.Message);
        }
        catch (ConflictException conflict)
        {
            logger.LogWarning(conflict.Message);
            context.Response.StatusCode = 409;
            await context.Response.WriteAsync(conflict.Message);
        }
        catch (ForbidException forbid)
        {
            logger.LogWarning(forbid.Message);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access forbidden");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Something went wrong");
        }
    }
}
