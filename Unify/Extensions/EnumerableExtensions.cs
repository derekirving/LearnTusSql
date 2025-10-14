using System.Collections.Generic;
using System.Linq;

namespace Unify.Extensions
{

    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,
            int maxItems)
        {
            return items.Select((item, index) => new { item, index })
                .GroupBy(x => x.index / maxItems)
                .Select(g => g.Select(x => x.item));
        }
        
        #if NET
        public static string ToOxfordComma(this string[] items)  
        {        
            var result = items?.Length switch  
            {  
                // three or more items  
                >=3 => $"{string.Join(", ", items[..^1])}, and {items[^1]}",  
                // 1 item or 2 items  
                not null => string.Join(" and ", items),  
                // null  
                _ => string.Empty  
            };  
    
            return result;  
        }
        #endif
    }
}