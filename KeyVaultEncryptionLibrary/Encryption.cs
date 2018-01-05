using System;
using System.IO;
using System.Security.Cryptography;

namespace KeyVaultEncryptionLibrary
{
    public class Encryption
    {
        public static byte[] EncryptStringToBytes_Aes(string plainText, string encryptionKey)
        {
            byte[] encKeyByteArray = Convert.FromBase64String(encryptionKey);
            byte[] IV = null;
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (encKeyByteArray == null || encKeyByteArray.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = encKeyByteArray;
                aesAlg.GenerateIV();
                IV = aesAlg.IV;

                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();

                    }
                }
            }

            byte[] rv = CombineIVEncrypted(IV, encrypted);
            
            // Return the encrypted bytes from the memory stream.
            //return encrypted
            return rv;
        }

        public static string DecryptStringFromBytes_Aes(string cipher, string key)
        {
            byte[] cipherText = Convert.FromBase64String(cipher);
            byte[] Key = Convert.FromBase64String(key);
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            
            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.GenerateIV();

                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                byte[] iv = new byte[16];

                Array.Copy(cipherText, 0, iv, 0, iv.Length);
                aesAlg.IV = iv;
                byte[] cipherTextNoIV = new byte[cipherText.Length - 16];
                Array.Copy(cipherText, 16, cipherTextNoIV, 0, cipherText.Length - 16);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherTextNoIV))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting 
                            // stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        public static byte[] CombineIVEncrypted(byte[] IV, byte[] encrypted)
        {
            byte[] combined = new byte[IV.Length + encrypted.Length];
            Buffer.BlockCopy(IV, 0, combined, 0, IV.Length);
            Buffer.BlockCopy(encrypted, 0, combined, IV.Length, encrypted.Length);
            return combined;
        }
    }
}
