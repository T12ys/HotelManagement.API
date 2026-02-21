using FluentValidation;
using FluentValidation.AspNetCore;
using HotelWebApplication.Data;
using HotelWebApplication.Mappings;
using HotelWebApplication.Middlewares;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add controllers + FluentValidation
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ITagService, TagService>();

// Swagger (Swashbuckle 10.x pattern)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Информация об API
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel API",
        Version = "v1",
        Description = "API для управления отелем"
    });

    // XML комментарии
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    // JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new List<string>()
    }
});
});


builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RoomTypeWrite", p => p.RequireRole("Admin"));
    options.AddPolicy("RoomTypeDelete", p => p.RequireRole("Admin"));
    options.AddPolicy("PhotoManagement", p => p.RequireRole("Admin", "Moderator"));
    options.AddPolicy("RoomWrite", p => p.RequireRole("Admin", "Moderator"));
    options.AddPolicy("RoomDelete", p => p.RequireRole("Admin"));
    options.AddPolicy("TagWrite", p => p.RequireRole("Admin", "Moderator"));
    options.AddPolicy("TagDelete", p => p.RequireRole("Admin"));
});


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") //адрес React
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware order
app.UseDeveloperExceptionPage();
//app.UseGlobalExceptionMiddleware(); // our middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel API V1");
    c.RoutePrefix = "swagger"; // доступно на /swagger
});

var swaggerUrl = "http://localhost:5207/swagger/index.html";

//не для продакшена
app.Lifetime.ApplicationStarted.Register(() =>
{
    var psi = new ProcessStartInfo
    {
        FileName = swaggerUrl,
        UseShellExecute = true
    };
    Process.Start(psi);
});


app.UseCors("AllowFrontend");

// enable static files to serve /uploads
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await RoleSeeder.SeedAdminAsync(app.Services);

app.Run();
