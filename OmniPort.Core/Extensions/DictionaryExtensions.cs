using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Extensions
{
    public static class DictionaryExtensions
    {
        public static T? GetValueAs<T>(this IDictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value == null) return default;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return default;
        }
    }
}
