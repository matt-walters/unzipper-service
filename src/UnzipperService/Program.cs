using Topshelf;

namespace UnzipperService
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(
                serviceConfig =>
                {
                    serviceConfig.Service<UnzipperService>(
                        serviceInstance =>
                        {
                            serviceInstance.ConstructUsing(() => new UnzipperService());
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
