using System;
using System.IO;
using System.Security.Cryptography;
using HashidsNet;
using Microsoft.Extensions.Configuration;
using SecurityDriven.Inferno;
using SecurityDriven.Inferno.Extensions;
using SecurityDriven.Inferno.Otp;
using Unify.Encryption.EasyCrypto;

namespace Unify.Encryption
{
    public class UnifyEncryption : IUnifyEncryption
    {
        private readonly IConfiguration _configuration;
        private readonly byte[] _masterKeyBytes;
        private readonly string _masterKey;
        private readonly Hashids _hashIds;
        private readonly CryptoRandom _cryptoRandom = new CryptoRandom();

        public UnifyEncryption(IConfiguration configuration)
        {
            _configuration = configuration;
            var masterKey = configuration["Application:MasterKey"];

            if(string.IsNullOrEmpty(masterKey))
            {
                // Backwards compat
                masterKey = configuration["Unify:Application:MasterKey"];
            }

            if (string.IsNullOrEmpty(masterKey)) throw new ArgumentException("Neither Application:MasterKey or Unify:Application:MasterKey found in config");

            _masterKey = masterKey;
            _masterKeyBytes = Utils.SafeUTF8.GetBytes(masterKey);

            _hashIds = new Hashids(_masterKey);
        }

        public string Encrypt(string plainText, string salt = "")
        {
            var inputBytes = new ArraySegment<byte>(plainText.ToBytes());

            byte[] encrypted;

            if (!string.IsNullOrEmpty(salt))
            {
                var saltBytes = new ArraySegment<byte>(salt.ToBytes());
                encrypted = SuiteB.Encrypt(_masterKeyBytes, inputBytes, saltBytes);
            }
            else
            {
                encrypted = SuiteB.Encrypt(_masterKeyBytes, inputBytes);
            }

            return encrypted.ToB64();
        }

        public string Decrypt(string cipherText, string salt = "")
        {
            var inputBytes = new ArraySegment<byte>(cipherText.FromB64());

            byte[] decrypted;

            if (!string.IsNullOrEmpty(salt))
            {
                var saltBytes = new ArraySegment<byte>(salt.ToBytes());
                decrypted = SuiteB.Decrypt(_masterKeyBytes, inputBytes, saltBytes);
            }
            else
            {
                decrypted = SuiteB.Decrypt(_masterKeyBytes, inputBytes);
            }

            return decrypted.FromBytes();
        }

        public bool Authenticate(string cipherText, string salt = "")
        {
            var inputBytes = new ArraySegment<byte>(cipherText.FromB64());

            bool authenticated;
            if (!string.IsNullOrEmpty(salt))
            {
                var saltBytes = new ArraySegment<byte>(salt.ToBytes());
                authenticated = SuiteB.Authenticate(_masterKeyBytes, inputBytes, saltBytes);
            }
            else
            {
                authenticated = SuiteB.Authenticate(_masterKeyBytes, inputBytes);
            }

            return authenticated;
        }

        public void EncryptFile(string inputPath, string outputPath)
        {
            using var originalStream = new FileStream(inputPath, FileMode.Open);
            using var encryptedStream = new FileStream(outputPath, FileMode.Create);
            using var encTransform = new EtM_EncryptTransform(_masterKeyBytes);
            using var cryptoStream = new CryptoStream(encryptedStream, encTransform, CryptoStreamMode.Write);
            originalStream.CopyTo(cryptoStream);
        }

        public byte[] EncryptFile(string inputPath)
        {
            using var originalStream = new FileStream(inputPath, FileMode.Open);
            using var encryptedStream = new MemoryStream();
            using var decTransform = new EtM_EncryptTransform(_masterKeyBytes);
            using var cryptoStream = new CryptoStream(originalStream, decTransform, CryptoStreamMode.Read);
            cryptoStream.CopyTo(encryptedStream);
            return encryptedStream.ToArray();
        }
        public byte[] DecryptFile(string inputPath)
        {
            using var encryptedStream = new FileStream(inputPath, FileMode.Open);
            using var decryptedStream = new MemoryStream();
            using var decTransform = new EtM_DecryptTransform(_masterKeyBytes);
            using var cryptoStream = new CryptoStream(encryptedStream, decTransform, CryptoStreamMode.Read);
            cryptoStream.CopyTo(decryptedStream);
            return decryptedStream.ToArray();
        }

