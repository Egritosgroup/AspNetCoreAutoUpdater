using System;
using Microsoft.Extensions.DependencyInjection;
using AspNetCoreAutoUpdater.HttpDownloader;

namespace AspNetCoreAutoUpdater
{
    public static class UpdaterExtensions
    {
        public static IServiceCollection UseHttpUpdater(this IServiceCollection services, Action<HttpAppUpdaterOptions> setupAction)
        {
            var opts = new HttpAppUpdaterOptions();
            setupAction.Invoke(opts);

            return services.AddSingleton<IAppUpdaterService, HttpAppUpdaterService>()
                                .AddSingleton<HttpAppUpdaterOptions>(opts);
        }
    }
}
