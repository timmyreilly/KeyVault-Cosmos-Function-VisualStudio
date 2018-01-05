using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace KeyVaultEncryptionLibrary
{
    /// <summary>
    /// 
    /// Interfaces with KeyVault 
    /// Does Encryption of Keys 
    /// 
    /// </summary>

    public class KeyVaultRepository
    {
        Redis _redis;
        static string _kekIdentifier;
        static string _dataEncryptionKeyId;
        static string _keyVaultPath;
        private KeyVaultClient _keyVaultClient;

        public KeyVaultRepository(string dataEncryptionKeyId, string kekIdentifier, string redisConnection, string applicationId, string applicationSecret, string keyVaultPath)
        {
            _redis = new Redis(redisConnection);
            _kekIdentifier = kekIdentifier;
            _dataEncryptionKeyId = dataEncryptionKeyId;
            _keyVaultPath = keyVaultPath;
            
            _keyVaultClient = new KeyVaultClient(async (authority, resource, scope) =>
            {
                var adCredential = new ClientCredential(applicationId, applicationSecret);
                var authenticationContext = new AuthenticationContext(authority, null);
                return (await authenticationContext.AcquireTokenAsync(resource, adCredential)).AccessToken;
            });
        }

        private async Task<SecretBundle> GetSecretFromVault(string secretName)
        {
            var secretIdentifier = _keyVaultPath + "secrets/" + secretName + "/";
            try
            {
                SecretBundle secret = await _keyVaultClient.GetSecretAsync(secretIdentifier);
                return secret;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }
            return null;
        }

        public async Task<byte[]> EncryptSecretUsingKeyVault(byte[] secret, string keyId)
        {
            var keyIdentifier = _keyVaultPath + "keys/" + keyId + "/";

            var keyOpResult = await _keyVaultClient.EncryptAsync(keyIdentifier, JsonWebKeyEncryptionAlgorithm.RSAOAEP, secret);

            return keyOpResult.Result;
        }

        public async Task<byte[]> DecryptSecretUsingKeyVault(byte[] secret)
        {
            var keyIdentifierURI = _keyVaultPath + "keys/" + _kekIdentifier + "/";

            var keyOpResult = await _keyVaultClient.DecryptAsync(keyIdentifierURI, JsonWebKeyEncryptionAlgorithm.RSAOAEP, secret);

            return keyOpResult.Result;
        }

        public async Task<KeyBundle> GetKeyFromVault(string keyId)
        {
            var keyIdentifier = _keyVaultPath + "keys/" + keyId + "/";
            try
            {
                KeyBundle key = await _keyVaultClient.GetKeyAsync(keyIdentifier);
                return key;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }
            return null;
        }

        public async Task<string> DecryptStringFromBytes(string val)
        {
            var dek = await GetDEK();
            return Encryption.DecryptStringFromBytes_Aes(val, dek); 
        }

        private string GetSecret(string secretId)
        {
            return null;
        }
        
        private async Task<string> GetDEK()
        {
            // returns from redis or unwraps from keyvault
            var dek = _redis.GetValue(_dataEncryptionKeyId);

            // want to return a string that looks like this: 7165234234234234234234
            // when we call to GetSecret we'll get back a secret bundle. 
            // we need to convert the value of that bundel to a byte array to be decrypted by keyvault. 
            // then we get back a byte array that we need to convert back to string to put in redis and return for encryption. 

            if (dek == null)
            {
                var encryptedSecret = await GetSecretFromVault(_dataEncryptionKeyId);
                var decryptedSecret = await DecryptSecretUsingKeyVault(Convert.FromBase64String(encryptedSecret.Value));
                dek = Convert.ToBase64String(decryptedSecret); 
                _redis.PutInRedis(_dataEncryptionKeyId, dek);
            }

            return dek;
        }

        public async Task<string> EncryptData(string data)
        {
            // get DEK from Redis or KeyVault 
            string dek = await GetDEK();

            var encryptedData = Convert.ToBase64String(Encryption.EncryptStringToBytes_Aes(data, dek));
            return encryptedData;
        }

        public async Task<string> DecryptDataAsync(string data)
        {
            if (data != null)
            {
                string decryptedData = await DecryptStringFromBytes(data);
                return decryptedData;
            }

            return null;
        }
    }
}
