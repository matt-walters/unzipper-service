using System.Configuration;
using Topshelf;

namespace UnzipperService
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceFolder = ConfigurationManager.AppSettings["sourceFolderPath"];
            var destinationFolder = ConfigurationManager.AppSettings["destinationFolderPath"];
            var workerTaskCount = int.Parse(ConfigurationManager.AppSettings["workerTaskCount"]);

            HostFactory.Run(
                serviceConfig =>
                {
                    serviceConfig.Service<UnzipperService>(
                        serviceInstance =>
                        {
                            serviceInstance.ConstructUsing(
                                () => new UnzipperService(sourceFolder, destinationFolder, workerTaskCount));

                            serviceInstance.WhenStarted(execute => execute.Start());
                            serviceInstance.WhenStopped(execute => execute.Stop());
                        });
                    serviceConfig.SetDisplayName("Unzipper Service");
                    serviceConfig.SetDescription("Automatically unzips files placed in a specified folder");
                    serviceConfig.SetServiceName("UnzipperService");
                });
        }
    }
}
