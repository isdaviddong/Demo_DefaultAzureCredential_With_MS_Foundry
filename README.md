# Azure OpenAI 驗證測試

這是使用 .NET 與 Microsoft Agent Framework 的**測試範例**，用來比較 API key 和 `DefaultAzureCredential` 存取 Azure OpenAI 的方式。兩個程式都會實際呼叫已部署的模型，不只是取得權杖；呼叫可能產生費用。

| 專案 | 功能 | 驗證方式 |
| --- | --- | --- |
| [CredentialConsole](./CredentialConsole/Program.cs) | 在終端機顯示呼叫結果與模型回應 | 透過 `useApiKey` 切換 API key 或 `DefaultAzureCredential` |
| [WebApp](./WebApp/Pages/Index.cshtml.cs) | 在首頁顯示呼叫成功或失敗 | 僅使用 `DefaultAzureCredential` |

網站每次載入或重新整理首頁都會再次呼叫模型。此專案用於驗證，並非完整的正式環境應用程式。

使用 Microsoft Go Deploy 時，可搭配 **AI-103T00 | Module 1** 的 Lab 使用此範例。

## 事前準備

1. 安裝 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。
2. 準備 Azure OpenAI 或 Microsoft Foundry 資源，以及**支援 Chat Completions 的模型部署**。
3. 取得資源端點與模型**部署名稱**。部署名稱不一定等於模型名稱。
4. 若使用 `DefaultAzureCredential`，安裝 [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)，並確保登入身分具有資源推論權限。對 Azure OpenAI 資源，可在資源的「存取控制 (IAM)」將 **Cognitive Services OpenAI User** 角色指派給該身分。
5. 若測試 API key，準備該資源的金鑰，並確認資源允許金鑰驗證。

Microsoft Entra 驗證除了需要成功登入，也需要正確的角色指派；角色變更可能需要數分鐘才生效。

## 使用 PowerShell 設定環境變數

以下指令從此專案根目錄執行。請將範例值換成自己的資源資訊，不要保留 `<...>`。

```powershell
# 資源根端點
$env:AZURE_OPENAI_ENDPOINT = "https://<資源名稱>.openai.azure.com/"

# 模型部署名稱
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "<部署名稱>"
```

也可以改用 Microsoft Foundry 提供的 v1 端點：

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://<資源名稱>.openai.azure.com/openai/v1/"
```

兩個程式皆接受 HTTPS 資源根端點，或結尾為 `/openai/v1`、`/openai/v1/` 的端點。請勿附加查詢字串、片段或其他 API 路徑。v1 端點也可以使用資源提供的 `services.ai.azure.com` 網域。

可以檢查這兩個非機密設定：

```powershell
$env:AZURE_OPENAI_ENDPOINT
$env:AZURE_OPENAI_DEPLOYMENT_NAME
```

`$env:...` 設定只屬於**目前的 PowerShell 程序**，以及之後從該程序啟動的子程序；關閉視窗後不會永久保存。請在設定變數的同一個 PowerShell 視窗執行 `dotnet run`。修改變數後，須停止並重新啟動已執行的程式；其他終端機或已啟動的 IDE 不會自動取得新值。

### 本機使用 DefaultAzureCredential

在執行程式前登入：

```powershell
az login
```

必要時使用 `az account show` 確認租用戶與訂閱。`DefaultAzureCredential` 會依序嘗試多種可用的身分來源，例如環境設定、受控識別、Visual Studio 和 Azure CLI；因此它**不一定使用 `az login` 的身分**。若要在本機測試時限制使用 Azure CLI，可在同一個 PowerShell 視窗設定：

```powershell
$env:AZURE_TOKEN_CREDENTIALS = "AzureCliCredential"
```

不再需要此限制時可移除：

```powershell
Remove-Item Env:\AZURE_TOKEN_CREDENTIALS
```

### Console 使用 API key

僅在 Console 的 `useApiKey` 設為 `true` 時需要設定：

```powershell
$key = Read-Host "輸入 Azure OpenAI API key" -AsSecureString
$env:AZURE_OPENAI_API_KEY = [System.Net.NetworkCredential]::new("", $key).Password
Remove-Variable key
```

輸入時不會顯示金鑰，但環境變數本身仍是明文，並會被子程序繼承。請勿將金鑰寫入程式碼、README、版本控制或日誌，也不要在分享畫面時顯示環境變數。測試完成後可在同一視窗移除：

```powershell
Remove-Item Env:\AZURE_OPENAI_API_KEY
```

## 執行 Console

1. 開啟 [CredentialConsole/Program.cs](./CredentialConsole/Program.cs)，設定：

   ```csharp
   var useApiKey = false;
   ```

   - `false`：使用 `DefaultAzureCredential`。本機可先執行 `az login`，不需要 API key。
   - `true`：使用 API key，必須先設定 `AZURE_OPENAI_API_KEY`。

2. 在已設定環境變數的 PowerShell 執行：

   ```powershell
   dotnet run --project .\CredentialConsole\CredentialConsole.csproj
   ```

成功時會顯示呼叫成功及模型回應；失敗時會顯示例外原因並以非零狀態碼結束。診斷輸出可能包含環境資訊，分享前請先檢查。

## 執行 Web

網站目前**只使用 `DefaultAzureCredential`**，不會讀取 `AZURE_OPENAI_API_KEY`，也不會使用 Console 的 `useApiKey` 設定。

1. 在 PowerShell 設定端點及部署名稱，並執行 `az login`。
2. 從同一個 PowerShell 啟動：

   ```powershell
   dotnet run --project .\WebApp\test.csproj
   ```

3. 開啟終端機列出的網址，例如 `http://localhost:5125`。首頁會顯示呼叫成功或失敗；詳細例外可查看執行網站的終端機。
4. 使用 `Ctrl+C` 停止網站。

