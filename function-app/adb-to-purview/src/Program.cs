using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Function.Domain.Helpers;
using Function.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Function.Domain.Middleware;
using Function.Domain.Providers;
using Microsoft.Extensions.Configuration;
using Function.Domain.Models.OL;
using Polly;
using System;
using Polly.Extensions.Http;
using System.Net.Http;
using Polly.Contrib.WaitAndRetry;
using System.Threading.Tasks;
using Polly.Retry;

namespace TestFunc
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication(workerApplication =>
                {
                    workerApplication.UseMiddleware<ScopedLoggingMiddleware>();
                })
                .ConfigureLogging((context, builder) =>
                    {
                        var key = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                        builder.AddApplicationInsights(key);
                    })
                .ConfigureServices((hostContext, s) =>
                    {
                        s.AddMemoryCache();
                        s.AddScoped<IHttpHelper, HttpHelper>();
                        s.AddScoped<IOlToPurviewParsingService, OlToPurviewParsingService>();
                        s.AddScoped<IPurviewIngestion, PurviewIngestion>();
                        s.AddScoped<IOlFilter, OlFilter>();
                        s.AddScoped<IOlConsolodateEnrich<EnrichedEvent>, OlConsolodateEnrich>();
                        s.AddScoped<IOlConsolodateEnrich<EnrichedSynapseEvent>, OlConsolidateEnrichSynapse>();
                        s.AddScoped<IOlConsolidateEnrichFactory, OlConsolidateEnrichFactory>();
                        s.AddSingleton<IBlobClientFactory, BlobClientFactory>();
                        s.AddTransient<IOlMessageProvider, OlMessageProvider>();
                        s.AddHttpClient<ISynapseClientProvider, SynapseClientProvider>()
                        .AddPolicyHandler(GetRetryPolicy());
                    })
                .Build();
            host.Run();
        }

        private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            //var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    5,
                    sleepDurationProvider: (retryAttempt, response, context) =>
                    {
                        return response.Result.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    },
                    onRetryAsync: (e, ts, i, ctx) => Task.CompletedTask
                );
                //.WaitAndRetryAsync(delay);

        }
    }
}