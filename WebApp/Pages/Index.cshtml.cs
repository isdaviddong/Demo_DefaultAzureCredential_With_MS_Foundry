using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace test.Pages;

public class IndexModel : PageModel
{
    public bool Succeeded { get; private set; }
    public string Message { get; private set; } = "";

    public async Task OnGetAsync()
    {
        Response.Headers.CacheControl = "no-store";
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deployment))
        {
            Message = "請設定 AZURE_OPENAI_ENDPOINT 和 AZURE_OPENAI_DEPLOYMENT_NAME 環境變數。";
        }
        else if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
                 endpointUri.Scheme != Uri.UriSchemeHttps ||
                 !string.IsNullOrEmpty(endpointUri.UserInfo) ||
                 !string.IsNullOrEmpty(endpointUri.Query) ||
                 !string.IsNullOrEmpty(endpointUri.Fragment) ||
                 (endpointUri.AbsolutePath != "/" && endpointUri.AbsolutePath != "/openai/v1" &&
                  endpointUri.AbsolutePath != "/openai/v1/"))
        {
            Message = "AZURE_OPENAI_ENDPOINT 必須是 HTTPS 資源根端點或 /openai/v1/ 端點。";
        }
        else
        {
            try
            {
                var credential = new DefaultAzureCredential();
#pragma warning disable OPENAI001
                var chatClient = endpointUri.AbsolutePath == "/"
                    ? new AzureOpenAIClient(endpointUri, credential).GetChatClient(deployment)
                    : new ChatClient(deployment,
                        new BearerTokenPolicy(credential, "https://ai.azure.com/.default"),
                        new OpenAIClientOptions
                        {
                            Endpoint = new Uri(endpointUri.GetLeftPart(UriPartial.Authority) + "/openai/v1/")
                        });
#pragma warning restore OPENAI001
                var agent = chatClient.AsAIAgent(
                    instructions: "You are a helpful assistant.");
                await agent.RunAsync("Reply with OK.", cancellationToken: HttpContext.RequestAborted);
                Succeeded = true;
                Message = "Azure OpenAI API 呼叫成功。";
            }
            catch (AuthenticationFailedException ex)
            {
                Console.Error.WriteLine(ex);
                Message = "身分驗證失敗。請確認本機已執行 az login，或 Azure 服務已啟用受控識別。";
            }
            catch (ClientResultException ex)
            {
                Console.Error.WriteLine(ex);
                Message = ex.Status switch
                {
                    401 => "驗證遭拒絕（HTTP 401），請確認登入的身分與資源設定。",
                    403 => "沒有存取權限（HTTP 403），請確認身分的角色指派及資源網路設定。",
                    404 => "找不到資源或部署（HTTP 404），請確認端點與部署名稱。",
                    429 => "超過速率或配額限制（HTTP 429），請稍後再試。",
                    _ => $"Azure OpenAI 呼叫失敗（HTTP {ex.Status}），請查看伺服器日誌。"
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Message = "呼叫失敗，請檢查網路與設定，並查看伺服器日誌。";
            }
        }
    }
}
