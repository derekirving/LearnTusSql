#nullable enable
using System;
using Microsoft.Extensions.Configuration;

namespace Unify.Encryption;

public static class UnifyEncryptionProvider
{
    private static UnifyEncryption? _instance;
    private static readonly object Lock = new();

    public static void Initialise(IConfiguration configuration, bool useSharedSecret = false)
    {
        if (_instance != null) return;
        lock (Lock)
        {
            _instance ??= new UnifyEncryption(configuration, useSharedSecret);
        }
    }

    public static UnifyEncryption Instance => _instance ?? throw new InvalidOperationException("UnifyEncryptionProvider not initialised.");
}