## 將 Web 部署至 Azure App Service

以下以 App Service 的**系統指派受控識別**及 Microsoft Foundry 資源為例。本機執行使用的是開發者身分；部署後必須另外啟用網站的受控識別，並授予該識別資源存取權。

### 1. 啟用 Web App 的受控識別

1. 在 Azure 入口網站開啟已部署網站的 **App Service**。
2. 選擇「設定」>「身分識別」，在「系統指派」頁籤將狀態設為「開啟」，然後儲存。
3. 記下畫面上的**物件（主體）識別碼**，以便指派角色。

![App Service 啟用系統指派受控識別](https://arock.blob.core.windows.net/blogdata202611/23-192102-1ce2fcf6-02c1-4ac5-b43e-8302028585d7.png)

### 2. 授予 Foundry 資源存取權

1. 在 Azure 入口網站開啟包含模型部署的 **Microsoft Foundry 資源**，而不是 Web App 的存取控制頁面。
2. 選擇「存取控制 (IAM)」>「新增」>「新增角色指派」，選擇 **Foundry User** 角色。
3. 在「成員」選擇剛才的 Web App 受控識別。可如圖以物件識別碼搜尋，或選擇「受控識別」後選取 Web App。
4. 完成「檢閱 + 指派」。角色指派可能需要數分鐘才生效，也需要執行操作的帳戶具備指派角色的權限。

![在 Foundry 資源中將 Foundry User 角色指派給 Web App 的受控識別](https://arock.blob.core.windows.net/blogdata202611/23-192424-d0d338d1-050c-45ca-991a-5f21eec6f097.png)

此圖示範 Foundry 資源的角色設定；直接使用 Azure OpenAI 資源時，也可依資源類型與所需權限指派 **Cognitive Services OpenAI User** 等適當角色。請確認角色的作用範圍涵蓋實際呼叫的資源。

### 3. 設定應用程式並驗證

1. 在 App Service 的「環境變數」設定 `AZURE_OPENAI_ENDPOINT` 與 `AZURE_OPENAI_DEPLOYMENT_NAME`，值分別為資源端點及模型部署名稱。這是雲端網站自己的設定，與本機 PowerShell 環境變數分開；儲存並套用變更。
2. 確認網站可連線到 Foundry 資源，然後開啟已部署的網站。網站會透過 `DefaultAzureCredential` 嘗試使用受控識別，不需要設定 API key。
3. 呼叫成功時，首頁應顯示如下畫面：

![網站顯示 Azure OpenAI API 呼叫成功](https://arock.blob.core.windows.net/blogdata202611/23-192638-c182758d-ec42-4f13-9948-894848643376.png)

若顯示失敗，請檢查 App Service 的應用程式日誌、角色指派、環境變數與資源網路限制。`DefaultAzureCredential` 仍可能優先使用其他已設定的憑證，請確認實際執行環境。

## 為什麼建議使用身分驗證？

Microsoft 建議在可行時使用 Microsoft Entra ID，而非在應用程式中管理 API key：

- **減少長期秘密**：不需要在程式碼或部署設定中保存和輪替資源金鑰。
- **權限更精細**：可以透過 Azure RBAC 對個別使用者或受控識別授予必要權限。
- **方便跨環境**：`DefaultAzureCredential` 可在本機使用開發者工具的身分，在支援受控識別的 Azure 主機上使用受控識別，無須為每個環境寫入金鑰。

正式部署時需在 Azure 主機啟用受控識別，並對該識別指派適當的資源角色。若使用使用者指派的受控識別，還需依主機設定識別資訊，例如 `AZURE_CLIENT_ID`。`DefaultAzureCredential` 是憑證鏈，可能選到其他可用身分；Microsoft 建議正式環境考慮明確使用 `ManagedIdentityCredential` 等特定憑證，以降低意外使用其他身分及探測延遲的風險。本範例保留 `DefaultAzureCredential`，便於比較與測試。

## 常見問題

- **缺少環境變數或端點格式錯誤**：確認在啟動程式的同一個 PowerShell 視窗設定變數，且端點符合上述格式。
- **身分驗證失敗**：確認已登入 Azure CLI、租用戶正確；也要檢查憑證鏈是否選用了其他身分。
- **HTTP 401 或 403**：檢查金鑰或登入身分、角色指派、資源的金鑰驗證與網路限制。取得權杖不代表已有資源存取權。
- **HTTP 404**：確認資源端點及模型部署名稱，並確認部署支援所使用的 API。
- **HTTP 429**：檢查速率限制及配額，稍後重試。

## 參考資料

- [Microsoft Agent Framework：Azure OpenAI](https://learn.microsoft.com/agent-framework/integrations/by-component/model-providers/azure-openai)
- [Azure OpenAI v1 API](https://learn.microsoft.com/azure/foundry/openai/api-version-lifecycle)
- [Azure Identity 憑證鏈](https://learn.microsoft.com/dotnet/azure/sdk/authentication/credential-chains)
- [Azure OpenAI 角色型存取控制](https://learn.microsoft.com/azure/foundry-classic/openai/how-to/role-based-access-control)
- [Microsoft Foundry 角色型存取控制](https://learn.microsoft.com/azure/foundry/concepts/rbac-foundry)
- [App Service 受控識別](https://learn.microsoft.com/azure/app-service/overview-managed-identity)
