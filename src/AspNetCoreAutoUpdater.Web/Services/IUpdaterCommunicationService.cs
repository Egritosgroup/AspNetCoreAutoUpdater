using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreAutoUpdater.Web
{
    public interface IUpdaterCommunicationService
    {
        Uri GetWebAppUri();
        Version GetCurrentVersion();
        Version GetNextVersion();
        void ShutdownApp();
    }
}
