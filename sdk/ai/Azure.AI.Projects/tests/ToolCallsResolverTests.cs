// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Azure.AI.Projects.Custom.Utility;

namespace Azure.AI.Projects.Tests
{
    public class ToolCallsResolverTests
    {
        private delegate string TestDelegate(string param);

        private class Address
        {
            public string Street { get; set; }
        }
        [Test]
        public void Resolve_ShouldInvokeFunctionWithCorrectArguments()
        {
            var function = new TestDelegate(param => "Resolved: " + param);
            string functionArguments = JsonSerializer.Serialize(new { param = "test" });

            var result = ToolCallsResolver.Resolve(function, functionArguments);

            Assert.AreEqual("Resolved: test", result);
        }

        [Test]
        public void Resolve_ShouldHandleMultipleParameters()
        {
            var function = new Func<string, int, string>((param1, param2) => $"{param1} {param2}");
            string functionArguments = JsonSerializer.Serialize(new { param1 = "test", param2 = 123 });

            var result = ToolCallsResolver.Resolve(function, functionArguments);

            Assert.AreEqual("test 123", result);
        }

        [Test]
        public void Resolve_ShouldHandleDefaultParameterValues()
        {
            string MyFunction(string param1, int param2 = 0)
            {
                return $"{param1} {param2}";
            }

            // Create a Func delegate pointing to the method
            Func<string, int, string> function = MyFunction;

            string functionArguments = JsonSerializer.Serialize(new { param1 = "test" });

            var result = ToolCallsResolver.Resolve(function, functionArguments);

            Assert.AreEqual("test 0", result); // Assuming default value for int is 0
        }

        [Test]
        public void Resolve_ShouldHandldDictionary()
        {
            var function = new Func<Dictionary<string, string>, string>(param => param["key"]);
            string functionArguments = JsonSerializer.Serialize(new { param = new Dictionary<string, string> { { "key", "value" } } });

            var result = ToolCallsResolver.Resolve(function, functionArguments);

            Assert.AreEqual("value", result);
        }

        [Test]
        public void Resolve_ShouldHandldArray()
        {
            var function = new Func<int[], string>(param => JsonSerializer.Serialize(param));
            string functionArguments = JsonSerializer.Serialize(new { param = new int[] { 1, 2 } });

            var result = ToolCallsResolver.Resolve(function, functionArguments);

            Assert.AreEqual("[1,2]", result);
        }

        [Test]
        public void Resolve_ShouldHandlerDeserializable()
        {
            var function = new Func<Address, string>(address => address.Street);
            string functionArguments = JsonSerializer.Serialize(new { address = new Dictionary<string, string> { { "street", "1 st" } } });

            var result = ToolCallsResolver.Resolve(function, functionArguments);

            Assert.AreEqual("1 st", result);
        }
    }
}
