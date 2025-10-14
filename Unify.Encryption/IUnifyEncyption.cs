using System;

namespace Unify.Encryption
{
    public interface IUnifyEncryption
    {
        // See https://www.meziantou.net/cryptography-in-dotnet.htm
        // https://dev.to/stratiteq/cryptography-with-practical-examples-in-net-core-1mc4
        // https://securitydriven.net/inferno/

        string Encrypt(string plainText, string salt = "");
        string Decrypt(string cipherText, string salt = "");
        bool Authenticate(string cipherText, string salt = "");
        void EncryptFile(string inputPath, string outputPath);
        byte[] DecryptFile(string inputPath);
        byte[] EncryptFile(string inputPath);
        void DecryptFile(string inputPath, string outputPath);
        bool AuthenticateFile(string path);
        byte[] Sign(byte[] data);
        bool ValidateSignature(byte[] data, byte[] signature);
        (int,DateTime) GenerateTotp(string modifier = null, int length = 6);
        bool ValidateTotp(int totp, string modifier = null, int length = 6);
        string FromBytesToString(byte[] bytes);
        byte[] FromStringToBytes(string input);
        string ToBase64Url(byte[] input);
        string ToBase64Url(string input);
        string HashPassword(string password);
        bool ValidateHashedPassword(string hash, string password);
        long NextRandom(long minValue = long.MinValue, long maxValue = long.MaxValue);
        string RandomString(int stringLength = 10);
        string GenerateId(bool addHyphens = false, int length = 0, string fixedPart = "");
        string HashId(int id);
        int HashId(string hash);
    }
}