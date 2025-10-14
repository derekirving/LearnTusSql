using Microsoft.Extensions.Configuration;
using Unify.Encryption;

namespace Unify.Web.Ui.Component.Upload;

public static class UnifyEncryptionProvider
{
    private static UnifyEncryption? _instance;
    private static readonly object Lock = new();

    public static void Initialise(IConfiguration configuration)
    {
        if (_instance != null) return;
        lock (Lock)
        {
            _instance ??= new UnifyEncryption(configuration);
        }
    }

    public static UnifyEncryption Instance => _instance ?? throw new InvalidOperationException("UnifyEncryptionProvider not initialised.");
}