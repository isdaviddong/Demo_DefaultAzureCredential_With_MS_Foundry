using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

var useApiKey = false;
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deployment))
{
    Console.Error.WriteLine("請設定 AZURE_OPENAI_ENDPOINT 和 AZURE_OPENAI_DEPLOYMENT_NAME 環境變數。");
    return 1;
}

if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
    endpointUri.Scheme != Uri.UriSchemeHttps ||
    !string.IsNullOrEmpty(endpointUri.UserInfo) ||
    !string.IsNullOrEmpty(endpointUri.Query) ||
    !string.IsNullOrEmpty(endpointUri.Fragment) ||
    (endpointUri.AbsolutePath != "/" && endpointUri.AbsolutePath != "/openai/v1" &&
     endpointUri.AbsolutePath != "/openai/v1/"))
{
    Console.Error.WriteLine("AZURE_OPENAI_ENDPOINT 必須是 HTTPS 資源根端點或 /openai/v1/ 端點。");
    return 1;
}

try
{
    var isV1 = endpointUri.AbsolutePath != "/";
    var v1Options = isV1
        ? new OpenAIClientOptions { Endpoint = new Uri(endpointUri.GetLeftPart(UriPartial.Authority) + "/openai/v1/") }
        : null;
    ChatClient chatClient;
    if (useApiKey)
    {
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("請設定 AZURE_OPENAI_API_KEY 環境變數。");
        }
        chatClient = isV1
            ? new ChatClient(deployment, new ApiKeyCredential(apiKey), v1Options)
            : new AzureOpenAIClient(endpointUri, new AzureKeyCredential(apiKey)).GetChatClient(deployment);
    }
    else
    {
#pragma warning disable OPENAI001
        chatClient = isV1
            ? new ChatClient(deployment, new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"), v1Options)
            : new AzureOpenAIClient(endpointUri, new DefaultAzureCredential()).GetChatClient(deployment);
#pragma warning restore OPENAI001
    }

    var agent = chatClient.AsAIAgent(
        instructions: "You are a helpful assistant.");
    var response = await agent.RunAsync("Reply with OK.");

    Console.WriteLine("Azure OpenAI API 呼叫成功。");
    Console.WriteLine(response);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Azure OpenAI API 呼叫失敗：{ex}");
    return 1;
}
