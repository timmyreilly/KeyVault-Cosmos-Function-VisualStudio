# Function to Interface with KeyVault and Redis 

These Functions replicate bring your own key encryption to Cosmos DB. 

You can use these functions to encrypt data using keys secured in Azure Key Vault Secrets and wrapped in Azure Key Service. 
The keys are placed in redis to reduce Azure Key Vault transactions and can be flushed with another function call. 
We've built three functions, one to encrypt data and put it into Cosmos, another to get data out of cosmos and decrypt it, and finally a third to flush the Redis cache.

They are all HTTP Trigger Based Functions. 

## Prerequisites:

### 1. Key Vault and Authorized Application
To get started with Azure Key Vault, follow the guide here: 

https://docs.microsoft.com/en-us/azure/key-vault/key-vault-get-started

- Set up Azure Key Vault
	- *Values to be used in `local.settings.json` file:*
		- `keyVaultPath` : the value of `Vault URI` from the key vault properties returned from cmdlet OR if using the Azure Portal, it'll be the value of `DNS Name` from the given Key Vault
		- `dataEncryptionKey` : the name of the secret which is used to encrypt your data and is stored in Azure Key Vault in an encrypted state
		- `kekIdentifier` : the name of the key which is used to encrypt your `dataEncryptionKey`
- Register an application with Active Directory
	- *Values to be used in `local.settings.json` file:*
		- `applicationId` : the value of `Application ID` in the App registration portion of Azure Active Directory in the portal
		- `applicationSecret` : the value of the key created in the App registration portion of Azure Active Directory in the portal
- Authorize the application to use the key or secret

### 2. Cosmos DB
- Create an Azure Cosmos DB Account
	- *Values to be used in `local.settings.json` file:*
		- `cosmosEndpoint` : value of `URI` from Cosmos DB Account >> Keys >> URI on the Azure Portal
		- `cosmosPrimaryKey`: value of `Primary Key` from Azure Cosmos DB Account >> Keys >> Primary Key on the Azure Portal

### 3. Redis
- Create a Azure Redis Cache
	- *Values to be used in `local.settings.json` file:*
		- `redisConnectionString` : the value of `Primary connection string` from Redis Cache >> Access Keys >> Primary connection string on the Azure Portal

## Setup:

1. To run clone the repo and open in Visual Studio 2018 V 15.5.1 with Azure Tools Installed. 
2. Restore NuGet Packages (right click Solution)
3. Rebuild Project 
4. Update/Add local.settings.json (refer to the Prerequisites sections for information regarding the values)

The kekIdentifier points to the Key that encrypts or "wraps" your dataEncryptionKey which is stored as a secret in Azure Key Vault in an encrypted state. 

*Sample local.settings.json:*
```JSON
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=storeafunc9520;AccountKey=secretCQ5CNZIjt0D8Vwo6hZClgnulam0bWY1GIHyZ/k3XHcbxX0qFCmIl1VAHLQXjSe0SvjFnirMREMciw==",
    "AzureWebJobsDashboard": "DefaultEndpointsProtocol=https;AccountName=storefunc9520;AccountKey=secretfogCQ5CNZIjt0D8Vwo6hZClgnulam0bWY1GIHyZ/k3XHcbxX0qFCmIl1VAHLQXjSe0SvjFnirMREMciw==",
    "applicationId": "secret1-a2aa-40ba-a621-c8320026e739",
    "applicationSecret": "secretsecretHFXsaCqvExPO2AP3tugkoJIo8cX6N9zVw=",
    "keyVaultPath": "https://kvtest.vault.azure.net/",
    "cosmosEndpoint": "https://cosmostest.documents.azure.com:443/",
    "cosmosPrimaryKey": "secretsecrettQaKtJow34S1LYtQEyEbaW7f7mXpUNs5Xm0mxTkob57V7chtAoVpX5LiuNJdTPkCmtsEL8v3w==",
    "dataEncryptionKey": "DEK",
    "kekIdentifier": "KeyOne",
    "redisConnectionString": "rediscachetest.redis.cache.windows.net:6380,password=secretdf/UcfgXeqqb5IvD6zSLMkG48oiKNzAM+T8g=,ssl=True,abortConnect=False"
  }
}
```

5. Start Debugging
6. Use Curl/Postman to POST Json to the endpoints. Will be clearly presented in CMD window that pops up. See examples below. 

## Example JSON Post bodies are as follows: 

### Encrypt Data and Store in Cosmos: 
`http://localhost:7071/api/EncryptData` 

```JSON
{
	"databaseName" : "EncryptedData",
	"collectionID": "UserInfo",
	"userID": "8", 
	"dataToBeEncrypted": "String of Data To Store"
}
```

### Get Data and Decrypt: 
`http://localhost:7071/api/GetDecryptedData`

```JSON
{
	"databaseName": "EncryptedData", 
	"collectionID": "UserInfo"
}
```

### Flush Redis: 
`http://localhost:7071/api/FlushRedis`

```JSON
{
  "no parameters": "required" 
} 
```

## TODO: 

### A couple features that would round out this project. 
- The Ability to rotate Key Encryption Keys as a function
- Better exception handling for unfound resources. 

## Feedback: 
###  We were forced to use V1 of Azure Functions for a couple reasons...
- First: 
`Package 'Microsoft.AspNet.WebApi.Client 5.2.2' was restored using '.NETFramework,Version=v4.6.1' instead of the project target framework '.NETStandard,Version=v2.0'. This package may not be fully compatible with your project.`

- Second after removing dependancy on WebApi Client:
`System.Private.CoreLib: Could not load file or assembly 'Microsoft.AspNetCore.Mvc.Abstractions, Version=2.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. Could not find or load a specific file. (Exception from HRESULT: 0x80131621). System.Private.CoreLib: Could not load file or assembly 'Microsoft.AspNetCore.Mvc.Abstractions, Version=2.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'.`

- Third we found `Microsoft.Azure.KeyVault.Core` on Nuget but that didn't work... even including prerelease. 

### Visual Studio Code support for Functions
- No clear story for building/debugging C# Projects in Visual Studio Code, but can be really fun in Node. 

## Authors
- [Tim Reilly](https://github.com/timmyreilly)
- [Elise Donkor](https://github.com/edonkor1)
- [Yutang Lin](https://github.com/yutanglin16)

## Issues?
- [Create an issue](https://github.com/timmyreilly/KeyVault-Cosmos-Function-VisualStudio/issues)