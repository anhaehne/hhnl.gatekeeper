using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.gatekeeper.ImageProcessing.VideoStream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace hhnl.gatekeeper.Cli
{
    public class TestRunner : IHostedService, IDisposable
    {
        private readonly IServiceScope _serviceScope;
        private readonly VideoStreamProcessor _videoStreamProcessor;

        public TestRunner(IServiceProvider serviceProvider)
        {
            _serviceScope = serviceProvider.CreateScope();
            _videoStreamProcessor = _serviceScope.ServiceProvider.GetRequiredService<VideoStreamProcessor>();
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var videoStream = new VideoFileStream(@"C:\Users\andre\Desktop\run.mp4");
            
            _videoStreamProcessor.Start(videoStream);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _videoStreamProcessor.Stop();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _serviceScope?.Dispose();
        }
    }
}