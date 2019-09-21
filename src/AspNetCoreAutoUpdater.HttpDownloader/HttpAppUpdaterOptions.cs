using System;

namespace AspNetCoreAutoUpdater.HttpDownloader
{
    public class HttpAppUpdaterOptions : AppUpdaterOptions
    {
        public string HttpDownloadUrl { get; set; }

    }
}
