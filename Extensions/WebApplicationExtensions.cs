using System.Diagnostics;
using HotelWebApplication.Data;
using HotelWebApplication.Middlewares;

namespace HotelWebApplication.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplication"/> that configure
/// the HTTP middleware pipeline in named, focused steps.
/// Called from <c>Program.cs</c> to keep the entry point clean.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Registers exception handling middleware.
    /// In development the built-in developer exception page is added first;
    /// the custom <see cref="GlobalExceptionMiddleware"/> runs in all environments.
    /// </summary>
    public static WebApplication UseExceptionHandling(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseMiddleware<GlobalExceptionMiddleware>();

        return app;
    }

    /// <summary>
    /// Registers Swagger UI middleware.
    /// The UI is available at <c>/swagger</c> in all environments.
    /// </summary>
    public static WebApplication UseSwaggerDocs(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel API V1");
            c.RoutePrefix = "swagger";
        });

        return app;
    }

    /// <summary>
    /// In development, opens the Swagger UI in the default browser
    /// automatically when the application starts.
    /// Silently ignored if the browser cannot be launched (e.g. in CI/CD).
    /// </summary>
    public static WebApplication UseSwaggerAutoOpen(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5207/swagger/index.html",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore if browser cannot be opened (e.g. in CI/CD)
            }
        });

        return app;
    }

    /// <summary>
    /// Applies migrations and seeds the database with initial data
    /// (admin user, room types, rooms, tags, price rules, sample reservations).
    /// Safe to call on every startup — skips entities that already exist.
    /// </summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        await DatabaseSeeder.SeedAsync(app.Services);
    }
}