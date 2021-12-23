using System;
using System.Collections;
using System.Linq;

namespace Tgs.ProfitRebate.Client
{
    public static class ClientPath
    {
        public static string AppendWithProperties(string path, object obj) => Append(path, obj);

        public static string PathAppendWithProperties(this string path, object obj) => Append(path, obj);

        private static string Append(string path, object obj)
        {
            var last = obj.GetType().GetProperties().Last();

            return obj.GetType().GetProperties().Aggregate(path,
                (current, propertyInfo) =>
                {
                    var value = propertyInfo.GetValue(obj);

                    if (propertyInfo.PropertyType == typeof(DateTimeOffset)
                        || propertyInfo.PropertyType == typeof(DateTimeOffset?))
                    {
                        value = ((DateTimeOffset?)value)?.ToUniversalTime()
                            .ToString("u");
                    }

                    if (propertyInfo.PropertyType.IsArray)
                    {
                        var query = string.Empty;
                        var enumerable = (IEnumerable)value;

                        query = enumerable.Cast<object>().Aggregate(query,
                            (current1, itme) => current1 + $"{propertyInfo.Name}={itme}&");

                        return $"{current}{query}";
                    }

                    return last.Equals(propertyInfo)
                        ? $"{current}{propertyInfo.Name}={value}"
                        : $"{current}{propertyInfo.Name}={value}&";
                });
        }
    }
}