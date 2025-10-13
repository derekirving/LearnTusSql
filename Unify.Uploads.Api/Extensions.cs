using System.Text;

namespace Unify.Uploads.Api;

public static class Extensions
{
    public static string GetValue(this string input, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(propertyName);
        
        var propertyValue = "";
        
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (!part.Trim().StartsWith(propertyName + " ")) continue;
            var propertyValueAsB64 = part.Split(' ')[1].Trim();
            var data = Convert.FromBase64String(propertyValueAsB64);
            propertyValue = Encoding.UTF8.GetString(data);
            break;
        }
        
        return propertyValue;
    }
}