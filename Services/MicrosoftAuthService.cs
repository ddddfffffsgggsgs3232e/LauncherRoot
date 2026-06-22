using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LauncherRoot.Services;

public class MicrosoftAuthResult
{
    public string Username { get; set; } = "";
    public string Uuid { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string AuthType { get; set; } = "microsoft";
}

public class MicrosoftAuthService : IDisposable
{
    private readonly HttpClient _http;
    private readonly ConfigService _config;

    private const string ClientId = "00000000402B5328";

    private const string DeviceCodeUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
    private const string TokenUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string XboxAuthUrl = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XstsAuthUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string MinecraftAuthUrl = "https://api.minecraftservices.com/authentication/login_with_xbox";
    private const string MinecraftProfileUrl = "https://api.minecraftservices.com/minecraft/profile";

    public MicrosoftAuthService(ConfigService config)
    {
        _config = config;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherRoot/1.0");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<MicrosoftAuthResult?> StartDeviceFlowAsync(
        Func<string, string, Task> showCodeCallback,
        Func<string, Task> updateStatusCallback)
    {
        try
        {
            await updateStatusCallback("Microsoft hesabına bağlanılıyor...");

            // Step 1: Get device code
            var deviceResp = await _http.PostAsync(DeviceCodeUrl, new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("scope", "XboxLive.signin offline_access"),
            }));

            deviceResp.EnsureSuccessStatusCode();
            var deviceJson = await deviceResp.Content.ReadFromJsonAsync<JsonElement>();
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
                await Task.Delay(interval * 1000);

                try
                {
                    var tokenResp = await _http.PostAsync(TokenUrl, new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", ClientId),
                        new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                        new KeyValuePair<string, string>("device_code", deviceCode),
                    }));

                    if (tokenResp.IsSuccessStatusCode)
                    {
                        var tokenJson = await tokenResp.Content.ReadFromJsonAsync<JsonElement>();
                        microsoftToken = tokenJson.GetProperty("access_token").GetString();
                        break;
                    }

                    var errorJson = await tokenResp.Content.ReadFromJsonAsync<JsonElement>();
                    var error = errorJson.GetProperty("error").GetString();
                    if (error == "authorization_declined" || error == "expired_token")
                    {
                        await updateStatusCallback("Doğrulama iptal edildi veya süresi doldu.");
                        return null;
                    }
                    if (error == "slow_down")
                    {
                        interval += 5;
                    }
                }
                catch { }
            }

            if (microsoftToken == null)
            {
                await updateStatusCallback("Doğrulama zaman aşımına uğradı.");
                return null;
            }

            await updateStatusCallback("Xbox Live doğrulanıyor...");

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
            }));
            xboxResp.EnsureSuccessStatusCode();
            var xboxJson = await xboxResp.Content.ReadFromJsonAsync<JsonElement>();
            var xboxToken = xboxJson.GetProperty("Token").GetString()!;

            await updateStatusCallback("XSTS doğrulanıyor...");

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
            }));
            xstsResp.EnsureSuccessStatusCode();
            var xstsJson = await xstsResp.Content.ReadFromJsonAsync<JsonElement>();
            var xstsToken = xstsJson.GetProperty("Token").GetString()!;
            var userHash = xstsJson.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString()!;

            await updateStatusCallback("Minecraft hesabına bağlanılıyor...");

            // Step 5: Minecraft auth
            var mcResp = await _http.PostAsync(MinecraftAuthUrl, JsonContent.Create(new
            {
                identityToken = $"XBL3.0 x={userHash};{xstsToken}"
            }));
            mcResp.EnsureSuccessStatusCode();
            var mcJson = await mcResp.Content.ReadFromJsonAsync<JsonElement>();
            var mcAccessToken = mcJson.GetProperty("access_token").GetString()!;

            await updateStatusCallback("Minecraft profili alınıyor...");

            // Step 6: Minecraft profile
            var profileReq = new HttpRequestMessage(HttpMethod.Get, MinecraftProfileUrl);
            profileReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mcAccessToken);
            var profileResp = await _http.SendAsync(profileReq);

            string username;
            string uuid;

            if (profileResp.IsSuccessStatusCode)
            {
                var profileJson = await profileResp.Content.ReadFromJsonAsync<JsonElement>();
                username = profileJson.GetProperty("name").GetString()!;
                uuid = profileJson.GetProperty("id").GetString()!;
            }
            else
            {
                return null;
            }

            _config.Log($"Microsoft girişi başarılı: {username}");

            return new MicrosoftAuthResult
            {
                Username = username,
                Uuid = uuid,
                AccessToken = mcAccessToken,
                AuthType = "microsoft"
            };
        }
        catch (Exception ex)
        {
            _config.Log($"Microsoft giriş hatası: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _http?.Dispose();
        GC.SuppressFinalize(this);
    }
}
