using MultiplayerGameBackend.API.Extensions;
using MultiplayerGameBackend.API.Middleware;
using Serilog;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.Common.Validators;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Infrastructure.Extensions;
using MultiplayerGameBackend.Infrastructure.Seeders;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddPresentation();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();
    
    // Initialize validator localizer
    using (var scope = app.Services.CreateScope())
    {
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        ValidatorLocalizer.Initialize(localizationService);
    }
    
    // Only run seeder if not in testing environment
    if (!app.Environment.IsEnvironment("Testing"))
    {
        var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IMultiplayerGameSeeder>();
        await seeder.Seed();
    }
    
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestTimeLoggingMiddleware>();

    app.UseSerilogRequestLogging();
    app.UseRequestLocalization();
    
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
    
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=86400");
        }
    });
    
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