// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable disable

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core.TestFramework;
using NUnit.Framework;

namespace Azure.AI.Projects.Tests;

public partial class Sample_Agent_Functions_Auto_Call_Streaming : SamplesBase<AIProjectsTestEnvironment>
{
    private int[] GeHhumidityByAddresses(Dictionary<string, string>[] addresses)
    {
        int[] hum = new int[addresses.Length];
        for (int i = 0; i < addresses.Length; i++)
        {
            if (addresses[i].TryGetValue("city", out string city))
            {
                hum[i] = (city == "Seattle") ? 60 : 80;
            }
            else
            {
                throw new ArgumentException("Each address must contain 'street' and 'city' keys.");
            }
        }
        return hum;
    }
    private FunctionToolDefinition geHhumidityByAddressesTool = new(
         name: "GeHhumidityByAddresses",
         description: "Get humidity by street and city",
         parameters: BinaryData.FromObjectAsJson(
         new
         {
             Type = "object",
             Properties = new
             {
                 Addresses = new
                 {
                     Type = "array",
                     Description = "A list of addresses",
                     Items = new
                     {
                         Type = "object",
                         Properties = new
                         {
                             Street = new
                             {
                                 Type = "string",
                                 Description = "Street"
                             },
                             City = new
                             {
                                 Type = "string",
                                 Description = "city"
                             },
                         },
                         Required = new[] { "street", "city" }
                     }
                 }
             },
             Required = new[] { "addresses" }
         },
         new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    private string[] GetWeatherByAddresses(Dictionary<string, string>[] addresses, string unit = "F")
    {
        string[] temps = new string[addresses.Length];
        for (int i = 0; i < addresses.Length; i++)
        {
            if (addresses[i].TryGetValue("city", out string city))
            {
                temps[i] = string.Format("{0}{1}", (city == "Seattle") ? "20" : "50", unit);
            }
            else
            {
                throw new ArgumentException("Each address must contain 'street' and 'city' keys.");
            }
        }
        return temps;
    }
    private FunctionToolDefinition getWeatherByAddressesTool = new(
         name: "GetWeatherByAddresses",
         description: "Get weather by street and city",
         parameters: BinaryData.FromObjectAsJson(
         new
         {
             Type = "object",
             Properties = new
             {
                 Addresses = new
                 {
                     Type = "array",
                     Description = "A list of addresses",
                     Items = new
                     {
                         Type = "object",
                         Properties = new
                         {
                             Street = new
                             {
                                 Type = "string",
                                 Description = "Street"
                             },
                             City = new
                             {
                                 Type = "string",
                                 description = "city"
                             },
                         },
                         Required = new[] { "street", "city" }
                     }
                 },
                 Unit = new
                 {
                     Type = "string",
                     Enum = new[] { "c", "f" },
                 },
             },
             Required = new[] { "addresses" }
         },
         new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

    [Test]
    [AsyncOnly]
    public async Task AutoFunctionCallingWithStreamingExampleAsync()
    {
        #region Snippet:FunctionsWithStreaming_CreateClient
#if SNIPPET
        var connectionString = System.Environment.GetEnvironmentVariable("PROJECT_CONNECTION_STRING");
        var modelDeploymentName = System.Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME");
#else
        var connectionString = TestEnvironment.AzureAICONNECTIONSTRING;
        var modelDeploymentName = TestEnvironment.MODELDEPLOYMENTNAME;
#endif
        AgentsClient client = new(connectionString, new DefaultAzureCredential());
        #endregion

        #region Snippet:FunctionsWithStreamingUpdateHandling
        #endregion
        #region Snippet:FunctionsWithStreaming_CreateAgent
        Agent agent = await client.CreateAgentAsync(
            model: modelDeploymentName,
            name: "SDK Test Agent - Functions",
                instructions: "You are a weather bot. Use the provided functions to help answer questions. "
                    + "Customize your responses to the user's preferences as much as possible and use friendly "
                    + "nicknames for cities whenever possible.",
            tools: [ geHhumidityByAddressesTool, getWeatherByAddressesTool ]
        );
        #endregion
        #region Snippet:FunctionsWithStreaming_CreateThread
        AgentThread thread = await client.CreateThreadAsync();

        ThreadMessage message = await client.CreateMessageAsync(
            thread.Id,
            MessageRole.User,
            "Get weather and humidity for street, 123 1st St in city, Seattle and street, 456 2nd Ave in city, Bellevue");
        #endregion
        #region Snippet:FunctionsWithStreamingUpdateCycle
        List<ToolOutput> toolOutputs = new();
        Dictionary<string, Delegate> delegates = new();
        delegates.Add(nameof(GetWeatherByAddresses), GetWeatherByAddresses);
        delegates.Add(nameof(GeHhumidityByAddresses), GeHhumidityByAddresses);
        client.EnableAutoFunctionCalls(delegates);
        await foreach (StreamingUpdate streamingUpdate in client.CreateRunStreamingAsync(thread.Id, agent.Id))
        {
            if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCreated)
            {
                Console.WriteLine("--- Run started! ---");
            }
            else if (streamingUpdate is MessageContentUpdate contentUpdate)
            {
                Console.Write(contentUpdate.Text);
            }
            else if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCompleted)
            {
                Console.WriteLine();
                Console.WriteLine("--- Run completed! ---");
            }
        }

        #endregion
        #region Snippet:FunctionsWithStreaming_Cleanup
        await client.DeleteThreadAsync(thread.Id);
        await client.DeleteAgentAsync(agent.Id);
        #endregion
    }

    [Test]
    [SyncOnly]
    public void AutoFunctionCallingWithStreamingExample()
    {
#if SNIPPET
        var connectionString = System.Environment.GetEnvironmentVariable("PROJECT_CONNECTION_STRING");
        var modelDeploymentName = System.Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME");
#else
        var connectionString = TestEnvironment.AzureAICONNECTIONSTRING;
        var modelDeploymentName = TestEnvironment.MODELDEPLOYMENTNAME;
#endif
        AgentsClient client = new(connectionString, new DefaultAzureCredential());

        #region Snippet:FunctionsWithStreamingSync_CreateAgent
        Agent agent = client.CreateAgent(
            model: modelDeploymentName,
            name: "SDK Test Agent - Functions",
                instructions: "You are a weather bot. Use the provided functions to help answer questions. "
                    + "Customize your responses to the user's preferences as much as possible and use friendly "
                    + "nicknames for cities whenever possible.",
            tools: [getWeatherByAddressesTool, geHhumidityByAddressesTool]
        );
        #endregion
        #region Snippet:FunctionsWithStreamingSync_CreateThread
        AgentThread thread = client.CreateThread();

        ThreadMessage message = client.CreateMessage(
            thread.Id,
            MessageRole.User,
            "Get weather and humidity for street, 123 1st St in city, Seattle and street, 456 2nd Ave in city, Bellevue");
        #endregion
        #region Snippet:FunctionsWithStreamingSyncUpdateCycle
        List<ToolOutput> toolOutputs = [];
        Dictionary<string, Delegate> delegates = new();
        delegates.Add(nameof(GetWeatherByAddresses), GetWeatherByAddresses);
        delegates.Add(nameof(GeHhumidityByAddresses), GeHhumidityByAddresses);
        client.EnableAutoFunctionCalls(delegates);
        CollectionResult<StreamingUpdate> stream = client.CreateRunStreaming(thread.Id, agent.Id);
        foreach (StreamingUpdate streamingUpdate in stream)
        {
            if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCreated)
            {
                Console.WriteLine("--- Run started! ---");
            }
            else if (streamingUpdate is MessageContentUpdate contentUpdate)
            {
                Console.Write(contentUpdate.Text);
            }
            else if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCompleted)
            {
                Console.WriteLine();
                Console.WriteLine("--- Run completed! ---");
            }
        }
        #endregion
        #region Snippet:FunctionsWithStreamingSync_Cleanup
        client.DeleteThread(thread.Id);
        client.DeleteAgent(agent.Id);
        #endregion
    }
}
