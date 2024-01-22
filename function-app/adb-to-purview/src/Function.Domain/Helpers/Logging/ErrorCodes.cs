namespace Function.Domain.Helpers.Logging
{
    public static class ErrorCodes
    {

        public static class OpenLineage
        {
            public const int GenericError = 100;  //100-199
            public const int EventValidation = 101;
            public const int JsonSerialization = 102;
            public const int ParseEvent = 103;
        }

        public static class PurviewOut
        {
            public const int GenericError = 200; //200-299
            public const int SynapseOlMessageGeneric = 201;
            public const int OlSynapseMessageEnrichment = 202;
            public const int OlSynapseMessageConsolidation = 203;
            public const int SynapseToPurviewParser = 204;
        }

        public static class SynapseAPI
        {
            public const int GetSynapseStorageLocation = 429;
            public const int GetSynapseJob = 430;
            public const int GetSynapseSparkPools = 431;
        }
        public static class Warnings
        {
            // 300 +
            public const int OlSynapseMessageEnrichmentCaptureNameSpace = 300;   //Generic warning codes
            public const int Warning2 = 301;   // New unexpected data assets in Purview  TODO Mani Sendtopurview
        }
    }
}