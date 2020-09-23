using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using ClientSdk.Client;

namespace Tests
{
    public enum Method
    {
        Get,
        Post,
        Put,
        Delete
    }

    public class TestCase
    {
        
        public string api { get; set; }
        public Method method { get; set; }
        public List<List<string>> inputs { get; set; }
        public List<string> expectations { get; set; }
        public string token { get; set; }
        public List<AssertInfo> asserts { get; set; }


        public  TestCase(string _token,  string _api, string _method, List<string[]> _input, List<string> _expectation)
        {
            token = _token;
            api = _api;
            method = (Method)Enum.Parse(typeof(Method), Utils.toTitleCase(_method));
            inputs = _input.Select(i => i.ToList()).ToList();
            inputs.ForEach(i=>i.Add(null));
            expectations = _expectation;
        }

        public List<AssertInfo> Run()
        {

            MyClient t = ClientFactory.CreateTestClient(Setting.baseUrl, new HttpClient(), token);

            string[] subStrings = api.Split('/');

            string funcName = "";

            string suffix = "Async";

            foreach (string s in subStrings)
            {
                funcName += Utils.toTitleCase(s);
            }

            MethodInfo methodInfo = typeof(MyClient).GetMethod(funcName + method.ToString() + suffix) ?? typeof(MyClient).GetMethod(funcName + suffix);

            Console.WriteLine(methodInfo.Name);



            List<Type> pars = methodInfo.GetParameters().Select(p => p.ParameterType).ToList();

            List<dynamic> tasks = inputs.Select(i => methodInfo.Invoke(t, Utils.MultipleParamParse(i, pars).ToArray())).ToList();

            List<dynamic> results = tasks.Select(t => t.GetAwaiter().GetResult()).ToList();

            return AssertInfo.MultipleCaseAssert(expectations, results, inputs, api, method.ToString());

        }

    }


}