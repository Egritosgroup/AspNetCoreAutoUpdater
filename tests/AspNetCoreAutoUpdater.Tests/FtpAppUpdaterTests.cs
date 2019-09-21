using System;
using System.IO;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using AspNetCoreAutoUpdater.FtpDownloader;

namespace AspNetCoreAutoUpdater.Tests
{

    public class FtpAppUpdaterTests
    {
        FtpAppUpdaterOptions opts;
        FtpAppUpdaterService ftpAppUpdaterService;

        public FtpAppUpdaterTests()
        {
            var workingDirectory = Path.GetDirectoryName(typeof(FtpAppUpdaterTests).Assembly.Location);
            opts = new FtpAppUpdaterOptions()
            {
                AppName = "",
                AppEntryDll = "",
                ContentRootPath = workingDirectory,
                Host = "",
                Username = "",
                Password = "",
                FtpFolderHostingTheApp = ""
            };
            ftpAppUpdaterService = new FtpAppUpdaterService(NullLogger<FtpAppUpdaterService>.Instance, opts);
        }


        [Fact]
        public async Task CheckFtpConnection()
        {
            await ftpAppUpdaterService.DownloadFilesAsync("4.6.19144.3393");
        }
    }
}
