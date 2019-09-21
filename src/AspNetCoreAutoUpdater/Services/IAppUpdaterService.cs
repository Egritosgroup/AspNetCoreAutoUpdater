using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreAutoUpdater
{
    public interface IAppUpdaterService
    {
        /// <summary>
        /// Returns the ProgressInfo of the downloading file
        /// </summary>
        ProgressInfo ProgressInfo { get; set; }
        /// <summary>
        /// Initiates the Update Progress
        /// </summary>
        /// <param name="version"></param>
        /// <param name="onSuccess"></param>
        /// <returns></returns>
        Task BeginUpdateProcessAsync(string version, Action onSuccess);
        /// <summary>
        /// CompleteUpdate after files are downloaded
        /// </summary>
        /// <param name="onSuccess"></param>
        /// <returns></returns>
        Task CompleteUpdateAsync(Action onSuccess);
        /// <summary>
        /// DownloadFiles
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        Task<DownloadContext> DownloadFilesAsync(string version);


    }
}
