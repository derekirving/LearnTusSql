#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Unify.Encryption.EncryptRoute;

public class HashIdParameter : 
    IModelBinder,
    IOutboundParameterTransformer
{

    private readonly IUnifyEncryption _encryption;

    public HashIdParameter(IUnifyEncryption encryption)
    {
        _encryption = encryption;
    }
    
    public string? TransformOutbound(object? value)
    {
        if (value == null)
        {
            return null;
        }
        
        var result = Convert.ToInt32(value);
        return _encryption.HashId(result);
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var key = bindingContext.FieldName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(key);

        if (valueProviderResult.FirstValue is not { } value)
        {
            return Task.CompletedTask;
        }

        var result = _encryption.HashId(value);
        bindingContext.Result = ModelBindingResult.Success(result);

        return Task.CompletedTask;
    }
}