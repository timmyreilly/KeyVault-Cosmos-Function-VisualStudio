using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using KeyVaultEncryptionLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace FuncV1
{
    public static class FlushRedis
    {
        [FunctionName("FlushRedis")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // KeyVault Repository Configuration
            string applicationId = System.Environment.GetEnvironmentVariable("applicationId");
            string applicationSecret = System.Environment.GetEnvironmentVariable("applicationSecret");
            string keyVaultPath = System.Environment.GetEnvironmentVariable("keyVaultPath");
            string dekIdentifier = System.Environment.GetEnvironmentVariable("dataEncryptionKey");
            string redisConnection = System.Environment.GetEnvironmentVariable("redisConnectionString");

            // Request Body Parsing
            string requestBody = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string kekIdentifier = data?.kekIdentifier ?? System.Environment.GetEnvironmentVariable("kekIdentifier");

            // Request Body Check
            if (kekIdentifier == null)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>()
                {
                    { "kekIDentifier", kekIdentifier }
                };

                string responseContent = "Please pass: JSON";

                foreach (KeyValuePair<string, string> pair in parameters)
                {
                    if (pair.Value == null)
                        responseContent = "Please pass: " + pair.Key;
                }
                return req.CreateResponse(HttpStatusCode.BadRequest, responseContent);
            }

            // Encrypt Data
            Redis redis = new Redis(redisConnection);

            bool result = redis.FlushRedis(); 

            return result.ToString() == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + result.ToString());
        }
    }
}
