using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace CosmosLibrary
{
    public class CosmosRepository
    {
        private readonly string _cosmosEndpoint;
        private readonly string _cosmosPrimaryKey;
        private DocumentClient _client;

        public CosmosRepository(string cosmosEndpoint, string cosmosPrimaryKey)
        {
            _cosmosEndpoint = System.Environment.GetEnvironmentVariable("cosmosEndpoint");
            _cosmosPrimaryKey = System.Environment.GetEnvironmentVariable("cosmosPrimaryKey");
            _client = new DocumentClient(new Uri(_cosmosEndpoint), _cosmosPrimaryKey);
        }

        // UpdateOrCreate 
        public async Task UpsertUser(string databaseId, string collectionId, string userID, string data)
        {
            UserInfo user = new UserInfo();
            user.Id = userID;
            user.Data = data; 
            
            try
            {
                var resultOfUpsert = await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId), user);
            }
            catch (DocumentClientException de)
            {
                Debug.Write("Unable to write to datastore");
            }
        }

        public IQueryable<UserInfo> GetData(string databaseName, string collectionName)
        {
            // Query for all data in the given collection
            IQueryable<UserInfo> userQuery = _client.CreateDocumentQuery<UserInfo>(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collectionName));

            return userQuery;
        }

        public string PutDataInCosmos(string data, string userID)
        {
            return null; 
        }
    }
}
