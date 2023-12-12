using System;
using System.Collections.Generic;
using System.Linq;
using Function.Domain.Models.Adb;
using Function.Domain.Models.SynapseSpark;

namespace Function.Domain.Models.OL
{
    public interface IEnrichedEvent
    {
        public Event? OlEvent { get; }
    }
    public class EnrichedEvent : IEnrichedEvent
    {
        public AdbRoot? AdbRoot = null;
        public AdbRoot? AdbParentRoot = null;
        public bool IsInteractiveNotebook = false;
        public EnrichedEvent(Event olEvent, AdbRoot? adbRoot, AdbRoot? adbParentRoot)
        {
            OlEvent = olEvent;
            AdbRoot = adbRoot;
            AdbParentRoot = adbParentRoot;
        }

        public Event? OlEvent { get; private set; }
    }



    public class EnrichedSynapseEvent : IEnrichedEvent
    {
        public Event? OlEvent { get; private set; }

        public string OlJobName
        {
            get
            {
                return OlEvent?.Job.Name ?? string.Empty;
            }
        }

        public string OlJobWorkspace { get; init; } = string.Empty;

        public string SparkPoolName { get; init; } = string.Empty;

        public string SparkApplicationId { get; init; } = string.Empty;

        public string NotebookName { get; init; } = string.Empty;

        public SynapseRoot? SynapseRoot { get; private set; } = null;

        public SynapseSparkPool? SynapseSparkPool { get; private set; } = null;

        public EnrichedSynapseEvent(Event olEvent, SynapseRoot? synapseRoot, SynapseSparkPool? synapseSparkPool)
        {
            OlEvent = olEvent;
            SynapseRoot = synapseRoot;
            SynapseSparkPool = synapseSparkPool;
        }
    }
}