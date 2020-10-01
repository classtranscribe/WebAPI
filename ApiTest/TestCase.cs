using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
        public List<string> inputs { get; set; }
        public string expectations { get; set; }
        public string token { get; set; }
        public AssertInfo asserts { get; set; }


        public  TestCase(string _token,  string _api, string _method, string[] _input, string _expectation)
        {
            token = _token;
            api = _api;
            method = (Method)Enum.Parse(typeof(Method), Utils.toTitleCase(_method));
            inputs = _input.ToList();
            inputs.Add(null);
            expectations = _expectation;
        }

        public AssertInfo Run()
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

            dynamic tasks = methodInfo.Invoke(t, Utils.MultipleParamParse(inputs, pars).ToArray());

            dynamic results = tasks.GetAwaiter().GetResult();

            return AssertInfo.SingleCaseAssert(expectations, results, inputs, api, method.ToString());

        }

    }


}