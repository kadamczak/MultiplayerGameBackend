using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Middleware;

public class ErrorHandlingMiddleware(
    ILogger<ErrorHandlingMiddleware> logger,
    ILocalizationService localizationService) : IMiddleware
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
            
            // Message is already localized by the service that threw the exception
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = notFound.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ConflictException conflict)
        {
            logger.LogWarning(conflict.Message);
            
            // If Errors is null, use the exception message directly (already localized)
            if (conflict.Errors == null)
            {
                await WriteProblemAsync(context, new ProblemDetails
                {
                    Title = conflict.Message,
                    Status = StatusCodes.Status409Conflict
                });
                return;
            }
    
            await WriteValidationProblemAsync(context, new ValidationProblemDetails(conflict.Errors)
            {
                Title = localizationService.GetString("Error.OneOrMoreConflicts"),
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (UnprocessableEntityException unprocessableEntity)
        {
            logger.LogWarning(unprocessableEntity.Message);
            
            if (unprocessableEntity.Errors != null)
            {
                await WriteValidationProblemAsync(context, new ValidationProblemDetails(unprocessableEntity.Errors)
                {
                    Title = localizationService.GetString("Error.OneOrMoreErrors"),
                    Status = StatusCodes.Status422UnprocessableEntity
                });
            }
            else
            {
                await WriteProblemAsync(context, new ProblemDetails
                {
                    Title = unprocessableEntity.Message,
                    Status = StatusCodes.Status422UnprocessableEntity
                });
            }
        }
        catch (ForbidException forbid)
        {
            logger.LogWarning(forbid.Message);
            
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = forbid.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (PayloadTooLargeException payloadTooLarge)
        {
            logger.LogWarning(payloadTooLarge.Message);
            
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = payloadTooLarge.Message,
                Status = StatusCodes.Status413PayloadTooLarge
            });
        }
        catch (UnsupportedMediaType unsupportedMediaType)
        {
            logger.LogWarning(unsupportedMediaType.Message);
            
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = unsupportedMediaType.Message,
                Status = StatusCodes.Status415UnsupportedMediaType
            });
        }
        catch (BadRequest badRequest)
        {
            logger.LogWarning(badRequest.Message);
            
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = badRequest.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = localizationService.GetString("Error.SomethingWentWrong"),
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
