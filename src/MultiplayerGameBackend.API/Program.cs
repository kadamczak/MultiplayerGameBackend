using MultiplayerGameBackend.API.Extensions;
using MultiplayerGameBackend.API.Middleware;
using Serilog;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Infrastructure.Extensions;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddPresentation();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();
    
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestTimeLoggingMiddleware>();

    app.UseSerilogRequestLogging();
    
    if (app.Environment.IsDevelopment())
    {
        // swagger?
    }
    
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }