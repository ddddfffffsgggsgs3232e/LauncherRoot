using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LauncherRoot.Services;

public class MicrosoftAuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string Username { get; set; } = "";
    public string Uuid { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string AuthType { get; set; } = "microsoft";
}

public class MicrosoftAuthService : IDisposable
{
    private readonly HttpClient _http;
    private readonly ConfigService _config;

    private const string DeviceCodeUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
    private const string TokenUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string XboxAuthUrl = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XstsAuthUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string MinecraftAuthUrl = "https://api.minecraftservices.com/authentication/login_with_xbox";
    private const string MinecraftProfileUrl = "https://api.minecraftservices.com/minecraft/profile";

    private static string DefaultClientId { get; set; } = "c36a9fb6-4f2a-41ff-90bd-ae7cc92031eb";

    public MicrosoftAuthService(ConfigService config)
    {
        _config = config;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherRoot/1.0");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public void SetClientId(string clientId)
    {
        if (!string.IsNullOrWhiteSpace(clientId))
            DefaultClientId = clientId.Trim();
    }

    private string ClientIdToUse => DefaultClientId;

    public async Task<MicrosoftAuthResult> StartDeviceFlowAsync(
        Func<string, string, Task> showCodeCallback,
        Func<string, Task> updateStatusCallback)
    {
        try
        {
            await updateStatusCallback("microsoft.connecting");

            // Step 1: Get device code
            using var deviceContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", ClientIdToUse),
                new KeyValuePair<string, string>("scope", "XboxLive.signin offline_access"),
            });
            var deviceResp = await _http.PostAsync(DeviceCodeUrl, deviceContent).ConfigureAwait(false);

            if (!deviceResp.IsSuccessStatusCode)
            {
                var body = await deviceResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                _config.Log($"Cihaz kodu hatası ({deviceResp.StatusCode}): {body}");
                return Fail($"Microsoft sunucusu hata döndü ({(int)deviceResp.StatusCode}). İnternet bağlantını kontrol et veya daha sonra tekrar dene.");
            }

            var deviceJson = await deviceResp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var deviceCode = deviceJson.GetProperty("device_code").GetString()!;
            var userCode = deviceJson.GetProperty("user_code").GetString()!;
            var verificationUri = deviceJson.GetProperty("verification_uri").GetString()!;
            var interval = deviceJson.GetProperty("interval").GetInt32();

            await showCodeCallback(userCode, verificationUri);

            // Step 2: Poll for token
            string? microsoftToken = null;
            var maxAttempts = 120;
            for (var i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(interval * 1000).ConfigureAwait(false);

                try
                {
                    using var tokenContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", ClientIdToUse),
                        new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                        new KeyValuePair<string, string>("device_code", deviceCode),
                    });
                    var tokenResp = await _http.PostAsync(TokenUrl, tokenContent).ConfigureAwait(false);

                    if (tokenResp.IsSuccessStatusCode)
                    {
                        var tokenJson = await tokenResp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
                        microsoftToken = tokenJson.GetProperty("access_token").GetString();
                        break;
                    }

                    var errorJson = await tokenResp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
                    var error = errorJson.GetProperty("error").GetString();
                    if (error == "authorization_declined" || error == "expired_token")
                    {
                        await updateStatusCallback("microsoft.declined");
                        return Fail("Doğrulama iptal edildi veya süresi doldu.");
                    }
                    if (error == "slow_down")
                    {
                        interval += 5;
                    }
                }
                catch (Exception pollEx)
                {
                    _config.Log($"Token yoklama hatası (deneme {i + 1}): {pollEx.Message}");
                }
            }

            if (microsoftToken == null)
            {
                await updateStatusCallback("microsoft.timeout");
                return Fail("Doğrulama zaman aşımına uğradı. Tarayıcıdaki kodu girdiğinden emin ol.");
            }

            await updateStatusCallback("microsoft.xbox_live");

            // Step 3: Xbox Live auth
            var xboxResp = await _http.PostAsync(XboxAuthUrl, JsonContent.Create(new
            {
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = $"d={microsoftToken}"
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            })).ConfigureAwait(false);

            if (!xboxResp.IsSuccessStatusCode)
            {
                var body = await xboxResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                _config.Log($"Xbox Live hatası ({xboxResp.StatusCode}): {body}");
                return Fail("Xbox Live doğrulaması başarısız. Microsoft hesabınla tekrar dene.");
            }

            var xboxJson = await xboxResp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var xboxToken = xboxJson.GetProperty("Token").GetString()!;

            await updateStatusCallback("microsoft.xsts");

            // Step 4: XSTS auth
            var xstsResp = await _http.PostAsync(XstsAuthUrl, JsonContent.Create(new
            {
                Properties = new
                {
                    SandboxId = "RETAIL",
                    UserTokens = new[] { xboxToken }
                },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            })).ConfigureAwait(false);

            if (!xstsResp.IsSuccessStatusCode)
            {
                var body = await xstsResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                _config.Log($"XSTS hatası ({xstsResp.StatusCode}): {body}");
                return Fail("XSTS doğrulaması başarısız. Microsoft hesabınla tekrar dene.");
            }

            var xstsJson = await xstsResp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var xstsToken = xstsJson.GetProperty("Token").GetString()!;
            var userHash = xstsJson.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString()!;

            await updateStatusCallback("microsoft.minecraft_auth");

            // Step 5: Minecraft auth
            var mcResp = await _http.PostAsync(MinecraftAuthUrl, JsonContent.Create(new
            {
                identityToken = $"XBL3.0 x={userHash};{xstsToken}"
            })).ConfigureAwait(false);

            if (!mcResp.IsSuccessStatusCode)
            {
                var body = await mcResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                _config.Log($"Minecraft auth hatası ({mcResp.StatusCode}): {body}");
                return Fail("Minecraft oturumu açılamadı. Hesabında Minecraft Java Edition olduğundan emin ol.");
            }

            var mcJson = await mcResp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var mcAccessToken = mcJson.GetProperty("access_token").GetString()!;

            await updateStatusCallback("microsoft.profile");

            // Step 6: Minecraft profile
            var profileReq = new HttpRequestMessage(HttpMethod.Get, MinecraftProfileUrl);
            profileReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mcAccessToken);
            var profileResp = await _http.SendAsync(profileReq).ConfigureAwait(false);

            string username;
            string uuid;

            if (profileResp.IsSuccessStatusCode)
            {
                var profileJson = await profileResp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
                username = profileJson.GetProperty("name").GetString()!;
                uuid = profileJson.GetProperty("id").GetString()!;
            }
            else
            {
                var body = await profileResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                _config.Log($"Minecraft profil hatası ({profileResp.StatusCode}): {body}");
                return Fail("Minecraft profili alınamadı. Hesabında Minecraft Java Edition olduğundan emin ol.");
            }

            _config.Log($"Microsoft girişi başarılı: {username}");

            return new MicrosoftAuthResult
            {
                Success = true,
                Username = username,
                Uuid = uuid,
                AccessToken = mcAccessToken,
                AuthType = "microsoft"
            };
        }
        catch (HttpRequestException httpEx)
        {
            _config.Log($"Microsoft giriş hatası (HTTP): {httpEx.Message}");
            return Fail("Sunucuya bağlanılamadı. İnternet bağlantını kontrol et.");
        }
        catch (TaskCanceledException)
        {
            _config.Log("Microsoft giriş hatası: İstek zaman aşımına uğradı.");
            return Fail("Bağlantı zaman aşımına uğradı. İnternet bağlantını kontrol et.");
        }
        catch (Exception ex)
        {
            _config.Log($"Microsoft giriş hatası: {ex.Message}");
            return Fail($"Beklenmeyen hata: {ex.Message}");
        }
    }

    private static MicrosoftAuthResult Fail(string message)
    {
        return new MicrosoftAuthResult { Success = false, ErrorMessage = message };
    }

    public void Dispose()
    {
        _http?.Dispose();
        GC.SuppressFinalize(this);
    }
}
