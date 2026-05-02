using BusinessLogic.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Mapping;
using Presentation.Models;
using Presentation.Services;
using Presentation.ViewModels.Components;
using Presentation.ViewModels.Pages;

namespace Presentation
{
    public static class PresentationConfiguration
    {
        public static IServiceCollection AddOmniPortPresentation(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAutoMapper(cfg =>
                cfg.AddProfile<OmniPortMappingProfile>());

            services.Configure<UploadLimits>(
                configuration.GetSection("UploadLimits"));

            services.AddScoped<ITemplateManager, TemplateManager>();
            services.AddScoped<ITransformationManager, TransformationManager>();
            services.AddScoped<ITransformationExecutionService, TransformationExecutor>();

            services.AddScoped<LoginViewModel>();
            services.AddScoped<IndexViewModel>();
            services.AddScoped<ErrorViewModel>();
            services.AddScoped<FieldRowEditorViewModel>();
            services.AddScoped<TemplateEditorViewModel>();
            services.AddScoped<JoinTemplatesViewModel>();
            services.AddScoped<TransformationViewModel>();

            services.AddSingleton<IAppSyncContext, AppSyncContext>();
            services.AddSingleton<ISourceFingerprintStore, InMemorySourceFingerprintStore>();
            services.AddHostedService<WatchedHashSyncService>();

            return services;
        }
    }
}
