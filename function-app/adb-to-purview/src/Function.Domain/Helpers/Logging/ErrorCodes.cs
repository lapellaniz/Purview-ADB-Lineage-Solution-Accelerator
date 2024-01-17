using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Function.Domain.Helpers.Logging
{
    public static class ErrorCodes
    {
        public static class Generic
        {
            public const int Error = 1;
        }

        public static class OpenLineage
        {
            public const int Error = 100;  //100-199
            public const int Error1 = 101;
        }

        public static class PurviewOut
        {
            public const int Error = 200; //200-299
            public const int Error1 = 201;
        }

        public static class SynapseAPI
        {
            public const int Error = 429;
        }
        public static class Warnings
        {
            // 300 +
            public const int Warning1 = 300;   //Generic warning codes
            public const int Warning2 = 301;   // New unexpected data assets in Purview
            public const int Warning3 = 302;   // Missing synapse serverless details
        }
    }
}