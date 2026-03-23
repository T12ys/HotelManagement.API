using HotelWebApplication.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithValidation()
    .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies())
    .AddDatabase(builder.Configuration)
    .AddApplicationServices()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorizationPolicies()
    .AddFrontendCors()
    .AddSwaggerWithJwt();

var app = builder.Build();

app.UseExceptionHandling()
   .UseSwaggerDocs()
   .UseSwaggerAutoOpen()
   .UseStaticFiles()
   .UseCors("AllowFrontend")
   .UseAuthentication()
   .UseAuthorization();

app.MapControllers();

await app.SeedDatabaseAsync();

app.Run();