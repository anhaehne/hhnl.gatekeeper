using System;
using hhnl.gatekeeper.ImageProcessing.ObjectDetection;
using hhnl.gatekeeper.ImageProcessing.ObjectTracking;
using hhnl.gatekeeper.ImageProcessing.VideoStream;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Runtime;

namespace hhnl.gatekeeper.ImageProcessing
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddImageProcessing(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<YoloV5Detector.Options>(options => config.GetSection(nameof(YoloV5Detector)).Bind(options));
            services.Configure<VideoStreamProcessor.Options>(options => config.GetSection(nameof(VideoStreamProcessor)).Bind(options));
            services.Configure<ObjectDetector.Options>(options => config.GetSection(nameof(ObjectDetector)).Bind(options));
            
            services.AddScoped<YoloV5Detector>();
            services.AddScoped<ObjectDetector>();
            services.AddScoped<ObjectDetector.Context>();

            services.AddScoped<ObjectTracker.Context>();
            
            services.AddScoped<VideoStreamProcessor>();

            services.AddSingleton(provider =>
            {
                var logger = provider.GetService<ILogger<MLContext>>();
                var context = new MLContext();

                context.Log += (_, args) =>
                {
                    switch(args.Kind)
                    {
                        case ChannelMessageKind.Trace:
                            logger.LogTrace(args.Message);
                            break;
                        case ChannelMessageKind.Info: 
                            logger.LogInformation(args.Message);
                            break;
                        case ChannelMessageKind.Warning: 
                            logger.LogWarning(args.Message);
                            break;
                        case ChannelMessageKind.Error: 
                            logger.LogError(args.Message);
                            break;
                    };
                };
                
                return context;
            });
            
            return services;
        }
    }
}