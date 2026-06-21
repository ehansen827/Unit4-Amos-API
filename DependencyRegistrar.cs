using System.Net.NetworkInformation;
using A1AR.SVC.Worker.Lib.Common;
using A1AR.SVC.Worker.Lib.DependencyInjection;
//using A1AR.Utilities.Database;
//using A1AR.Utilities.Helpers;
using Fjord1.Int.API.Services;
using Fjord1.Int.API.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Fjord1.Int.API
{
    public static class WorkerServiceBuilder
    {
        //public class DependencyRegistrar : IDependencyRegistrar
        //{
        //    public void Register(IServiceCollection builder)
        //    {
        //        builder.AddScoped<WorkerSettings>();
        //        builder.AddTransient<OrderAmos>();
        //        builder.AddTransient<MoveXML>();
        //        builder.AddTransient<PoAmos>();
        //        builder.AddTransient<MoveInv>();
        //        builder.AddTransient<PrActUBW>();
        //        builder.AddTransient<ReceivAmos>();
        //        builder.AddTransient<FinalTot>();
        //        builder.AddTransient<OrderUBW>();
        //        builder.AddTransient<RsSupplier>();
        //        builder.AddTransient<RsF1Bestiller>();
        //        builder.AddTransient<RsPOnummer>();
        //        builder.AddTransient<SupplierSync>();
        //        //builder.AddTransient<AmosInvoiceEx>();
        //        //builder.AddTransient<SupplierSync55>();
        //        builder.AddTransient<MissingInv>();
        //        builder.AddTransient<SyncApoready>();
        //        builder.AddTransient<SyncProjects>();
        //        builder.AddScoped<IGetHttpClient, GetHttpClient>();
        //        //builder.AddScoped<IRest, Rest>();
        //    }
        //}
        public static IServiceCollection AddWorkers(this IServiceCollection services)
        {
            return services
                //.UseTimer()
                //.UseChangeDetector()
                //.UseWorkerMutex()
                .AddScoped<FinalTot>()
                .AddScoped<MissingInv>()
                .AddScoped<MoveInv>()
                .AddScoped<MoveXML>()
                .AddScoped<OrderAmos>()
                .AddScoped<OrderUBW>()
                .AddScoped<PoAmos>()
                .AddScoped<PrActUBW>()
                .AddScoped<ReceivAmos>()
                .AddScoped<RsF1Bestiller>()
                .AddScoped<RsPOnummer>()
                .AddScoped<RsSupplier>()
                .AddScoped<RsSupplier>()
                .AddScoped<SupplierSync>()
                .AddScoped<SyncApoready>()
                .AddScoped<SyncProjects>()
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