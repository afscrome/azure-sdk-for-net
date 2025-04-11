// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable disable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core.TestFramework;
using NUnit.Framework;

namespace Azure.AI.Projects.Tests;

public partial class Sample_Agent_Functions_Auto_Call_Streaming : SamplesBase<AIProjectsTestEnvironment>
{
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

        FunctionToolDefinition getShortAddressTool = new(
             name: "GetShortAddresses",
             description: "Shorter addresses",
             parameters: BinaryData.FromObjectAsJson(
             new
             {
                 type = "object",
                 properties = new
                 {
                     addresses = new
                     {
                         type = "array",
                         description = "A list of addresses",
                         items = new
                         {
                             type = "object",
                             properties = new
                             {
                                 street = new
                                 {
                                     type = "string",
                                     description = "Street"
                                 },
                                 city = new
                                 {
                                     type = "string",
                                     description = "city"
                                 },
                             },
                             required = new[] { "street", "city" }
                         }
                     }
                 },
                 required = new[] { "addresses" }
             },
             new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        string[] GetShortAddresses(Dictionary<string, string>[] addresses)
        {
            string[] shortAddresses = new string[addresses.Length];
            for (int i = 0; i < addresses.Length; i++)
            {
                if (addresses[i].TryGetValue("street", out string street) && addresses[i].TryGetValue("city", out string city))
                {
                    shortAddresses[i] = $"{street}, {city}";
                }
                else
                {
                    throw new ArgumentException("Each address must contain 'street' and 'city' keys.");
                }
            }
            return shortAddresses;
        }

        FunctionToolDefinition getCurrentWeatherAtLocationTool = new(
            name: "GetWeatherAtLocation",
            description: "Gets the current weather at a provided location.",
            parameters: BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Location = new
                        {
                            Type = "string",
                            Description = "The city and state, e.g. San Francisco, CA",
                        },
                        Unit = new
                        {
                            Type = "string",
                            Enum = new[] { "c", "f" },
                        },
                    },
                    Required = new[] { "location" },
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        #region Snippet:FunctionsWithStreamingUpdateHandling
        #endregion
        #region Snippet:FunctionsWithStreaming_CreateAgent
        Agent agent = await client.CreateAgentAsync(
            model: modelDeploymentName,
            name: "SDK Test Agent - Functions",
                instructions: "You are a weather bot. Use the provided functions to help answer questions. "
                    + "Customize your responses to the user's preferences as much as possible and use friendly "
                    + "nicknames for cities whenever possible.",
            tools: [ getShortAddressTool ]
        );
        #endregion
        #region Snippet:FunctionsWithStreaming_CreateThread
        AgentThread thread = await client.CreateThreadAsync();

        ThreadMessage message = await client.CreateMessageAsync(
            thread.Id,
            MessageRole.User,
            "Shorter the address for street, 123 1st street in city, Seattle and street, 456 2nd ave in city, Bellevue");
        #endregion
        #region Snippet:FunctionsWithStreamingUpdateCycle
        List<ToolOutput> toolOutputs = new();
        Dictionary<string, Delegate> delegates = new();
        delegates.Add(nameof(GetShortAddresses), GetShortAddresses);

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
        string[] GetShortAddresses(Dictionary<string, string>[] addresses)
        {
            string[] shortAddresses = new string[addresses.Length];
            for (int i = 0; i < addresses.Length; i++)
            {
                if (addresses[i].TryGetValue("street", out string street) && addresses[i].TryGetValue("city", out string city))
                {
                    shortAddresses[i] = $"{street}, {city}";
                }
                else
                {
                    throw new ArgumentException("Each address must contain 'street' and 'city' keys.");
                }
            }
            return shortAddresses;
        }
        FunctionToolDefinition getShortAddressTool = new(
             name: "GetShortAddresses",
             description: "Shorter addresses",
             parameters: BinaryData.FromObjectAsJson(
             new
             {
                 type = "object",
                 properties = new
                 {
                     addresses = new
                     {
                         type = "array",
                         description = "A list of addresses",
                         items = new
                         {
                             type = "object",
                             properties = new
                             {
                                 street = new
                                 {
                                     type = "string",
                                     description = "Street"
                                 },
                                 city = new
                                 {
                                     type = "string",
                                     description = "city"
                                 },
                             },
                             required = new[] { "street", "city" }
                         }
                     }
                 },
                 required = new[] { "addresses" }
             },
             new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        #region Snippet:FunctionsWithStreamingSync_CreateAgent
        Agent agent = client.CreateAgent(
            model: modelDeploymentName,
            name: "SDK Test Agent - Functions",
                instructions: "You are a weather bot. Use the provided functions to help answer questions. "
                    + "Customize your responses to the user's preferences as much as possible and use friendly "
                    + "nicknames for cities whenever possible.",
            tools: [getShortAddressTool]
        );
        #endregion
        #region Snippet:FunctionsWithStreamingSync_CreateThread
        AgentThread thread = client.CreateThread();

        ThreadMessage message = client.CreateMessage(
            thread.Id,
            MessageRole.User,
            "Shorter the address for street, 123 1st street in city, Seattle and street, 456 2nd ave in city, Bellevue");
        #endregion
        #region Snippet:FunctionsWithStreamingSyncUpdateCycle
        List<ToolOutput> toolOutputs = [];
        Dictionary<string, Delegate> delegates = new();
        delegates.Add(nameof(GetShortAddresses), GetShortAddresses);
        client.EnableAutoFunctionCalls(delegates);
        foreach (StreamingUpdate streamingUpdate in client.CreateRunStreaming(thread.Id, agent.Id))
        {
            if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCreated)
            {
                Console.WriteLine("--- Run started! ---");
            }
            else if (streamingUpdate is MessageContentUpdate contentUpdate)
            {
                Console.Write(contentUpdate.Text);
            }
        }
        #endregion
        #region Snippet:FunctionsWithStreamingSync_Cleanup
        client.DeleteThread(thread.Id);
        client.DeleteAgent(agent.Id);
        #endregion
    }
}
