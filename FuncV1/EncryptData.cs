using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KeyVaultEncryptionLibrary;
using CosmosLibrary;

namespace FuncV1
{
    /// <summary>
    /// This Function retrieves the DEK from redis 
    ///  Uses the DEK to encrypt data using AES encryption
    ///  Stores the data into cosmos DB
    ///  
    /// Take in parameters for database, collection, and data to be encrypted. 
    /// Stores this data encrypted in cosmos. 
    /// And returns encrypted data. 
    /// 
    /// Expected JSON Request Body:
    ///     {
    ///         "databaseName": "XXXXX",
    ///         "collectionID": "XXXXX",
    ///         "dataToBeEncrypted": "XXXXX",
    ///         "userID": "XXXXX",
    ///         "kekIDentifier": "XXXXX"
    ///     }
    /// 
    /// </summary>

    public static class EncryptData
    {
        [FunctionName("EncryptData")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // KeyVault Repository Configuration
            string applicationId = System.Environment.GetEnvironmentVariable("applicationId");
            string applicationSecret = System.Environment.GetEnvironmentVariable("applicationSecret");
            string keyVaultPath = System.Environment.GetEnvironmentVariable("keyVaultPath");
            string dekIdentifier = System.Environment.GetEnvironmentVariable("dataEncryptionKey");
            string redisConnection = System.Environment.GetEnvironmentVariable("redisConnectionString");

            // Cosmos Configuration 
            string cosmosEndpoint = System.Environment.GetEnvironmentVariable("cosmosEndpoint");
            string cosmosPrimaryKey = System.Environment.GetEnvironmentVariable("cosmosPrimaryKey");

            // Request Body Parsing
            string requestBody = await req.Content.ReadAsStringAsync();
            JObject jdata = JObject.Parse(requestBody);

            // Optional Kekidentifier Parameter for using a specific key encryption key. 
            string kekIdentifier = jdata.Value<string>("kekIdentifier") ?? System.Environment.GetEnvironmentVariable("kekIdentifier");

            // check jdata for all required parameters
            string responseContent = "Please pass: JSON { 'databaseName' : 'EncryptedData', 'collectionID': 'UserInfo', 'dataAsJSON': 'Data', 'userID': '8'}";

            string[] requiredParams = { "databaseName", "collectionID", "dataToBeEncrypted", "userID" };
            JToken v;
            foreach (string parameter in requiredParams)
            {
                bool ready = jdata.TryGetValue(parameter, out v);
                if (!ready)
                {
                    responseContent = "Please pass: " + parameter;
                    return req.CreateResponse(HttpStatusCode.BadRequest, responseContent);
                }
            }

            // Encrypt Data
            KeyVaultRepository KVR = new KeyVaultRepository(dekIdentifier, kekIdentifier, redisConnection, applicationId, applicationSecret, keyVaultPath);

            string dataToBeEncrypted = jdata.GetValue("dataToBeEncrypted").ToString();
            var EncryptedData = await KVR.EncryptData(dataToBeEncrypted);

            // Push Encrypted Data to Cosmos
            CosmosRepository CR = new CosmosRepository(cosmosEndpoint, cosmosPrimaryKey);
            var Result = CR.UpsertUser(jdata.Value<string>("databaseName"), jdata.Value<string>("collectionID"), jdata.Value<string>("userID"), EncryptedData);

            return Result == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, responseContent)
                : req.CreateResponse(HttpStatusCode.OK, "This encrypted data was passed along your UserID: " + EncryptedData);
        }
    }
}
