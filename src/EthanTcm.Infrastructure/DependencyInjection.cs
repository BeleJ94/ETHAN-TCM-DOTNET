using EthanTcm.Application.Abstractions;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EthanTcm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EthanTcmDatabase")
            ?? throw new InvalidOperationException("Connection string 'EthanTcmDatabase' is missing.");

        services.Configure<NotificationOptions>(options =>
        {
            var section = configuration.GetSection(NotificationOptions.SectionName);
            options.DryRun = bool.TryParse(section["DryRun"], out var dryRun) ? dryRun : options.DryRun;
            options.DailyCron = section["DailyCron"] ?? options.DailyCron;
            options.Smtp.Host = section["Smtp:Host"] ?? options.Smtp.Host;
            options.Smtp.Port = int.TryParse(section["Smtp:Port"], out var port) ? port : options.Smtp.Port;
            options.Smtp.EnableSsl = bool.TryParse(section["Smtp:EnableSsl"], out var enableSsl) ? enableSsl : options.Smtp.EnableSsl;
            options.Smtp.UserName = section["Smtp:UserName"];
            options.Smtp.Password = section["Smtp:Password"];
            options.Smtp.From = section["Smtp:From"] ?? options.Smtp.From;
        });

        services.AddDbContext<EthanTcmDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(EthanTcmDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure();
            }));

        services.TryAddScoped<ICurrentUser, SystemCurrentUser>();
        services.TryAddScoped<IActiveDirectoryUserSyncService, ActiveDirectoryUserSyncService>();
        services.TryAddScoped<IAccessAdministrationService, AccessAdministrationService>();
        services.TryAddScoped<ITaxMatrixImportService, TaxMatrixImportService>();
        services.TryAddScoped<ITaxMatrixImporter, TaxMatrixImportService>();
        services.TryAddScoped<ITaxObligationReferentialService, TaxObligationReferentialService>();
        services.TryAddScoped<ITaxDeclarationGenerationService, TaxDeclarationGenerationService>();
        services.TryAddScoped<ITaxDeclarationWorkflowService, TaxDeclarationWorkflowService>();
        services.TryAddScoped<ITaxDocumentService, TaxDocumentService>();
        services.TryAddScoped<IDashboardService, DashboardService>();
        services.TryAddScoped<IAuditRequestContext, EmptyAuditRequestContext>();
        services.TryAddScoped<IAuditService, AuditService>();
        services.TryAddScoped<INotificationEmailSender, SmtpNotificationEmailSender>();
        services.TryAddScoped<IDeadlineReminderService, DeadlineReminderService>();
        services.TryAddScoped<IInitialTaxObligationSeeder, InitialTaxObligationSeeder>();
        services.TryAddScoped<ITaxCatalogSynchronizationService, TaxCatalogSynchronizationService>();
        services.TryAddScoped<ICorrespondenceService, CorrespondenceService>();
        services.TryAddScoped<ICorrespondenceOrganizationService, CorrespondenceOrganizationService>();
        services.TryAddScoped<ICorrespondenceActionService, CorrespondenceActionService>();
        services.TryAddScoped<ICorrespondenceActionReminderService, CorrespondenceActionReminderService>();

        return services;
    }
}
