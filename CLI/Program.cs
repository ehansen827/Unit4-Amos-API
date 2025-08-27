using Fjord1.Int.API;
using Fjord1.Int.API.Workers;
using CommandLine; 
using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using A1AR.SVC.Worker.Lib.Common;

namespace CommandLine
{
	public static class ExtMeth
	{
		public static ParserResult<object> ParseArguments<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(this Parser parser, IEnumerable<string> args)
		{
			if (parser == null) throw new ArgumentNullException("parser");
			return parser.ParseArguments(args, new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8),typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17)});
		}


		public static TResult MapResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TResult>(this ParserResult<object> result,
			Func<T1, TResult> parsedFunc1,
			Func<T2, TResult> parsedFunc2,
			Func<T3, TResult> parsedFunc3,
			Func<T4, TResult> parsedFunc4,
			Func<T5, TResult> parsedFunc5,
			Func<T6, TResult> parsedFunc6,
			Func<T7, TResult> parsedFunc7,
			Func<T8, TResult> parsedFunc8,
			Func<T9, TResult> parsedFunc9,
			Func<T10, TResult> parsedFunc10,
			Func<T11, TResult> parsedFunc11,
			Func<T12, TResult> parsedFunc12,
			Func<T13, TResult> parsedFunc13,
			Func<T14, TResult> parsedFunc14,
			Func<T15, TResult> parsedFunc15,
			Func<T16, TResult> parsedFunc16,
			Func<T17, TResult> parsedFunc17,
		Func<IEnumerable<Error>, TResult> notParsedFunc)
		{
			var parsed = result as Parsed<object>;
			if (parsed != null)
			{
				if (parsed.Value is T1)
				{
					return parsedFunc1((T1)parsed.Value);
				}
				if (parsed.Value is T2)
				{
					return parsedFunc2((T2)parsed.Value);
				}
				if (parsed.Value is T3)
				{
					return parsedFunc3((T3)parsed.Value);
				}
				if (parsed.Value is T4)
				{
					return parsedFunc4((T4)parsed.Value);
				}
				if (parsed.Value is T5)
				{
					return parsedFunc5((T5)parsed.Value);
				}
				if (parsed.Value is T6)
				{
					return parsedFunc6((T6)parsed.Value);
				}
				if (parsed.Value is T7)
				{
					return parsedFunc7((T7)parsed.Value);
				}
				if (parsed.Value is T8)
				{
					return parsedFunc8((T8)parsed.Value);
				}
				if (parsed.Value is T9)
				{
					return parsedFunc9((T9)parsed.Value);
				}
				if (parsed.Value is T10)
				{
					return parsedFunc10((T10)parsed.Value);
				}
				if (parsed.Value is T11)
				{
					return parsedFunc11((T11)parsed.Value);
				}
				if (parsed.Value is T12)
				{
					return parsedFunc12((T12)parsed.Value);
				}
				if (parsed.Value is T13)
				{
					return parsedFunc13((T13)parsed.Value);
				}
				if (parsed.Value is T14)
				{
					return parsedFunc14((T14)parsed.Value);
				}
				if (parsed.Value is T15)
				{
					return parsedFunc15((T15)parsed.Value);
				}
				if (parsed.Value is T16)
				{
					return parsedFunc16((T16)parsed.Value);
				}
				if (parsed.Value is T17)
				{
					return parsedFunc17((T17)parsed.Value);
				}
				throw new InvalidOperationException();
			}
			return notParsedFunc(((NotParsed<object>)result).Errors);
		}
	}
}


namespace CLI 
{

    [Verb("accountingdocuments", HelpText = "Update accountingdocuments from Basware.")]
    class AccountingdocumentsOptions : CommonOptions
    {
        [Option("period", Required = true, HelpText = "Workorder current period.")]
        public int Period { get; set; }
    }

