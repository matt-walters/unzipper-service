using System.Configuration;

namespace UnzipperService
{
    public class UnzipperConfig
    {
        public string SourceFolder { get; private set; }
        public string DestinationFolder { get; private set; }
        public bool DeleteFileAfterUnzip { get; private set; }
        public int WorkerTaskCount { get; private set; }

        public static UnzipperConfig FromAppConfig()
        {
            return new UnzipperConfig()
            {
                SourceFolder = ConfigurationManager.AppSettings["sourceFolderPath"],
                DestinationFolder = ConfigurationManager.AppSettings["destinationFolderPath"],
                DeleteFileAfterUnzip = bool.Parse(ConfigurationManager.AppSettings["deleteFileAfterUnzip"]),
                WorkerTaskCount = int.Parse(ConfigurationManager.AppSettings["workerTaskCount"])
            };
        }
    }
}
