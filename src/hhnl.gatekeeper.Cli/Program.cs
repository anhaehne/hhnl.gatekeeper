using hhnl.gatekeeper.ImageProcessing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceCollectionExtensions = hhnl.gatekeeper.ImageProcessing.ServiceCollectionExtensions;

namespace hhnl.gatekeeper.Cli
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
            {
                services.AddMediatR(config =>
                {
                    config.Using<Mediator>().AsScoped();
                }, typeof(ServiceCollectionExtensions));
                services.AddImageProcessing(context.Configuration);
                services.AddHostedService<TestRunner>();
            }).Build().Run();
        }
    }
}