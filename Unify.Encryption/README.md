# Unify.Encryption

**NOTE** This package assumes you have run `unify config` on your project using the [Unify Cli](../../DotNetTools/Unify.Cli). If not, you will need to ensure the following in `appsettings.json`

```json
{
  "Application": {
    "MasterKey": "[A suitably long and random string]"
  }
}
```
To setup:

```c#
using Unify.Encryption;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddUnifyEncryption();
```

Which is shorthand for:

```c#
using Unify.Encryption;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IUnifyEncryption, UnifyEncryption>();
```

## Example usage

Get `IUnifyEncryption` from dependency injection. In these examples, we are using `encLib` as the variable name i.e ` var encLib = host.Services.GetRequiredService<IUnifyEncryption>();`

### Encrypt/Decrypt/Authenticate Text

```csharp

var encryptedText = encLib.Encrypt("Plain Text to Encrypt");

// Use "Salt" override
var encryptedTextWithSalt = encLib.Encrypt("Plain Text to Encrypt", "mysalt");

var decryptedText = encLib.Decrypt(encryptedText, "mysalt");
var authenticated = encLib.Authenticate(encryptedText, "mysalt");
```

### Encrypt/Decrypt a file

```csharp

var fileToEncrypt = Path.Combine(AppContext.BaseDirectory, "FileToEncrypt.txt");
var encryptedFilePath = Path.Combine(AppContext.BaseDirectory, "FiletoEncrypt.enc");
encLib.EncryptFile(fileToEncrypt, encryptedFilePath);

var fileIsAuthentic = encLib.AuthenticateFile(encryptedFilePath);
encLib.DecryptFile(encryptedFilePath, Path.Combine(AppContext.BaseDirectory, "FileToEncrypt-Decrypted.txt"));

// Use Bytes

byte[] encToBytes = encLib.EncryptFile(fileToEncrypt);
File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "FiletoEncryptFromBytes.enc"), encToBytes);
var decryptedBytes = encLib.DecryptFile(Path.Combine(AppContext.BaseDirectory, "FiletoEncryptFromBytes.enc"));
var plainText = Encoding.UTF8.GetString(decryptedBytes);

```


### Sign Bytes

```csharp
byte[] encToBytes = encLib.EncryptFile(fileToEncrypt);
var signature = encLib.Sign(encToBytes);
var validSignature = encLib.ValidateSignature(encToBytes, signature); //true

// Mess with the bytes
encToBytes[5] ^= 1;
var notValidSignature = encLib.ValidateSignature(encToBytes, signature); //false
```

### TOTP

Generate a [Time-based one-time password](https://en.wikipedia.org/wiki/Time-based_one-time_password)

```csharp
var (shortId, expiry) = encLib.GenerateTotp();
var isValid = encLib.ValidateTotp(shortId); //true

// With modifier
var (shortId, expiry) = encLib.GenerateTotp("derek.irving");
isValid = encLib.ValidateTotp(shortId); //false
var isvalidWithModifier = encLib.ValidateTotp(shortId, "derek.irving"); //true

// With length
var (shortId, expiry) = encLib.GenerateTotp("derek.irving", 8);
var isvalidWithModifierAndLength = encLib.ValidateTotp(shortId, "derek.irving", 8); //true
```
In each example, the `expiry` variable is a [DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime) object.

### Hashing

```csharp
var passwordToHash = "pa55w0rd";
var hashed = encLib.HashPassword(passwordToHash);
var hashVerified = encLib.ValidateHashedPassword(hashed, passwordToHash);
```

You can also pass a string to use as the hmac key rather than using `Application:MasterKey` or `Unify:Application:MasterKey`

```csharp
var key = "CustomValueToUseForKey";
var passwordToHash = "pa55w0rd";
var hashed = encLib.HashPassword(passwordToHash, key);
var hashVerified = encLib.ValidateHashedPassword(hashed, passwordToHash, key);
```

### HashID's

Using [hashids.net](https://github.com/ullmark/hashids.net) to generate YouTube-like IDs from numbers that can be used on the query string and thus stop displaying "hackable" URL's such as https://example.com/employees/1

```csharp
 string hash = encLib.HashId(1);
 int myInt = encLib.HashId(hash);
```

### Generate ID's

```csharp
var id = encLib.GenerateId(); // 234fh76ehd8
var idWithHyphens = encLib.GenerateId(true); //235-hg7
var idWithLength = encLib.GenerateId(true, 16); //2347dhf8-fh76ehd8
var idWithFixedMiddleDelimineter = encLib.GenerateId(true, 0, "MYBIT"); // 2Rt5n6K3-MYBIT-nvxvUp
```

### Convert

```csharp

// String <-- --> Bytes
var myString = "Hello. I'll be bytes";
var toBytes = encLib.FromStringToBytes(myString);
var backToString = encLib.FromBytesToString(toBytes);

// Base64 from String/Bytes

var toB64UrlFromString = encLib.ToBase64Url(myString);
var toB64UrlFromBytes = encLib.ToBase64Url(toBytes);
```

### Encrypt URL Parameters

In a Razor Pages or MVC application, encrypt a parameter to secure against URL Tampering and Leaking System/Business information.

Add the service:

```c#
builder.Services.AddUnifyRouteEncryption();
```

Two types of encryption are available for `id` and `string`.

#### Encrypt an id (int)

Use the `hashid` route constraint:

```c#
@page "{id:hashid?}"

@model WebApp.Pages.Index

<a asp-page="/Index" asp-route-id="1">Link To ID</a>

@if (Model.Id != null)
{
    <p>ID: @Model.Id</p>
}
```

The decrypted id is now available in the PageModel:

```c#
public class Index : PageModel
{
  [BindProperty(SupportsGet = true, BinderType = typeof(HashIdParameter))]
  public int? Id { get; set; }

    public void OnGet()
    {
        
    }
}
```

#### Encrypt a string

Use the `encrypt` route constraint:

```c#
@page "{id:encrypt?}"

@model WebApp.Pages.Index

<a asp-page="/Index" asp-route-id="assets/1">Link To Asset One</a>

@if (Model.Id != null)
{
    <p>ID: @Model.Id</p>
}
```

The decrypted string is now available in the PageModel:

```c#
public class Index : PageModel
{
    [BindProperty(SupportsGet = true, BinderType = typeof(EncryptParameter))]
    public string? Id { get; set; }
    
    public void OnGet()
    {
        
    }
}
```

**NOTE** Your IDE *may* display a warning similar to `Route parameter constraint 'hashid' is not resolved`. This can be ignored.

### Using a static instance for shared keys

In applications (or more likely other libraries) that wish to use the shared `Unify:Secret` key to encrypt/decrypt data, use the static `UnifyEncryptionProvider` passing in an `IConfiguration` object and setting `useSharedSecret: true`: 

```csharp
UnifyEncryptionProvider.Initialise(configuration, true);

var encryptionLib = UnifyEncryptionProvider.Instance;
var encryptedText = encryptionLib.Encrypt("This is plain text");
var decryptedText = encryptionLib.Decrypt(encryptedText);
```