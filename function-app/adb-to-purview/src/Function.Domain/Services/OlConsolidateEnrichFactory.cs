using System;
using System.Threading.Tasks;
using Function.Domain.Models.OL;
using Function.Domain.Models;
using System.Drawing;
using Microsoft.Extensions.DependencyInjection;

namespace Function.Domain.Services
{
    /// <summary>
    /// Service that consolidates OpenLineage Start messages, containing environment data, with Complete messages.  
    /// Further, this service enriches OpenLineage messages with data from the ADB Jobs API
    /// </summary>
    public class OlConsolidateEnrichFactory : IOlConsolidateEnrichFactory
    {
        private readonly IServiceProvider _provider;


        public OlConsolidateEnrichFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IOlConsolodateEnrich<TEvent> CreateEnrichment<TEvent>(OlEnrichmentType type) where TEvent : class, IEnrichedEvent
        {
            switch (type)
            {
                case OlEnrichmentType.Adb:
                    return (IOlConsolodateEnrich<TEvent>)_provider.GetRequiredService<OlConsolodateEnrich>();
                case OlEnrichmentType.Synapse:
                    return (IOlConsolodateEnrich<TEvent>)_provider.GetRequiredService<OlConsolidateEnrichSynapse>();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}