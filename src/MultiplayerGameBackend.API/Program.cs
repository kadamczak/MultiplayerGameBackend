using MultiplayerGameBackend.API.Extensions;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddPresentation();


    var app = builder.Build();

    app.UseSerilogRequestLogging();
    
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }