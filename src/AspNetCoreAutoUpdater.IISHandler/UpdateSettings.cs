using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreAutoUpdater.IISHandler
{

    public class UpdateSettings
    {
        public UpdateSettings()
        {
            ExcludedDirectoriesFromClean = new string[] { };
        }

        public string[] ExcludedDirectoriesFromClean { get; set; }
        public string DirectoryName { get; set; }
        public string AppEntryDll { get; set; }
    }

}
