using System;

namespace AspNetCoreAutoUpdater.FtpDownloader
{
    public class FtpAppUpdaterOptions : AppUpdaterOptions
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// by default o fpt xristis pou exoume mpainei ston fakelo me ta versions kateu8ian. auto einai an 8eloume na allaksoume
        /// </summary>
        public string FtpFolderHostingTheApp { get; set; } = "";
        public bool TrustClientCertificate { get; set; } = true;

    }
}