        public void DecryptFile(string inputPath, string outputPath)
        {
            using var encryptedStream = new FileStream(inputPath, FileMode.Open);
            using var decryptedStream = new FileStream(outputPath, FileMode.Create);
            using var decTransform = new EtM_DecryptTransform(_masterKeyBytes);
            using (var cryptoStream = new CryptoStream(encryptedStream, decTransform, CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(decryptedStream);
            }

            if (!decTransform.IsComplete) throw new Exception("Not all blocks are decrypted.");
        }

        public bool AuthenticateFile(string path)
        {
            try
            {
                using var encryptedStream = new FileStream(path, FileMode.Open);
                using var decTransform = new EtM_DecryptTransform(_masterKeyBytes, authenticateOnly: true);
                using (var cryptoStream = new CryptoStream(encryptedStream, decTransform, CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(Stream.Null);
                }

                return decTransform.IsComplete;
            }
            catch
            {
                return false;
            }
        }

        public byte[] Sign(byte[] data)
        {
            var privateKey = _configuration["Unify:Application:RsaKeys:PrivateKey"];
            if (string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException("Unify:Application:RsaKeys:PrivateKey not found in config");
            }

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKey);
            
            byte[] hash;
            using (var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(data);
            }

            var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
            rsaFormatter.SetHashAlgorithm("SHA256");
            return rsaFormatter.CreateSignature(hash);
        }

        public bool ValidateSignature(byte[] data, byte[] signature)
        {
            var publicKey = _configuration["Unify:Application:RsaKeys:PublicKey"];
            
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("Unify:Application:RsaKeys:PublicKey not found in config");
            }

            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKey);
            
            byte[] hash;
            using (var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(data);
            }

            var rsaFormatter = new RSAPKCS1SignatureDeformatter(rsa);
            rsaFormatter.SetHashAlgorithm("SHA256");
            return rsaFormatter.VerifySignature(hash, signature);
        }

        public (int,DateTime) GenerateTotp(string modifier = null, int length = 6)
        {
            var code = TOTP.GenerateTOTP(_masterKeyBytes, null, length, modifier);
            var expiry = TOTP.GetExpiryTime();
            return (code, expiry);
        }

        public bool ValidateTotp(int totp, string modifier = null, int length = 6)
        {
            return TOTP.ValidateTOTP(_masterKeyBytes, totp, null, length, modifier);
        }

        public string FromBytesToString(byte[] bytes)
        {
            return Utils.SafeUTF8.GetString(bytes);
        }

        public byte[] FromStringToBytes(string input)
        {
            return Utils.SafeUTF8.GetBytes(input);
        }

        public string ToBase64Url(byte[] input)
        {
            return input.ToB64Url();
        }

        public string ToBase64Url(string input)
        {
            return FromStringToBytes(input).ToB64Url();
        }

        public string HashPassword(string password)
        {
            var data = Utils.SafeUTF8.GetBytes(password);
            using var hmac = SuiteB.HmacFactory();
            hmac.Key = _masterKeyBytes;
            return hmac.ComputeHash(data).ToBase16();
        }
        
        public string HashPassword(string password, string key)
        {
            var data = Utils.SafeUTF8.GetBytes(password);
            using var hmac = SuiteB.HmacFactory();
            hmac.Key = Utils.SafeUTF8.GetBytes(key);
            return hmac.ComputeHash(data).ToBase16();
        }

        public bool ValidateHashedPassword(string hash, string password)
        {
            var data = Utils.SafeUTF8.GetBytes(password);
            using var hmac = SuiteB.HmacFactory();
            hmac.Key = _masterKeyBytes;
            return hmac.ComputeHash(data).ToBase16().Equals(hash);
        }
        
        public bool ValidateHashedPassword(string hash, string password, string key)
        {
            var data = Utils.SafeUTF8.GetBytes(password);
            using var hmac = SuiteB.HmacFactory();
            hmac.Key = Utils.SafeUTF8.GetBytes(key);
            return hmac.ComputeHash(data).ToBase16().Equals(hash);
        }

        public long NextRandom(long minValue = long.MinValue, long maxValue = long.MaxValue)
        {
            return _cryptoRandom.NextLong(minValue, maxValue);
        }

        public string RandomString(int stringLength = 10)
        {
            const string allowedChars = "qwertyuiopasdfghjklzxcvbnm1234567890QWERTYUIOPASDFGHJKLZXCVBNM";
            var chars = new char[stringLength];
            var rd = new Random();

            for (var i = 0; i < stringLength; i++)
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];

            return new string(chars);
        }

        public string GenerateId(bool addHyphens = false, int length = 0, string fixedPart = "")
        {
            var gen = !string.IsNullOrEmpty(fixedPart)
                ? new IdGenerator(fixedPart, true) {AddHyphens = addHyphens}
                : new IdGenerator {AddHyphens = addHyphens};

            if (length > 0) gen.RandomPartLength = length;

            return gen.NewId(DateTime.UtcNow);
        }

        public string HashId(int id) => _hashIds.Encode(id);

        public int HashId(string hash)
        {
            try
            {
                return _hashIds.Decode(hash)[0];
            }
            catch
            {
                return 0;
            }
        }
    }
}