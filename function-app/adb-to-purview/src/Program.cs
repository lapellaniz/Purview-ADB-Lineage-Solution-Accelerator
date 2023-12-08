using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Function.Domain.Helpers;
using Function.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Function.Domain.Middleware;
using Function.Domain.Providers;
using Microsoft.Extensions.Configuration;
using Function.Domain.Models.OL;

namespace TestFunc
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication(workerApplication => {
                    workerApplication.UseMiddleware<ScopedLoggingMiddleware>();
                })
                .ConfigureLogging((context, builder) =>
                    {
                        var key = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                        builder.AddApplicationInsights(key);
                    })
                .ConfigureServices((hostContext, s) =>
                    {
                        s.AddScoped<IHttpHelper, HttpHelper>();
                        s.AddScoped<IOlToPurviewParsingService, OlToPurviewParsingService>();
                        s.AddScoped<IPurviewIngestion, PurviewIngestion>();
                        s.AddScoped<IOlFilter, OlFilter>();
                        s.AddScoped<IOlConsolodateEnrich<EnrichedEvent>, OlConsolodateEnrich>();
                        s.AddScoped<IOlConsolodateEnrich<EnrichedSynapseEvent>, OlConsolidateEnrichSynapse>();
                        s.AddScoped<IOlConsolidateEnrichFactory, OlConsolidateEnrichFactory>();
                        s.AddSingleton<IBlobClientFactory, BlobClientFactory>();
                        s.AddTransient<IOlMessageProvider, OlMessageProvider>();
                    })
                .Build();
            host.Run();
        }
    }
}