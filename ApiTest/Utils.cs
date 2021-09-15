using System;
using System.Collections.Generic;
using JsonDiffPatchDotNet;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Tests
{

  class Utils
    {

        public static string toTitleCase(string str)
        {
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

            return myTI.ToTitleCase(str);
        }
        
        // Parse string into required type
        public static dynamic SingleParamParse(string obj, Type t)
        {
            if (obj == null)
            {
                return null;
            }
            if (t == typeof(string))
            {
                return obj;
            }
            if (t == typeof(DateTime))
            {
                return DateTime.Parse(obj);
            }

            // if parameter type is Object
            JToken jtoken = JToken.Parse(obj);

            dynamic rets = jtoken.ToObject(t);

            return rets;

        }

        public static List<dynamic> MultipleParamParse(List<string> objs, List<Type> types)
        {
            List<dynamic> ret = new List<dynamic>();
            for (int i = 0; i < types.Count; i++)
            {
                ret.Add(SingleParamParse(objs[i], types[i]));
            }
            return ret;
        }

        // compare two jsons, able to compare two JArray with the same content but different order
        public static JToken JsonDeepCompare(JToken left, JToken right)
        {
            var jdp = new JsonDiffPatch();

            JToken patch = jdp.Diff(left, right);

            return patch;
        }

        
    }

}
