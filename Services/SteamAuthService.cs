using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace HackHelper.Services
{
    public class SteamAuthService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string STEAM_OPENID_URL = "https://steamcommunity.com/openid/login";
        private const string RETURN_URL = "http://localhost:8080/auth/steam/callback";
        private HttpListener? _listener;
        private string? _steamApiKey;

        public string? SteamId { get; private set; }
        public string? PersonaName { get; private set; }
        public string? AvatarUrl { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(SteamId);

        public SteamAuthService(string? steamApiKey = null)
        {
            _steamApiKey = steamApiKey;
        }

        /// <summary>
        /// Initiates Steam OpenID authentication flow
        /// </summary>
        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                // Start local HTTP listener
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:8080/");
                _listener.Start();

                // Build Steam OpenID login URL
                var parameters = new Dictionary<string, string>
                {
                    { "openid.ns", "http://specs.openid.net/auth/2.0" },
                    { "openid.mode", "checkid_setup" },
                    { "openid.return_to", RETURN_URL },
                    { "openid.realm", "http://localhost:8080" },
                    { "openid.identity", "http://specs.openid.net/auth/2.0/identifier_select" },
                    { "openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select" }
                };

                var queryString = string.Join("&", parameters.Select(p =>
                    $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
                var loginUrl = $"{STEAM_OPENID_URL}?{queryString}";

                // Open browser for user to login
                Process.Start(new ProcessStartInfo
                {
                    FileName = loginUrl,
                    UseShellExecute = true
                });

                // Wait for callback
                var context = await _listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // Parse Steam ID from response
                var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
                var claimedId = queryParams["openid.claimed_id"];

                if (!string.IsNullOrEmpty(claimedId))
                {
                    // Extract Steam ID from claimed_id URL
                    var match = Regex.Match(claimedId, @"https://steamcommunity.com/openid/id/(\d+)");
                    if (match.Success)
                    {
                        SteamId = match.Groups[1].Value;

                        // Verify the authentication
                        if (await VerifyAuthenticationAsync(queryParams))
                        {
                            // Fetch user profile info
                            await FetchUserProfileAsync();

                            // Send success response to browser
                            string responseString = @"
                                <!DOCTYPE html>
                                <html>
                                <head>
                                    <title>Steam Authentication Success</title>
                                    <style>
                                        body { 
                                            font-family: Arial, sans-serif; 
                                            text-align: center; 
                                            padding: 50px;
                                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                            color: white;
                                        }
                                        .container {
                                            background: rgba(255,255,255,0.1);
                                            border-radius: 10px;
                                            padding: 30px;
                                            max-width: 500px;
                                            margin: 0 auto;
                                        }
                                        h1 { color: #fff; }
                                    </style>
                                </head>
                                <body>
                                    <div class='container'>
                                        <h1>✅ Authentication Successful!</h1>
                                        <p>You have successfully logged in with Steam.</p>
                                        <p>You can close this window and return to the application.</p>
                                    </div>
                                </body>
                                </html>";

                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = buffer.Length;
                            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            response.Close();

                            _listener.Stop();
                            return true;
                        }
                    }
                }

                // Send error response
                string errorResponse = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Authentication Failed</title>
                        <style>
                            body { 
                                font-family: Arial, sans-serif; 
                                text-align: center; 
                                padding: 50px;
                                background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
                                color: white;
                            }
                        </style>
                    </head>
                    <body>
                        <h1>❌ Authentication Failed</h1>
                        <p>Please try again.</p>
                    </body>
                    </html>";

                byte[] errorBuffer = System.Text.Encoding.UTF8.GetBytes(errorResponse);
                response.ContentLength64 = errorBuffer.Length;
                await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                response.Close();

                _listener.Stop();
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Authentication error: {ex.Message}");
                _listener?.Stop();
                return false;
            }
        }

        /// <summary>
        /// Verifies the OpenID authentication response
        /// </summary>
        private async Task<bool> VerifyAuthenticationAsync(System.Collections.Specialized.NameValueCollection queryParams)
        {
            try
            {
                var verifyParams = new Dictionary<string, string>();

                foreach (string key in queryParams.AllKeys)
                {
                    if (key != null && key.StartsWith("openid."))
                    {
                        verifyParams[key] = queryParams[key] ?? "";
                    }
                }

                verifyParams["openid.mode"] = "check_authentication";

                var content = new FormUrlEncodedContent(verifyParams);
                var response = await _httpClient.PostAsync(STEAM_OPENID_URL, content);
                var responseText = await response.Content.ReadAsStringAsync();

                return responseText.Contains("is_valid:true");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Verification error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fetches user profile information from Steam API
        /// </summary>
        private async Task FetchUserProfileAsync()
        {
            if (string.IsNullOrEmpty(SteamId))
                return;

            try
            {
                // Try using API key if available
                if (!string.IsNullOrEmpty(_steamApiKey))
                {
                    var url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={_steamApiKey}&steamids={SteamId}";
                    var response = await _httpClient.GetStringAsync(url);
                    var json = JObject.Parse(response);

                    var player = json["response"]?["players"]?[0];
                    if (player != null)
                    {
                        PersonaName = player["personaname"]?.ToString();
                        AvatarUrl = player["avatarfull"]?.ToString();
                        return;
                    }
                }

                // Fallback: scrape profile page (no API key needed)
                var profileUrl = $"https://steamcommunity.com/profiles/{SteamId}?xml=1";
                var profileResponse = await _httpClient.GetStringAsync(profileUrl);

                var nameMatch = Regex.Match(profileResponse, @"<steamID><!\[CDATA\[(.*?)\]\]></steamID>");
                if (nameMatch.Success)
                {
                    PersonaName = nameMatch.Groups[1].Value;
                }

                var avatarMatch = Regex.Match(profileResponse, @"<avatarFull><!\[CDATA\[(.*?)\]\]></avatarFull>");
                if (avatarMatch.Success)
                {
                    AvatarUrl = avatarMatch.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch profile: {ex.Message}");
                PersonaName = $"User {SteamId}";
            }
        }

        /// <summary>
        /// Fetches user's Steam wishlist
        /// </summary>
        public async Task<List<string>> GetWishlistAsync()
        {
            if (string.IsNullOrEmpty(SteamId))
                return new List<string>();

            try
            {
                // Steam wishlist endpoint (public if profile is public)
                var url = $"https://store.steampowered.com/wishlist/profiles/{SteamId}/wishlistdata/";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var wishlist = new List<string>();
                foreach (var item in json)
                {
                    wishlist.Add(item.Key); // App ID
                }

                return wishlist;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch wishlist: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Logs out the current user
        /// </summary>
        public void Logout()
        {
            SteamId = null;
            PersonaName = null;
            AvatarUrl = null;
        }

        public void Dispose()
        {
            _listener?.Stop();
            _listener?.Close();
        }
    }
}