    [Verb("bestiller", HelpText = "Lager XML fil med bestillere hos F1")]
    class RsF1BestillerOptions : CommonOptions
    {
    }
    [Verb("POnummer", HelpText = "Lager XML fil med PO nummer fra F1")]
    class rsPOnummerOptions : CommonOptions
    {
    }

    class CommonOptions
	{
		[Option('i', Default = false, HelpText = "Initialize all.")]
		public bool Initialize { get; set; }

		[Option('c', Required = true, HelpText = "Connection string. Ie. -c \"Server=.; Database=agrdemoM7; Trusted_Connection=True\"")]
		public string ConnectString { get; set; }

		[Option('f', Required = false, HelpText = "Client.")]
		public string Client { get; set; }

        [Option('r', Required = false, HelpText = "Rest endpoint (base uri).")]
		public string BaseUri { get; set; }

		[Option('u', Required = false, HelpText = "Rest user name.")]
		public string UserName { get; set; }

		[Option('p', Required = false, HelpText = "Rest password.")]
		public string Password { get; set; }

        [Option('z', Required = false, HelpText = "SourceAPI")]
        public string SourceAPI { get; set; }

        //[Option('x', Required = false, HelpText = "UBW user name.")]
        //public string UserNameUBW { get; set; }

        //[Option('y', Required = false, HelpText = "UBW password.")]
        //public string PasswordUBW { get; set; }
    }

	class Program
	{
		static async Task Main(string[] args)
		{
			try
			{
				await Parser.Default.ParseArguments<

                    RsF1BestillerOptions,
                    rsPOnummerOptions

                    >(args)
					.MapResult(
                        (RsF1BestillerOptions opts) => SyncRsF1Bestiller(opts),
                        (rsPOnummerOptions opts) => SyncRsPOnummer(opts),
                        errs => Task.FromResult(0));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

        private static async Task SyncRsF1Bestiller(RsF1BestillerOptions options)
        {
            using var serviceProvider = ServiceProvider(options);
            var worker = serviceProvider.GetService<RsF1Bestiller>();
            await worker.Execute(new WorkerParameters { Initialize = options.Initialize });
        }

        private static async Task SyncRsPOnummer(rsPOnummerOptions options)
        {
            using var serviceProvider = ServiceProvider(options);
            var worker = serviceProvider.GetService<RsPOnummer>();
            await worker.Execute(new WorkerParameters { Initialize = options.Initialize });
        }

        //private static async Task SyncAccountingdocuments(AccountingdocumentsOptions options)
        //{
        //    using var serviceProvider = ServiceProvider(options);
        //    var service = serviceProvider.GetService<GetAccountingDocumentsService>();
        //    JobResult result;
        //    try
        //    {
        //        result = await service.Execute(new WorkerParametersPeriod
        //        { 
        //            Period = options.Period,
        //            //Date = options.
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        result = JobResult.Failed(ex.ToString());
        //    }

        //    if (result.Code != ResultCode.Succeeded)
        //    {
        //        Console.Error.WriteLine(result.Message);
        //    }
        //}


        private static ServiceProvider ServiceProvider(CommonOptions options)
        {
            IConfigurationRoot Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            return new ServiceCollection()
                .AddSingleton<IConfiguration>(Configuration)
                .AddScoped(m => new WorkerSettings
                {
                    AmosDbConnection = new CLI.ConnectionFactory(options.ConnectString),
                    //ATEDbConnection = new ConnectionFactory(options.ConnectString),
                    //Client = options.Client,
                    BaseUri = options.BaseUri,
					//RestUserName = options.UserName,
					//RestPassword = options.Password,
					//UserNameUBW = options.UserNameUBW,
                    //PasswordUBW = options.PasswordUBW,
					//SourceAPI = options.SourceAPI
				})
                .AddScoped(m => new WorkerSettings
                {
                    AmosDbConnection = new CLI.ConnectionFactory(options.ConnectString),
                })
                .AddWorkers()
                .AddLogging(config => config
						.ClearProviders()
						.AddConsole()
						.SetMinimumLevel(LogLevel.Debug))
                .BuildServiceProvider();
        }
    }
}