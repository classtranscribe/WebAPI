using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Tests
{
    public class AssertInfo
    {
        public string assert;
        public string expect;
        public string output;      
        public string requestBody;
        public string diffInfo;

        public static AssertInfo SingleCaseAssert(string expectation, dynamic result, List<string> input, string api, string method)
        {

            AssertInfo ret = new AssertInfo()
            {
                expect = expectation,
                output = JsonConvert.SerializeObject(result, Formatting.Indented),
                requestBody = JsonConvert.SerializeObject(new
                {
                    api,
                    method,
                    input
                }, Formatting.Indented)
            };
            
            if (result == null && expectation == null)
            {
                ret.assert = "succeed";
                return ret;
            }
            if (result == null || expectation == null)
            {
                ret.assert = "fail";
                ret.diffInfo = "";
                return ret;
            }

            //schema matching 
            //JSchemaGenerator generator = new JSchemaGenerator();
            //generator.SchemaIdGenerationHandling = SchemaIdGenerationHandling.TypeName;
            //Console.WriteLine(result[0].GetType());
            //JSchema schema = generator.Generate(typeof(Contracts.Offering));
            //string jsonSchema = schema.ToString();


            //content matching
            JsonSerializer jsonSerializer = JsonSerializer.Create(Setting.jsonSetting); 
            JToken exp = JToken.FromObject(JsonConvert.DeserializeObject(expectation, result.GetType(), Setting.jsonSetting), jsonSerializer);
            JToken output = JToken.FromObject(result, jsonSerializer);

            JToken patch = Utils.JsonDeepCompare(exp, output);
            ret.diffInfo = patch?.ToString();
            ret.assert = patch == null? "succeed" : "fail";
            
            Console.WriteLine(ret.diffInfo);
            return ret;
        }

    }

}
