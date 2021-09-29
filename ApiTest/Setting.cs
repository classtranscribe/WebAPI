using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace Tests
{

    class Setting
    {
        public static JsonSerializerSettings jsonSetting =  new JsonSerializerSettings
        {
            NullValueHandling = ignoreNullValue == "true" ? NullValueHandling.Ignore : NullValueHandling.Include,
            ContractResolver = new CompareContractResolver(),
            //DefaultValueHandling = DefaultValueHandling.Include
        };

        // ignoreNullValue == "true" if ignore properties whose value is null
        public static string ignoreNullValue = "false";

        public static string baseUrl = "https://localhost:5001/";

        public static List<string> ignoreProperties = new List<string>();

        public class CompareContractResolver : DefaultContractResolver
        {
            // Filter out ignored properties given by user
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var jsonProperty = base.CreateProperty(member, memberSerialization);
                if (ignoreProperties.Count!=0 && ignoreProperties.Contains(jsonProperty.PropertyName))
                {
                    jsonProperty.ShouldSerialize = instance => { return false; };
                }
                jsonProperty.NullValueHandling = NullValueHandling.Include;
                return jsonProperty;
            }
        }

    }

}
