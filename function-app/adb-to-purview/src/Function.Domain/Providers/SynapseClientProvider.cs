using System;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Function.Domain.Models.Adb;
using Function.Domain.Models.Settings;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Function.Domain.Models.SynapseSpark;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Function.Domain.Providers
{
    /// <summary>
    /// Provider for REST API calls to the ADB API.
    /// </summary>
    public class SynapseClientProvider : ISynapseClientProvider
    {
        // TODO : REDO config with DI
        private AppConfigurationSettings? config = new AppConfigurationSettings();

        // TODO : REDO token cache with MemoryCache provider. 

        // static for simple function cache
        private static JwtSecurityToken? _bearerToken;
        private readonly HttpClient _client;
        private readonly ILogger _log;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Constructs the AdbClientProvider object from the Function framework using DI
        /// </summary>
        /// <param name="loggerFactory">Logger Factory to support DI from function framework or code calling helper classes</param>
        /// <param name="config">Function framework config from DI</param>
        public SynapseClientProvider(ILoggerFactory loggerFactory, IConfiguration config, HttpClient httpClient, IMemoryCache cache)
        {
            _log = loggerFactory.CreateLogger<AdbClientProvider>();
            _client = httpClient;
            _cache = cache;
        }

        private async Task GetBearerTokenAsync()
        {
            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

            if (config!.IsAppUsingClientSecret())
            {
                // Even if this is a console application here, a daemon application is a confidential client application
                app = ConfidentialClientApplicationBuilder.Create(config.ClientID)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }
            else
            {
                ICertificateLoader certificateLoader = new DefaultCertificateLoader();
                certificateLoader.LoadIfNeeded(config!.Certificate!);

                app = ConfidentialClientApplicationBuilder.Create(config.ClientID)
                    .WithCertificate(config!.Certificate!.Certificate)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }

            string[] scopes = new string[] { "https://dev.azuresynapse.net/.default" };

            AuthenticationResult? result;
            try
            {
                foreach (string s in scopes)
                {
                    _log.LogInformation(s);
                }
                _log.LogInformation(config.ClientID);
                _log.LogInformation(config.Authority);

                result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                _log.LogError("Error getting Authentication Token for Synapse Workspace API");
                return;
            }
            catch (Exception coreex)
            {

                _log.LogError($"Error getting Authentication Token for Synapse Workspace API");
                _log.LogError(coreex.Message);
                return;
            }

            _bearerToken = new JwtSecurityToken(result.AccessToken);

        }

        public async Task<SynapseRoot?> GetSynapseJobAsync(long runId, string synapseWorkspaceName)
        {
            if(_cache.TryGetValue<SynapseRoot>(runId, out SynapseRoot? cachedSynapseRoot))
            {
                return cachedSynapseRoot;
            }

            if (isTokenExpired(_bearerToken))
            {
                await GetBearerTokenAsync();

                if (_bearerToken is null)
                {
                    _log.LogError("SynapseClient-GetSynapseJobAsync: unable to get bearer token");
                    return null;
                }
            }

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"https://{synapseWorkspaceName}.dev.azuresynapse.net/monitoring/workloadTypes/spark/Applications?api-version=2020-12-01&$filter=contains(name, '{runId}')"),
                Method = HttpMethod.Get,
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _bearerToken!.RawData);

            SynapseRoot? resultSynapseRoot = null;
            try
            {
                var tokenResponse = await _client.SendAsync(request);

                tokenResponse.EnsureSuccessStatusCode();
                resultSynapseRoot = JsonConvert.DeserializeObject<SynapseRoot>(await tokenResponse.Content.ReadAsStringAsync());
                _cache.Set(runId, resultSynapseRoot, TimeSpan.FromHours(5));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"SynapseClient-GetSynapseJobAsync: error, message: {ex.Message}");
            }
            return resultSynapseRoot;
        }

        public async Task<SynapseSparkPool?> GetSynapseSparkPoolsAsync(string synapseWorkspaceName, string synapseSparkPoolName)
        {
            if(_cache.TryGetValue<SynapseSparkPool>(synapseSparkPoolName, out SynapseSparkPool? cachedSynapseSparkPool))
            {
                return cachedSynapseSparkPool;
            }
            
            if (isTokenExpired(_bearerToken))
            {
                await GetBearerTokenAsync();

                if (_bearerToken is null)
                {
                    _log.LogError("SynapseClient-GetSynapseSparkPoolsAsync: unable to get bearer token");
                    return null;
                }
            }

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri($"https://{synapseWorkspaceName}.dev.azuresynapse.net/bigDataPools/{synapseSparkPoolName}?api-version=2020-12-01"),
                Method = HttpMethod.Get
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _bearerToken!.RawData);

            SynapseSparkPool? resultSynapseSparkPool = null;
            try
            {
                var tokenResponse = await _client.SendAsync(request);

                tokenResponse.EnsureSuccessStatusCode();
                resultSynapseSparkPool = JsonConvert.DeserializeObject<SynapseSparkPool>(await tokenResponse.Content.ReadAsStringAsync());
                _cache.Set(synapseSparkPoolName, resultSynapseSparkPool, TimeSpan.FromHours(5));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"SynapseClient-GetSynapseSparkPoolsAsync: error, message: {ex.Message}");
            }
            return resultSynapseSparkPool;
        }


        /// get synapse spark notebook source
        public async Task<string> GetSparkNotebookSource(string synapseWorkspaceName, string sparkNoteBookName)
        {
            if (config?.OpenAIEndpoint is null)
            {
                _log.LogError("SynapseClient-GetSparkNotebookSource: OpenAIEndpoint is null");
                return string.Empty;
            }

            if (isTokenExpired(_bearerToken))
            {
                await GetBearerTokenAsync();

                if (_bearerToken is null)
                {
                    _log.LogError("SynapseClient-GetSparkNotebookSource: unable to get bearer token");
                    return string.Empty;
                }
            }
            var request = new HttpRequestMessage()
            {

                RequestUri = new Uri($"https://{synapseWorkspaceName}.dev.azuresynapse.net/notebooks/{sparkNoteBookName}?api-version=2020-12-01"),
                Method = HttpMethod.Get,
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _bearerToken!.RawData);

            SynapseSparkNoteBook? resultSynapseSparkNoteBook = null;
            try
            {
                var tokenResponse = await _client.SendAsync(request);

                tokenResponse.EnsureSuccessStatusCode();
                resultSynapseSparkNoteBook = JsonConvert.DeserializeObject<SynapseSparkNoteBook>(tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                string sparkNoteBookSource = "";

                if (resultSynapseSparkNoteBook?.Properties?.Cells is not null)
                {
                    foreach (var item in resultSynapseSparkNoteBook.Properties.Cells)
                    {
                        if (item?.CellType == "code")
                        {
                            foreach (var line in item.Source)
                            {
                                sparkNoteBookSource += line;
                            }

                        }
                    }
                }
                return await GetOpenAICompletions(sparkNoteBookSource);

            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"SynapseClient-GetSparkNotebookSource: error, message: {ex.Message}");
                return ex.Message;

            }


        }

        private async Task<string> GetOpenAICompletions(string sparkNotebookSource)
        {



            string openAIUri = $"{config?.OpenAIEndpoint}/openai/deployments/{config?.OpenAIDeploymentName}/completions?api-version=2022-12-01";

            //_client.DefaultRequestHeaders.Authorization  = new AuthenticationHeaderValue("api-key", config?.OpenAIKey);
            var requestBody = new
            {
                prompt = "Explain this python code:\n\n" + sparkNotebookSource + "\n\n",
                max_tokens = config?.OpenAIMaxTokens
            };
            var request = new HttpRequestMessage()
            {

                RequestUri = new Uri(openAIUri),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", config?.OpenAIKey);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");



            try
            {
                //string requestBodyJson = System.Text.Json.JsonSerializer.Serialize(requestBody);
                //var requestBodyContent = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

                var tokenResponse = await _client.SendAsync(request);
                tokenResponse.EnsureSuccessStatusCode();
                OpenAICompletionResponse? resultFromOpenAI = JsonConvert.DeserializeObject<OpenAICompletionResponse>(tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                if (resultFromOpenAI != null && resultFromOpenAI.Choices != null)
                {
                    string? result = resultFromOpenAI.Choices[0].Text;
                    return result ?? "Could not get source code explaination.";
                }
                else
                    return "Could not get source code explaination.";

            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"SynapseClient-GetOpenAICompletions: error, message: {ex.Message}");
                return "Could not get source code explaination.";

            }

        }

        private bool isTokenExpired(JwtSecurityToken? jwt)
        {
            if (jwt is null)
            {
                return true;
            }
            if (jwt.ValidTo > DateTime.Now.AddMinutes(3))
            {
                _log.LogInformation("SynapseClient-isTokenExpired: Token cache hit");
                return false;
            }
            return true;
        }
    }
}