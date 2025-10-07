using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Unify.Validation.Binary;

public interface IUnifyBinaryValidator
{
    public (bool, string) Validate(Stream input, int maxLength, string providedExtension);
    public (bool, string) Validate(IFormFile input, int maxLength);
}