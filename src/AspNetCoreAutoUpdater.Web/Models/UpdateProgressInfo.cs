using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreAutoUpdater.Web.Models
{
    public class UpdateProgressInfo
    {
        public string Version { get; set; }
        public ProgressInfo ProgressInfo { get; set; }
    }
}
