using AskMe.Application.Common.Interfaces;
using AskMe.Infrastructure.Email;
using AskMe.Infrastructure.Identity;
using AskMe.Infrastructure.Persistence;
using AskMe.Infrastructure.Services;
using AskMe.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AskMe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.")));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddSingleton<IDateTime, DateTimeService>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IOtpHasher, BCryptOtpHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        services.Configure<GoogleOptions>(config.GetSection(GoogleOptions.SectionName));
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();

        services.Configure<FacebookOptions>(config.GetSection(FacebookOptions.SectionName));
        services.AddHttpClient<IFacebookTokenValidator, FacebookTokenValidator>();

        // Cloudflare R2 kept configured but unregistered as IFileStorageService -
        // swap the two lines below back to it when moving off Postgres-backed
        // image storage for production.
        services.Configure<R2Options>(config.GetSection(R2Options.SectionName));

        services.Configure<PostgresImageStorageOptions>(config.GetSection(PostgresImageStorageOptions.SectionName));
        // Scoped (not Singleton, unlike the R2 client) because this implementation
        // depends on the request-scoped ApplicationDbContext.
        services.AddScoped<IFileStorageService, PostgresImageStorageService>();

        return services;
    }
}
