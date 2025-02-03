using A1AR.SVC.Worker.Lib.Common;
using A1AR.SVC.Worker.Lib.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Fjord1.Int.NetCore
{
	public class DependencyRegistrar : IDependencyRegistrar
	{
		public void Register(IServiceCollection builder)  
		{
			builder.AddScoped<WorkerSettings>();
			builder.AddTransient<OrderAmos>();
			builder.AddTransient<MoveXML>();
			builder.AddTransient<PoAmos>();
			builder.AddTransient<MoveInv>();
			builder.AddTransient<PrActUBW>();
			builder.AddTransient<ReceivAmos>();
			builder.AddTransient<FinalTot>();
			builder.AddTransient<OrderUBW>(); 
			builder.AddTransient<RsSupplier>(); 
			builder.AddTransient<RsF1Bestiller>();
			builder.AddTransient<RsPOnummer>();
			builder.AddTransient<SupplierSync>();
			builder.AddTransient<AmosInvoiceEx>();
			builder.AddTransient<SupplierSync55>();
			builder.AddTransient<MissingInv>();
            builder.AddTransient<SyncApoready>();
            builder.AddTransient<SyncProjects>();
        }
    }
}