using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Auditing;
using PullUp.Infrastructure.Auditing;
using PullUp.Infrastructure.Notifications;
using PullUp.Infrastructure.Persistence;
using PullUp.Infrastructure.Security;
using System.Text;

namespace PullUp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AppDb")
            ?? throw new InvalidOperationException("Missing connection string 'AppDb'.");

        // SQL Server (SQLEXPRESS) is the canonical local + production provider per the
        // implementation guidance. The Sqlite branch exists only for integration tests
        // which set "Database:Provider" to "Sqlite" and point AppDb at an in-memory store.
        var provider = configuration["Database:Provider"] ?? "SqlServer";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAuditLogger, AuditLogger>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<INotificationSender, LoggingNotificationSender>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenHasher, HmacTokenHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddMemoryCache();
        services.AddSingleton<ISignInRateLimiter, SignInRateLimiter>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing Jwt configuration section.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
                };
            });

        services.AddAuthorization();

        return services;
    }
}
