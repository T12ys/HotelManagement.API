using FluentValidation;
using FluentValidation.AspNetCore;
using HotelWebApplication.BackgroundJobs;
using HotelWebApplication.Data;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace HotelWebApplication.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> that group
/// related service registrations into focused, reusable methods.
/// Called from <c>Program.cs</c> to keep the entry point clean.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MVC controllers with JSON enum serialization
    /// and FluentValidation auto-validation.
    /// </summary>
    public static IServiceCollection AddControllersWithValidation(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddFluentValidationAutoValidation();

        return services;
    }

    /// <summary>
    /// Registers the SQL Server <see cref="HotelDbContext"/> using the
    /// <c>DefaultConnection</c> connection string from configuration.
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<Data.HotelDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        return services;
    }

    /// <summary>
    /// Registers all application-level scoped services and the
    /// <see cref="ReservationCompletionJob"/> background job.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
        services.AddScoped<IRoomTypeService, RoomTypeService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IPriceRuleService, PriceRuleService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IUserService, UserService>();

        services.AddHostedService<ReservationCompletionJob>();

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication using <c>Jwt:*</c> settings from configuration.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        services
            .AddAuthentication(options =>
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
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
                };
            });

        return services;
    }

    /// <summary>
    /// Registers all role-based authorization policies used across controllers.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Room type management
            options.AddPolicy("RoomTypeWrite", p => p.RequireRole("Admin"));
            options.AddPolicy("RoomTypeDelete", p => p.RequireRole("Admin"));

            // Room management
            options.AddPolicy("RoomWrite", p => p.RequireRole("Admin", "Moderator"));
            options.AddPolicy("RoomDelete", p => p.RequireRole("Admin"));

            // Photo management
            options.AddPolicy("PhotoManagement", p => p.RequireRole("Admin", "Moderator"));

            // Tag management
            options.AddPolicy("TagWrite", p => p.RequireRole("Admin", "Moderator"));
            options.AddPolicy("TagDelete", p => p.RequireRole("Admin"));

            // Price rule management
            options.AddPolicy("PriceRuleWrite", p => p.RequireRole("Admin", "Moderator"));
            options.AddPolicy("PriceRuleDelete", p => p.RequireRole("Admin"));

            // Reservation management
            options.AddPolicy("ReservationRead", p => p.RequireRole("Admin", "Moderator"));
            options.AddPolicy("ReservationWrite", p => p.RequireRole("Admin", "Moderator"));
            options.AddPolicy("ReservationCancel", p => p.RequireRole("Admin", "Moderator"));

            // User management
            options.AddPolicy("UserRead", p => p.RequireRole("Admin", "Moderator"));
            options.AddPolicy("UserRoleWrite", p => p.RequireRole("Admin"));
        });

        return services;
    }

    /// <summary>
    /// Configures CORS to allow requests from the React frontend at <c>http://localhost:5173</c>.
    /// </summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        });

        return services;
    }

    /// <summary>
    /// Registers Swagger/OpenAPI with JWT Bearer security definition
    /// and XML documentation comments.
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Hotel Booking API",
                Version = "v1",
                Description = "REST API for hotel room booking management. " +
                              "Supports guest reservations, admin/moderator panel, " +
                              "price rules, audit logging, and mock payment processing."
            });

            // Load XML documentation for controller/action summaries
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT access token. Example: Bearer {token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}