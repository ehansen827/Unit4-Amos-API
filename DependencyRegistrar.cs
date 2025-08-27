using A1AR.SVC.Worker.Lib.Common;
using A1AR.SVC.Worker.Lib.DependencyInjection;
using A1AR.Utilities.Database;
using A1AR.Utilities.Helpers;
using Fjord1.Int.API.Services;
using Fjord1.Int.API.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Fjord1.Int.API
{
    public static class WorkerServiceBuilder
    {
        public static IServiceCollection AddWorkers(this IServiceCollection services)
        {
            return services
                .UseTimer()
                .UseChangeDetector()
                .UseWorkerMutex()
                .AddScoped<RsF1Bestiller>()
                .AddScoped<RsPOnummer>()
                .AddScoped<IGetHttpClient, GetHttpClient>()
                //.AddScoped<IUbwRepository, UbwRepository>()
                //.AddScoped<IRest, Rest>()
                //.AddScoped(typeof(IMasterDataPusher<>), typeof(MasterDataPusher<>))
                ;
        }
    }

    public class DependencyRegistrar : IDependencyRegistrar
    {
        // Add only dependencies that are provided by BaswareCLI here. The rest goes to AddWorkers().  System.
        public void Register(IServiceCollection builder)
        {
            builder
                .AddScoped<WorkerSettings>()
                .AddWorkers();
        }
    }
}