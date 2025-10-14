#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Unify.Encryption.EncryptRoute;

public class EncryptParameter : IModelBinder, IOutboundParameterTransformer
{
    private readonly IUnifyEncryption _encryption;

    public EncryptParameter(IUnifyEncryption encryption)
    {
        _encryption = encryption;
    }

    public string? TransformOutbound(object? value)
    {
        var result = value?.ToString();
        
        return string.IsNullOrEmpty(result) 
            ? null 
            : _encryption.Encrypt(result);
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var key = bindingContext.FieldName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(key);

        if (valueProviderResult.FirstValue is not { } value)
        {
            return Task.CompletedTask;
        }
        
        var result = _encryption.Decrypt(value);
        bindingContext.Result = ModelBindingResult.Success(result);

        return Task.CompletedTask;
    }
}