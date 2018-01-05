using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CosmosLibrary;
using KeyVaultEncryptionLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FuncV1
{
    /// <summary>
    /// 
    /// This Function retrieves the decrypted data from CosmosDB as a JSON 
    /// Uses the decrypted DEK in the Redis Cache to decrypt the data in CosmosDB
    ///  
    /// Take in parameters for database, collection, and data (for KeyVault Repository call). 
    /// Retrieves all the data from the given collection
    /// And returns the decrypted data with its ID in JSON ("id" and "Data" for this example code)
    /// 
    /// Expected JSON Request Body:
    ///     {
    ///         "databaseName": "XXXXX",
    ///         "collectionID": "XXXXX",
    ///         "kekIDentifier": "XXXXX"
    ///     }
    ///     
    /// </summary>

    public static class GetDecryptedData
    {
        [FunctionName("GetDecryptedData")]
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


            string responseContent = "Please pass: JSON { 'databaseName' : 'EncryptedData', 'collectionID': 'UserInfo'}";
            string[] requiredParams = { "databaseName", "collectionID" };
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

            KeyVaultRepository KVR = new KeyVaultRepository(dekIdentifier, kekIdentifier, redisConnection, applicationId, applicationSecret, keyVaultPath);
            CosmosRepository CR = new CosmosRepository(cosmosEndpoint, cosmosPrimaryKey);

            IQueryable<UserInfo> dataResult = CR.GetData(jdata.Value<string>("databaseName"), jdata.Value<string>("collectionID"));
            StringBuilder dataString = new StringBuilder();

            var collectionData = new JArray();

            foreach (UserInfo user in dataResult)
            {
                dynamic userT = new JObject();
                userT.Id = user.Id;
                var rawData = await KVR.DecryptDataAsync(user.Data);
                // convert rawData to JSON if possible. 
                try
                {
                    dynamic j = JsonConvert.DeserializeObject(rawData);
                    userT.Data = j;
                }
                catch (Exception)
                {
                    userT.Data = rawData; 
                }
                    
                collectionData.Add(userT);
            }

            return collectionData == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, responseContent)
                : req.CreateResponse(HttpStatusCode.OK, collectionData);
        }
    }
}
