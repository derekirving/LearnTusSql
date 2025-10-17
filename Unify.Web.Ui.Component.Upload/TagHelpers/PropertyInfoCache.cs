using System.Collections.Concurrent;
using System.Reflection;

namespace Unify.Web.Ui.Component.Upload.TagHelpers;

internal static class PropertyInfoCache
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> Cache = new();

    public static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
    {
        var properties = Cache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p));
        properties.TryGetValue(propertyName, out var propertyInfo);
        return propertyInfo;
    }
}