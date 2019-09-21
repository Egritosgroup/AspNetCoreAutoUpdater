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
        Task UpdateAsync(string version, Action onSuccess);

        Task CompleteUpdateAsync(Action onSuccess);

        Task<DownloadContext> DownloadFilesAsync(string version);


    }
}
