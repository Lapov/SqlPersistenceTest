using Topshelf;

namespace TestPersistance
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(c =>
            {
                c.RunAsNetworkService();
                c.Service<WorkerHost>();

            });
        }
    }
}
