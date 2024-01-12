using System;
using System.Collections.Generic;
using System.Linq;
using Function.Domain.Models.Adb;
using Function.Domain.Models.SynapseSpark;

namespace Function.Domain.Models.OL
{
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
}