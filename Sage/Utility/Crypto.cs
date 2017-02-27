/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace Highpoint.Sage.Utility {
    /// <summary>
    /// A class of static Cryptography helper functions.
    /// </summary>
    public class Crypto {
        /// <summary>
        /// Triple-DES encrypts the plain text using the provided key.
        /// </summary>
        /// <param name="plainText">The plain text to be encrypted.</param>
        /// <param name="key">The key to use in encrypting the text.</param>
        /// <returns>The cipher text string resultant from the encryption.</returns>
        public static string EncryptString(string plainText, string key) {
            TripleDESCryptoServiceProvider tripProvider = new TripleDESCryptoServiceProvider();
            UnicodeEncoding uEncode = new UnicodeEncoding();
            // stores plaintext as a byte array
            byte[] bytePlainText = uEncode.GetBytes(plainText);
            // create a memory stream to hold encrypted text
            MemoryStream cipherText = new MemoryStream();
            // private key
            byte[] slt = new byte[0];
            Rfc2898DeriveBytes passwordderiveBytes = new Rfc2898DeriveBytes(key, slt);
            byte[] byteDeriveKey = passwordderiveBytes.GetBytes(24);
            tripProvider.Key = byteDeriveKey;
            // initialization vector is the encryption seed
            tripProvider.IV = passwordderiveBytes.GetBytes(8);
            // create a cryto-writer to encrypt the bytearray
            // into a stream
            CryptoStream encrypted = new CryptoStream(cipherText, tripProvider.CreateEncryptor(), CryptoStreamMode.Write);
            encrypted.Write(bytePlainText, 0, bytePlainText.Length);
            encrypted.FlushFinalBlock();
            // return result as a Base64 encoded string
            return Convert.ToBase64String(cipherText.ToArray());
        }

        /// <summary>
        /// Triple-DES decrypts the cipher text using the provided key.
        /// </summary>
        /// <param name="cipherText">The cipher text to be decrypted.</param>
        /// <param name="key">The key to use in decrypting the text.</param>
        /// <returns>The plain text resultant from the decryption.</returns>
        public static string DecryptString(string cipherText, string key) {
            TripleDESCryptoServiceProvider tripProvider = new TripleDESCryptoServiceProvider();
            UnicodeEncoding uEncode = new UnicodeEncoding();
            // stores ciphertext as a byte array
            byte[] byteCipherText = Convert.FromBase64String(cipherText);
            // create a memory stream to hold encrypted text
            MemoryStream plainText = new MemoryStream();
            MemoryStream cipherTextStream = new MemoryStream(byteCipherText);
            // private key
            byte[] slt = new byte[0];
            Rfc2898DeriveBytes passwordderiveBytes = new Rfc2898DeriveBytes(key, slt);
            byte[] byteDeriveKey = passwordderiveBytes.GetBytes(24);
            tripProvider.Key = byteDeriveKey;
            // initialization vector is the encryption seed
            tripProvider.IV = passwordderiveBytes.GetBytes(8);
            // create a cryto-stream decoder to decode
            // a cipher text stream into a plain text stream
            CryptoStream decrypted = new CryptoStream(cipherTextStream, tripProvider.CreateDecryptor(), CryptoStreamMode.Read);
            StreamWriter writer = new StreamWriter(plainText);
            StreamReader reader = new StreamReader(decrypted);
            writer.Write(reader.ReadToEnd());
            // clean up afterwards
            writer.Flush();
            decrypted.Clear();
            tripProvider.Clear();
            // return result as a Base64 encoded string
            return uEncode.GetString(plainText.ToArray());
        }

        /// <summary>
        /// Generates a hash for the given plain text value and returns a
        /// base64-encoded result. Before the hash is computed, a random salt
        /// is generated and appended to the plain text. This salt is stored at
        /// the end of the hash value, so it can be used later for hash
        /// verification.
        /// 
        /// </summary>
        /// <param name="plainText">
        /// Plaintext value to be hashed. The function does not check whether
        /// this parameter is null.
        /// </param>
        /// <param name="hashAlgorithm">
        /// Name of the hash algorithm. Allowed values are: "MD5", "SHA1",
        /// "SHA256", "SHA384", and "SHA512" (if any other value is specified
        /// MD5 hashing algorithm will be used). This value is case-insensitive.
        /// </param>
        /// <param name="saltBytes">
        /// Salt bytes. This parameter can be null, in which case a random salt
        /// value will be generated.
        /// </param>
        /// <returns>
        /// Hash value formatted as a base64-encoded string.
        /// </returns>
        public static string ComputeHash(string plainText, string hashAlgorithm, byte[] saltBytes) {
            /*
             
             low-footprint : 
             System.Security.Cryptography.HashAlgorithm hash = new System.Security.Cryptography.SHA1Managed();
             string hashValue = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(line)));
             
             */

            // If salt is not specified, generate it on the fly.
            if (saltBytes == null) {
                // Define min and max salt sizes.
                int minSaltSize = 4;
                int maxSaltSize = 8;

                // Generate a random number for the size of the salt.
                Random random = new Random();
                int saltSize = random.Next(minSaltSize, maxSaltSize);

                // Allocate a byte array, which will hold the salt.
                saltBytes = new byte[saltSize];

                // Initialize a random number generator.
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                // Fill the salt with cryptographically strong byte values.
                rng.GetNonZeroBytes(saltBytes);
            }

            // Convert plain text into a byte array.
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Allocate array, which will hold plain text and salt.
            byte[] plainTextWithSaltBytes =
                    new byte[plainTextBytes.Length + saltBytes.Length];

            // Copy plain text bytes into resulting array.
            for (int i = 0; i < plainTextBytes.Length; i++)
                plainTextWithSaltBytes[i] = plainTextBytes[i];

            // Append salt bytes to the resulting array.
            for (int i = 0; i < saltBytes.Length; i++)
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];

            // Because we support multiple hashing algorithms, we must define
            // hash object as a common (abstract) base class. We will specify the
            // actual hashing algorithm class later during object creation.
            HashAlgorithm hash;

            // Make sure hashing algorithm name is specified.
            if (hashAlgorithm == null)
                hashAlgorithm = "";

            // Initialize appropriate hashing algorithm class.
            switch (hashAlgorithm.ToUpper()) {
                case "SHA1":
                    hash = new SHA1Managed();
                    break;

                case "SHA256":
                    hash = new SHA256Managed();
                    break;

                case "SHA384":
                    hash = new SHA384Managed();
                    break;

                case "SHA512":
                    hash = new SHA512Managed();
                    break;

                default:
                    hash = new MD5CryptoServiceProvider();
                    break;
            }

            // Compute hash value of our plain text with appended salt.
            byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

            // Create array which will hold hash and original salt bytes.
            byte[] hashWithSaltBytes = new byte[hashBytes.Length +
                                                saltBytes.Length];

            // Copy hash bytes into resulting array.
            for (int i = 0; i < hashBytes.Length; i++)
                hashWithSaltBytes[i] = hashBytes[i];

            // Append salt bytes to the result.
            for (int i = 0; i < saltBytes.Length; i++)
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

            // Convert result into a base64-encoded string.
            string hashValue = Convert.ToBase64String(hashWithSaltBytes);

            // Return the result.
            return hashValue;
        }

        /// <summary>
        /// Compares a hash of the specified plain text value to a given hash
        /// value. Plain text is hashed with the same salt value as the original
        /// hash.
        /// </summary>
        /// <param name="plainText">
        /// Plain text to be verified against the specified hash. The function
        /// does not check whether this parameter is null.
        /// </param>
        /// <param name="hashAlgorithm">
        /// Name of the hash algorithm. Allowed values are: "MD5", "SHA1", 
        /// "SHA256", "SHA384", and "SHA512" (if any other value is specified,
        /// MD5 hashing algorithm will be used). This value is case-insensitive.
        /// </param>
        /// <param name="hashValue">
        /// Base64-encoded hash value produced by ComputeHash function. This value
        /// includes the original salt appended to it.
        /// </param>
        /// <returns>
        /// If computed hash mathes the specified hash the function the return
        /// value is true; otherwise, the function returns false.
        /// </returns>
        public static bool VerifyHash(string plainText, string hashAlgorithm, string hashValue) {
            // Convert base64-encoded hash value into a byte array.
            byte[] hashWithSaltBytes = Convert.FromBase64String(hashValue);

            // We must know size of hash (without salt).
            int hashSizeInBits;

            // Make sure that hashing algorithm name is specified.
            if (hashAlgorithm == null)
                hashAlgorithm = "";

            // Size of hash is based on the specified algorithm.
            switch (hashAlgorithm.ToUpper()) {
                case "SHA1":
                    hashSizeInBits = 160;
                    break;

                case "SHA256":
                    hashSizeInBits = 256;
                    break;

                case "SHA384":
                    hashSizeInBits = 384;
                    break;

                case "SHA512":
                    hashSizeInBits = 512;
                    break;

                default: // Must be MD5
                    hashSizeInBits = 128;
                    break;
            }

            // Convert size of hash from bits to bytes.
            int hashSizeInBytes = hashSizeInBits / 8;

            // Make sure that the specified hash value is long enough.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
                return false;

            // Allocate array to hold original salt bytes retrieved from hash.
            byte[] saltBytes = new byte[hashWithSaltBytes.Length -
                                        hashSizeInBytes];

            // Copy salt from the end of the hash to the new array.
            for (int i = 0; i < saltBytes.Length; i++)
                saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];

            // Compute a new hash string.
            string expectedHashString =
                        ComputeHash(plainText, hashAlgorithm, saltBytes);

            // If the computed hash matches the specified hash,
            // the plain text value must be correct.
            return (hashValue == expectedHashString);
        }

        /// <summary>
        /// Gets the key Blob.
        /// </summary>
        /// <returns>The key Blob.</returns>
        public static byte[] GetKeyBlob() {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();         
            return rsa.ExportCspBlob(true);
        }

        /// <summary>
        /// Generates the signature.
        /// </summary>
        /// <param name="hashValue">The hash value.</param>
        /// <param name="cspKeyBlob">The CSP key Blob of the signer.</param>
        /// <returns></returns>
        public static byte[] GenerateSignature(byte[] hashValue, byte[] cspKeyBlob) {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(cspKeyBlob);
            RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
            rsaFormatter.SetHashAlgorithm("SHA1");
            return rsaFormatter.CreateSignature(hashValue);
        }

        /// <summary>
        /// Validates the signature.
        /// </summary>
        /// <param name="hashValue">The hash value.</param>
        /// <param name="signedHashValue">The signed hash value.</param>
        /// <param name="cspKeyBlob">The CSP key Blob of the asserted signer.</param>
        /// <returns></returns>
        public static bool ValidateSignature(byte[] hashValue, byte[] signedHashValue, byte[] cspKeyBlob) {

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(cspKeyBlob);
            RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
            rsaDeformatter.SetHashAlgorithm("SHA1");
            return rsaDeformatter.VerifySignature(hashValue, signedHashValue);
        }

        /// <summary>
        /// Generates a 1024 bit RSA key pair string.
        /// </summary>
        /// <returns>A 1024 bit RSA key pair string.</returns>
        public static string GenerateFullKeyPair() => GenerateFullKeyPair(1024);

        /// <summary>
        /// Generates a RSA key pair string.
        /// </summary>
        /// <param name="keySize">Size of the key.</param>
        /// <returns>A 1024 bit RSA key pair string.</returns>
        public static string GenerateFullKeyPair(int keySize) {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize);
            string keyPair = rsa.ToXmlString(true);
            return keyPair;
        }

        /// <summary>
        /// Accepts a full keyPair string (as generated by the GenerateKeyPair API) and returns a keyPair string without the private key.
        /// </summary>
        /// <param name="fullKeyPair">The full key pair.</param>
        /// <returns></returns>
        public static string PublicKeyOnly(string fullKeyPair) {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(fullKeyPair);
            string pko = rsa.ToXmlString(false);
            return pko;
        }

        ///// <summary>
        ///// Generates a cryptographic key pair, and saves it in the named container.
        ///// </summary>
        ///// <param name="ContainerName">Name of the container.</param>
        //public static void GenKey_SaveInContainer(string ContainerName) {
        //    // Create the CspParameters object and set the key container 
        //    // name used to store the RSA key pair.
        //    CspParameters cp = new CspParameters();
        //    cp.KeyContainerName = ContainerName;

        //    // Create a new instance of RSACryptoServiceProvider that accesses
        //    // the key container MyKeyContainerName.
        //    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cp);

        //    // Display the key information to the console.
        //    Console.WriteLine("Key added to container: \n  {0}", rsa.ToXmlString(true));
        //}

        //public static void GetKeyFromContainer(string ContainerName) {
        //    RSACryptoServiceProvider csp = new RSACryptoServiceProvider();

        //                CspParameters cp = new CspParameters();
        //    cp.KeyContainerName = ContainerName;

        //    cp.

        //    csp.
        //}

        //public static void DeleteKeyFromContainer(string ContainerName) {
        //    // Create the CspParameters object and set the key container 
        //    // name used to store the RSA key pair.
        //    CspParameters cp = new CspParameters();
        //    cp.KeyContainerName = ContainerName;

        //    // Create a new instance of RSACryptoServiceProvider that accesses
        //    // the key container.
        //    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(cp);

        //    // Delete the key entry in the container.
        //    rsa.PersistKeyInCsp = false;

        //    // Call Clear to release resources and delete the key from the container.
        //    rsa.Clear();

        //    Console.WriteLine("Key deleted.");
        //}
    }
}