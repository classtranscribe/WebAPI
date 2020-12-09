using ClassTranscribeDatabase.Models;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace ClassTranscribeServer.Utils
{


    internal static class StringExtensions
    {
        internal static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }
    }

    public class SwaggerSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {

            if (schema == null || context == null || schema.Properties.Count == 0)
            {
                return;
            }

            const BindingFlags bindingFlags = BindingFlags.Public |
                                              BindingFlags.NonPublic |
                                              BindingFlags.Instance;

            var memberList = context.Type
                                .GetFields(bindingFlags).Cast<MemberInfo>()
                                .Concat(context.Type
                                .GetProperties(bindingFlags));

            var excludedList = memberList.Where(m =>
                                                m.GetCustomAttribute<SwaggerIgnoreAttribute>()
                                                != null)
                                         .Select(m =>
                                             (m.GetCustomAttribute<JsonPropertyAttribute>()
                                              ?.PropertyName
                                              ?? m.Name.ToCamelCase()));

            foreach (var excludedName in excludedList)
            {
                if (schema.Properties.ContainsKey(excludedName))
                {
                    System.Diagnostics.Debug.WriteLine("????");
                    System.Diagnostics.Debug.WriteLine(excludedName);
                    schema.Properties.Remove(excludedName);
                }

            }
        }
    }
}
