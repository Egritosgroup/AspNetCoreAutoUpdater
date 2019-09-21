using System;
using Microsoft.Extensions.DependencyInjection;
using AspNetCoreAutoUpdater.FtpDownloader;

namespace AspNetCoreAutoUpdater
{
    public static class UpdaterExtensions
    {
        public static IServiceCollection UseFtpUpdater(this IServiceCollection services, Action<FtpAppUpdaterOptions> setupAction)
        {
            var opts = new FtpAppUpdaterOptions();
            setupAction.Invoke(opts);

            return services.AddSingleton<IAppUpdaterService, FtpAppUpdaterService>()
                                .AddSingleton<FtpAppUpdaterOptions>(opts);
        }
    }
}
