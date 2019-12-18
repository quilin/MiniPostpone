using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MiniPostpone.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(b => b
                    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"))
                    .AddEnvironmentVariables()
                    .AddCommandLine(args))
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddOptions<MqConfiguration>()
                        .Configure(hostContext.Configuration.GetSection(nameof(MqConfiguration)).Bind);

                    services
                        .AddOptions()
                        .AddHostedService<Worker>();
                });
    }
}