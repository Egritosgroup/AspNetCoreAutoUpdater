namespace AspNetCoreAutoUpdater
{
    public class ProgressInfo
    {
        public int TotalFiles { get; set; }
        public string CurrentFile { get; set; }
        public int CurrentFileIndex { get; set; }
        public double Value { get; set; }
    }
}
