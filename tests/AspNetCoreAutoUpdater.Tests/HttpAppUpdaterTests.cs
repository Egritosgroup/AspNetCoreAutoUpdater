using System.IO;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using AspNetCoreAutoUpdater.HttpDownloader;

namespace AspNetCoreAutoUpdater.Tests
{
    public class HttpAppUpdaterTests
    {
        HttpAppUpdaterOptions opts;
        HttpAppUpdaterService httpAppUpdaterService;

        public HttpAppUpdaterTests()
        {
            var workingDirectory = Path.GetDirectoryName(typeof(FtpAppUpdaterTests).Assembly.Location);
            opts = new HttpAppUpdaterOptions()
            {
                AppName = "",
                AppEntryDll = "",
                ContentRootPath = workingDirectory,
                HttpDownloadUrl = "",
            };
            httpAppUpdaterService = new HttpAppUpdaterService(NullLogger<HttpAppUpdaterService>.Instance, opts);
        }


        [Fact]
        public async Task CheckFtpConnection()
        {
            await httpAppUpdaterService.DownloadFilesAsync("4.6.19144.3393");
        }
    }
}
