using hhnl.gatekeeper.ImageProcessing.ObjectDetection;
using hhnl.gatekeeper.ImageProcessing.VideoStream;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace hhnl.gatekeeper.ImageProcessing
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddImageProcessing(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<YoloV5Detector.Options>(options => config.GetSection(nameof(YoloV5Detector)).Bind(options));
            services.Configure<VideoStreamProcessor.Options>(options => config.GetSection(nameof(VideoStreamProcessor)).Bind(options));
            
            services.AddScoped<YoloV5Detector>();
            services.AddScoped<ObjectDetector>();
            services.AddScoped<ObjectDetector.Context>();
            
            services.AddScoped<VideoStreamProcessor>();
            
            return services;
        }
    }
}