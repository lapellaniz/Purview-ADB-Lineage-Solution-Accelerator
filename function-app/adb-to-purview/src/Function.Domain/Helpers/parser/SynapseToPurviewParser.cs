using Function.Domain.Models.Settings;
using Function.Domain.Models.OL;
using Function.Domain.Models.Purview;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Function.Domain.Helpers
{
    /// <summary>
    /// Creates Purview Databricks objects from OpenLineage and ADB data from the jobs API
    /// </summary>
    public class SynapseToPurviewParser : ISynapseToPurviewParser
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ParserSettings _parserConfig;
        private readonly IQnParser? _qnParser;
        private readonly IColParser? _colParser;
        private readonly EnrichedSynapseEvent _eEvent;
        private readonly string? _synapseWorkspaceUrl;
        const string SETTINGS = "OlToPurviewMappings";



        /// <summary>
        /// Constructor for SynapseToPurviewParser
        /// </summary>
        /// <param name="loggerFactory">Loggerfactory from Function framework DI</param>
        /// <param name="configuration">Configuration from Function framework DI</param>
        /// <param name="eEvent">The enriched event which combines OpenLineage data with data from ADB get job API</param>
        public SynapseToPurviewParser(ILoggerFactory loggerFactory, IConfiguration configuration, EnrichedSynapseEvent eEvent)
        {
            _logger = loggerFactory.CreateLogger<SynapseToPurviewParser>();
            _loggerFactory = loggerFactory;

            try
            {
                var map = configuration[SETTINGS] ?? throw new MissingCriticalDataException("critical config not found");
                _parserConfig = JsonConvert.DeserializeObject<ParserSettings>(map) ?? throw new MissingCriticalDataException("critical config not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SynapseToPurviewParser: Error retrieving ParserSettings.  Please make sure these are configured on your function.");
                throw;
            }

            if (eEvent.OlEvent == null)
            {
                throw new ArgumentNullException(nameof(eEvent));
            }

            _eEvent = eEvent;
            _synapseWorkspaceUrl = $"https://{_eEvent.OlEvent.Job.Namespace.Split(",")[0]}.dev.azuresynapse.net"; ;
            _parserConfig.AdbWorkspaceUrl = this.GetSynapseWorkspace().Attributes.Name;
            _qnParser = new QnParser(_parserConfig, _loggerFactory, null);
            _colParser = new ColParser(_parserConfig, _loggerFactory,
                                    _eEvent.OlEvent,
                                    _qnParser);
        }

        /// <summary>
        /// Creates a Purview Synapse workspace object for an enriched event
        /// </summary>
        /// <returns>A Synapse workspace object</returns>
        public SynapseWorkspace GetSynapseWorkspace()
        {
            SynapseWorkspace synapseWorkspace = new();
            string workspaceName = _eEvent.OlJobWorkspace;
            synapseWorkspace.Attributes.Name = $"{workspaceName}";
            synapseWorkspace.Attributes.QualifiedName = $"https://{workspaceName}.azuresynapse.net";
            return synapseWorkspace;
        }

        public SynapseNotebook GetSynapseNotebook(string workspaceQn)
        {
            var synapseNotebook = new SynapseNotebook();
            string sparkNoteBookName = _eEvent!.NotebookName;
            string sparkClusterName =  _eEvent!.SparkPoolName;
            string notebookPath = $"/notebooks/{sparkNoteBookName}";

            // TODO : Refactor to use async.await. remove until then.
            //var result = _synapseClientProvider.GetSparkNotebookSource(_eEvent!.OlEvent!.Job.Namespace!.Split(",")[0], sparkNoteBookName).GetAwaiter().GetResult();

            synapseNotebook.Attributes.Name = sparkNoteBookName;
            synapseNotebook.Attributes.QualifiedName = $"{workspaceQn}/{notebookPath.Trim('/')}";
            synapseNotebook.Attributes.SparkPoolName = sparkClusterName;
            synapseNotebook.Attributes.User = _eEvent!.SynapseRoot!.SparkJobs![0].Submitter!;
            synapseNotebook.Attributes.SparkVersion = _eEvent!.SynapseSparkPool!.Properties!.SparkVersion!;
            //synapseNotebook.Attributes.SourceCodeExplaination = result;
            //synapseNotebook.Attributes.Inputs = inputs;
            //synapseNotebook.Attributes.Outputs = outputs;
            synapseNotebook.RelationshipAttributes.Workspace.QualifiedName = workspaceQn;
            return synapseNotebook;
        }

        public SynapseProcess GetSynapseProcess(string sparkNotebookQn, SynapseNotebook synapseNotebook)
        {
            var synapseProcess = new SynapseProcess();
            //var ColumnAttributes = new ColumnLevelAttributes();

            List<Inputs> eventInputs;
            var eventOutputs = _eEvent!.OlEvent!.Outputs.ToList();
            // Assumes that when outputs are empty and we have multiple inputs, the first input is the sink and the rest are the source
            if (eventOutputs.Count == 0 && _eEvent!.OlEvent!.Inputs.Count > 1)
            {
                eventInputs = _eEvent!.OlEvent!.Inputs.Skip(1).DistinctBy(i => i.Name).ToList();
                eventOutputs = _eEvent!.OlEvent!.Inputs.Take(1).Select(i => new Outputs { Name = i.Name, NameSpace = i.NameSpace }).ToList();
            }
            else
            {
                eventInputs = _eEvent!.OlEvent!.Inputs.ToList();
            }

            var inputs = new List<InputOutput>();
            foreach (IInputsOutputs input in eventInputs)
            {
                inputs.Add(GetInputOutputs(input));
            }

            var outputs = new List<InputOutput>();
            foreach (IInputsOutputs output in eventOutputs)
            {
                outputs.Add(GetInputOutputs(output));
            }

            synapseProcess.Attributes = GetProcAttributes(sparkNotebookQn, inputs, outputs, _eEvent.OlEvent, synapseNotebook);
            synapseProcess.Attributes.SparkPoolName = synapseNotebook.Attributes.SparkPoolName;
            synapseProcess.Attributes.User = synapseNotebook.Attributes.User;
            synapseProcess.Attributes.SparkVersion = synapseNotebook.Attributes.SparkVersion;
            synapseProcess.Attributes.Inputs = inputs;
            synapseProcess.Attributes.Outputs = outputs;
            synapseProcess.Attributes.ColumnMapping = JsonConvert.SerializeObject(_colParser!.GetColIdentifiers());
            synapseProcess.RelationshipAttributes.Notebook.QualifiedName = sparkNotebookQn;
            return synapseProcess;
        }



        private SynapseProcessAttributes GetProcAttributes(string taskQn, List<InputOutput> inputs, List<InputOutput> outputs, Event sparkEvent, SynapseNotebook synapseNotebook)
        {
            var pa = new SynapseProcessAttributes();
            string sparkNoteBookName = synapseNotebook.Attributes.Name;
            pa.Name = sparkNoteBookName + "-lineage";
            pa.QualifiedName = taskQn + "-lineage";
            pa.ColumnMapping = JsonConvert.SerializeObject(_colParser!.GetColIdentifiers());
            //pa.SparkPlan = sparkEvent.Run.Facets.SparkLogicalPlan.ToString(Formatting.None);
            pa.Inputs = inputs;
            pa.Outputs = outputs;
            return pa;
        }


        private InputOutput GetInputOutputs(IInputsOutputs inOut)
        {
            var id = _qnParser!.GetIdentifiers(inOut.NameSpace, inOut.Name);
            var inputOutputId = new InputOutput
            {
                TypeName = id.PurviewType
            };
            inputOutputId.UniqueAttributes.QualifiedName = id.QualifiedName;

            return inputOutputId;
        }
    }
}