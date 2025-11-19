using MultiplayerGameBackend.API.Extensions;
using MultiplayerGameBackend.API.Middleware;
using Serilog;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Infrastructure.Extensions;
using MultiplayerGameBackend.Infrastructure.Seeders;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddPresentation();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();
    
    var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IMultiplayerGameSeeder>();

    await seeder.Seed();
    
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestTimeLoggingMiddleware>();

    app.UseSerilogRequestLogging();
    
    if (app.Environment.IsDevelopment())
    {
        // Enable CORS for local React dev server in development
        app.UseCors("LocalReact");
        
        // Enable Swagger
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Multiplayer Game Backend API v1");
            options.RoutePrefix = "swagger"; // Access at /swagger
        });
    }
    
    app.UseStaticFiles();
    app.UseHttpsRedirection();
    
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    
    Log.Information("Application started");
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