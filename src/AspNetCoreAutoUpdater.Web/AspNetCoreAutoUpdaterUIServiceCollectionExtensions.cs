using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreAutoUpdater.Web
{
    public static class AspNetCoreAutoUpdaterUIServiceCollectionExtensions
    {
        public static void AddAspNetCoreAutoUpdaterUI(this IServiceCollection services)
        {
            services.ConfigureOptions(typeof(AspNetCoreAutoUpdaterUIConfigureOptions));
        }
    }
}
