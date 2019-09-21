using System;
using System.Threading.Tasks;
using AspNetCoreAutoUpdater;
using AspNetCoreAutoUpdater.Extentions;
using AspNetCoreAutoUpdater.Web;
using AspNetCoreAutoUpdater.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SampleApplication1.Controllers
{
    public class AspNetCoreAutoUpdaterController : Controller
    {
        private readonly ILogger<AspNetCoreAutoUpdaterController> _logger;
        private readonly IAppUpdaterService _appUpdaterService;
        private readonly IUpdaterCommunicationService _updaterCommunicationService;

        public AspNetCoreAutoUpdaterController(ILogger<AspNetCoreAutoUpdaterController> logger,
                                                    IAppUpdaterService appUpdaterService,
                                                    IUpdaterCommunicationService updaterCommunicationService)
        {
            _logger = logger;
            _appUpdaterService = appUpdaterService;
            _updaterCommunicationService = updaterCommunicationService;
        }

        [HttpGet]
        [Route("Updater/CompleteUpdate/{versionNumber}")]
        public IActionResult CompleteUpdate(string versionNumber)
        {
            _logger.LogInformation("CompleteUpdate called for versionNumber:{versionNumber}", versionNumber);
            
            var vNext = new Version(versionNumber);
            var vCurrent = _updaterCommunicationService.GetCurrentVersion();
            if (vNext > vCurrent)
            {
                try
                {

                    var t = _appUpdaterService.BeginUpdateProcessAsync(versionNumber, () => _updaterCommunicationService.ShutdownApp());
                    //var cntxt = await _updater.DownloadFilesAsync(versionNumber);
                    //var t = _updater.CompleteUpdateAsync(() => { Program.Shutdown(); });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.GetAllMessages() + ex.StackTrace);
                    var content = ex.GetAllMessages() + ex.StackTrace;
                    return new ContentResult()
                    {
                        Content = "<body><html>׃צכלב <hr/>" + content + "</body></html>",
                        ContentType = "text/html",
                    };
                }
                return Redirect("~/updating.html?v=" + versionNumber);
            }
            else
            {
                return new ContentResult()
                {
                    Content = $@"<html>
                                    <body>
                                        Update Not Done! (Might not needed!) No need for update! <hr/>
                                        <a href='{_updaterCommunicationService.GetWebAppUri().ToString()}'>start page</a> <hr/>
                                        vCurrent : {vCurrent}<br>
                                        vNext : {vNext}<br>
                                    </body>
                                </html>",
                    ContentType = "text/html",
                };
            }
        }

        [HttpGet]
        [Route("Updater/GetProgressInfo")]
        public IActionResult GetProgressInfo()
        {
            var cur = _updaterCommunicationService.GetCurrentVersion();
            //var nxt = _updaterCommunicationService.GetNextVersion();
            return Json(new UpdateProgressInfo() { Version = cur.ToString(), ProgressInfo = _appUpdaterService.ProgressInfo });
        }
    }
}
