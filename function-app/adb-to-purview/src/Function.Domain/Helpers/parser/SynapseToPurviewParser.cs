using Function.Domain.Helpers;
using Function.Domain.Models.Settings;
using Function.Domain.Models.OL;
using Function.Domain.Models.Adb;
using Function.Domain.Models.Purview;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Function.Domain.Providers;
using Function.Domain.Models.SynapseSpark;
using System.Threading.Tasks;

namespace Function.Domain.Helpers
{
    /// <summary>
    /// Creates Purview Databricks objects from OpenLineage and ADB data from the jobs API
    /// </summary>
    public class SynapseToPurviewParser: ISynapseToPurviewParser
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ParserSettings _parserConfig;
        private readonly IQnParser? _qnParser;
        private readonly IColParser? _colParser;
        private readonly EnrichedSynapseEvent? _eEvent;
        private readonly string? _synapseWorkspaceUrl;
        
        private readonly ISynapseClientProvider _synapseClientProvider;
        const string SETTINGS = "OlToPurviewMappings";

        

        /// <summary>
        /// Constructor for DatabricksToPurviewParser
        /// </summary>
        /// <param name="loggerFactory">Loggerfactory from Function framework DI</param>
        /// <param name="configuration">Configuration from Function framework DI</param>
        /// <param name="eEvent">The enriched event which combines OpenLineage data with data from ADB get job API</param>
        public SynapseToPurviewParser(ILoggerFactory loggerFactory, IConfiguration configuration, EnrichedSynapseEvent eEvent)
        {
            _logger = loggerFactory.CreateLogger<SynapseToPurviewParser>();
            _loggerFactory = loggerFactory;
            _synapseClientProvider = new SynapseClientProvider(loggerFactory, configuration);

            try{
            var map = configuration[SETTINGS];
            _parserConfig = JsonConvert.DeserializeObject<ParserSettings>(map) ?? throw new MissingCriticalDataException("critical config not found");
            } 
            catch (Exception ex) {
                _logger.LogError(ex,"SynapseToPurviewParser: Error retrieving ParserSettings.  Please make sure these are configured on your function.");
                throw;
            }
           
           if(eEvent.OlEvent != null){
                _eEvent = eEvent;
                _synapseWorkspaceUrl = $"https://{_eEvent.OlEvent.Job.Namespace.Split(",")[0]}.dev.azuresynapse.net";;
                _parserConfig.AdbWorkspaceUrl = this.GetSynapseWorkspace().Attributes.Name;
                _qnParser = new QnParser(_parserConfig, _loggerFactory, null);

                _colParser = new ColParser(_parserConfig, _loggerFactory,
                                        _eEvent.OlEvent,
                                        _qnParser);            
           }

        }

        /// <summary>
        /// Gets the job type from the supported ADB job types.  Currently all are supported except Spark Submit jobs.
        /// </summary>
        /// <returns></returns>
        

        /// <summary>
        /// Creates a Purview Databricks workspace object for an enriched event
        /// </summary>
        /// <returns>A Databricks workspace object</returns>
        public SynapseWorkspace GetSynapseWorkspace()
        {
            SynapseWorkspace synapseWorkspace = new SynapseWorkspace();
            synapseWorkspace.Attributes.Name = $"{_eEvent!.OlEvent!.Job.Namespace!.Split(",")[0]}";
            synapseWorkspace.Attributes.QualifiedName = $"synapse://{_eEvent!.OlEvent!.Job.Namespace!.Split(",")[0]}.dev.azuresynapse.net";
            return synapseWorkspace;
        }

        public SynapseNotebook GetSynapseNotebook(string workspaceQn)
        {
            var synapseNotebook = new SynapseNotebook();
            string sparkjobname = _eEvent!.OlEvent!.Job.Name.Split(".")[0].Split("_")[_eEvent!.OlEvent!.Job.Name.Split(".")[0].Split("_").Length-1];
            string sparkNoteBookName = _eEvent!.OlEvent!.Job.Name.Substring(0,_eEvent!.OlEvent!.Job.Name.IndexOf(sparkjobname) - 1);
            string sparkClusterName = sparkNoteBookName.Split("_").Last();
            sparkNoteBookName = String.Join("_", sparkNoteBookName.Split("_").Take(sparkNoteBookName.Split("_").Length - 1));
            
            string notebookPath = $"authoring/analyze/notebooks/{sparkNoteBookName}";
                       
            

            synapseNotebook.Attributes.Name = sparkNoteBookName;
            synapseNotebook.Attributes.QualifiedName = $"{workspaceQn}/{notebookPath.Trim('/')}";
            synapseNotebook.Attributes.SparkPoolName = sparkClusterName;
            synapseNotebook.Attributes.User = _eEvent!.SynapseRoot!.SparkJobs![0].Submitter!;
            synapseNotebook.Attributes.SparkVersion = _eEvent!.SynapseSparkPool!.Properties!.SparkVersion!;
            //synapseNotebook.Attributes.Inputs = inputs;
            //synapseNotebook.Attributes.Outputs = outputs;
            synapseNotebook.RelationshipAttributes.Workspace.QualifiedName = workspaceQn;
            return synapseNotebook;
        }

