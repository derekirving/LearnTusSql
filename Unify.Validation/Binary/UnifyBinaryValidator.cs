using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using MimeDetective;
using MimeDetective.Definitions.Licensing;
using MimeDetective.Storage;

namespace Unify.Validation.Binary;

public sealed class UnifyBinaryValidator : IUnifyBinaryValidator
{
    private readonly IContentInspector _inspector;
    
    public UnifyBinaryValidator(IEnumerable<string> types)
    {
        if (types == null)
        {
            _inspector = new ContentInspectorBuilder
            {
                Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
            }.Build();
            
            return;
        }
        
        var allDefinitions = new MimeDetective.Definitions.ExhaustiveBuilder() { 
            UsageType = UsageType.PersonalNonCommercial
        }.Build();
        
        var scopedDefinitions = allDefinitions
            .ScopeExtensions(types.ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase)) //Limit results to only the extensions provided
            .TrimMeta() //If you don't care about the meta information (definition author, creation date, etc)
            .TrimDescription() //If you don't care about the description
            .TrimMimeType() //If you don't care about the mime type
            .ToImmutableArray();
        
        _inspector = new ContentInspectorBuilder
        {
            Definitions = scopedDefinitions
        }.Build();
    }
    
    public (bool, string) Validate(Stream input, int maxLength, string providedExtension)
    {
        
        var content = ContentReader.Default.ReadFromStream(input);
            
        if (content.Length == 0)
        {
            return (false, "Empty file");
        }

        if (content.Length > maxLength)
        {
            return (false, "Too Large");
        }
            
        var allResults = _inspector.Inspect(content).ByFileExtension();
            
        var results = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        if (allResults.Length > 0)
        {
            var maxPoints = allResults.First().Points;
            results.UnionWith(
                from x in allResults
                where x.Points == maxPoints
                select x.Extension
            );
        }

        var expected = providedExtension.Split('.').LastOrDefault()?.ToLower() ?? string.Empty;
        var isGood = results.Contains(expected);

        return isGood ? (true, "") : (false, "Mime-Type mismatch");
    }

    public (bool, string) Validate(IFormFile input, int maxLength)
    {
        var content = ContentReader.Default.ReadFromStream(input.OpenReadStream());

        if (content.Length == 0)
        {
            return (false, "Empty file");
        }

        if (content.Length > maxLength)
        {
            return (false, "Too Large");
        }
            
        var allResults = _inspector.Inspect(content).ByFileExtension();
            
        var results = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        if (allResults.Length > 0)
        {
            var maxPoints = allResults.First().Points;
            results.UnionWith(
                from x in allResults
                where x.Points == maxPoints
                select x.Extension
            );
        }

        var expected = Path.GetExtension(input.FileName)?.ToLowerInvariant() ?? string.Empty;
        var isGood = results.Contains(expected);

        return isGood ? (true, "") : (false, "Mime-Type mismatch");
    }
}