
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreAutoUpdater.Web;
using SampleApplication1.Models;

namespace SampleApplication1.Services
{
    public class UpdaterCommunicationService : IUpdaterCommunicationService
    {
        private readonly GeneralSettings _generalSettings;

        public UpdaterCommunicationService(GeneralSettings generalSettings)
        {
            _generalSettings = generalSettings;
        }

        public Version GetCurrentVersion()
        {
            return new Version(_generalSettings.CurrentVersion);
        }

        public Version GetNextVersion()
        {
            return new Version(_generalSettings.NewVersion);
        }

        public Uri GetWebAppUri()
        {
            return new Uri(_generalSettings.AppUrl);
        }

        public void ShutdownApp()
        {
            Program.Shutdown();
        }
    }
}