        public SynapseProcess GetSynapseProcess(string sparkNotebookQn, SynapseNotebook synapseNotebook)
        {
            var synapseProcess = new SynapseProcess();
            //var ColumnAttributes = new ColumnLevelAttributes();

            var inputs = new List<InputOutput>();
            foreach (IInputsOutputs input in _eEvent!.OlEvent!.Inputs)
            {
                inputs.Add(GetInputOutputs(input));
            }

            var outputs = new List<InputOutput>();
            foreach (IInputsOutputs output in _eEvent.OlEvent!.Outputs)
            {
                outputs.Add(GetInputOutputs(output));
            }

            synapseProcess.Attributes = GetProcAttributes(sparkNotebookQn, inputs,outputs,_eEvent.OlEvent);
            synapseProcess.Attributes.SparkPoolName = synapseNotebook.Attributes.SparkPoolName;
            synapseProcess.Attributes.User = synapseNotebook.Attributes.User;
            synapseProcess.Attributes.SparkVersion = synapseNotebook.Attributes.SparkVersion;
            synapseProcess.Attributes.Inputs = inputs;
            synapseProcess.Attributes.Outputs = outputs;
            synapseProcess.Attributes.ColumnMapping = JsonConvert.SerializeObject(_colParser!.GetColIdentifiers());
            synapseProcess.RelationshipAttributes.Notebook.QualifiedName = sparkNotebookQn; 
            return synapseProcess;
        }

       

        private SynapseProcessAttributes GetProcAttributes(string taskQn, List<InputOutput> inputs, List<InputOutput> outputs, Event sparkEvent)
        {
            var pa = new SynapseProcessAttributes();
            string sparkjobname = sparkEvent.Job.Name.Split(".")[0].Split("_")[sparkEvent.Job.Name.Split(".")[0].Split("_").Length-1];
            string sparkNoteBookName = sparkEvent.Job.Name.Substring(0,sparkEvent.Job.Name.IndexOf(sparkjobname) - 1);
            pa.Name = sparkNoteBookName + "-lineage";
            pa.QualifiedName = taskQn + "-lineage"; //+ sparkEvent.Outputs[0].Name;
            pa.ColumnMapping = JsonConvert.SerializeObject(_colParser!.GetColIdentifiers());
            //pa.SparkPlan = sparkEvent.Run.Facets.SparkLogicalPlan.ToString(Formatting.None);
            pa.Inputs = inputs;
            pa.Outputs = outputs;
            return pa;
        }

        private InputOutput GetInputOutputs(IInputsOutputs inOut)
        {
            var id = _qnParser!.GetIdentifiers(inOut.NameSpace,inOut.Name);
            var inputOutputId = new InputOutput();
            inputOutputId.TypeName = id.PurviewType;
            inputOutputId.UniqueAttributes.QualifiedName = id.QualifiedName;

            return inputOutputId;
        }

        private string GetInputsOutputsHash(List<InputOutput> inputs, List<InputOutput> outputs)
        {
            inputs.Sort((x, y) => x.UniqueAttributes.QualifiedName.CompareTo(y.UniqueAttributes.QualifiedName));;
            StringBuilder sInputs = new StringBuilder(inputs.Count);
            foreach (var input in inputs)
            {
                sInputs.Append(input.UniqueAttributes.QualifiedName.ToLower().ToString());
                if (!input.Equals(inputs.Last()))
                {
                    sInputs.Append(",");
                }
            }
            var inputHash = GenerateMd5Hash(sInputs.ToString());
            // Outputs should only ever have one item
            var outputHash = GenerateMd5Hash(outputs[0].UniqueAttributes.QualifiedName.ToString());

            return $"{inputHash}->{outputHash}";
        }

        private string GenerateMd5Hash(string input)
        {
            byte[] tmpSource;
            byte[] tmpHash;

            //Create a byte array from source data.
            tmpSource = ASCIIEncoding.ASCII.GetBytes(input);

            //Compute hash based on source data.
            tmpHash = MD5.Create().ComputeHash(tmpSource);

            StringBuilder sOutput = new StringBuilder(tmpHash.Length);
            for (int i=0;i < tmpHash.Length; i++)
            {
                sOutput.Append(tmpHash[i].ToString("X2"));
            }
            return sOutput.ToString();
        }
        
    }
}