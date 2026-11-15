using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace auth.integrationTest
{
    // RawApiKey is null for public clients; Origin is what a browser on that app would send
    public record AppCredentials(Guid Id, string Audience, string? RawApiKey, string? Origin = null);

    public record AuthResult(HttpStatusCode Status, string? AccessToken, string? RefreshToken, JsonElement Body);

    // Thin wrapper over HttpClient that speaks the auth API and tracks the refresh cookie by hand,
    // so tests can replay old cookies.
    public class ApiClient
    {
        public const string Password = "Passw0rd!";

        private readonly HttpClient _http;

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<AppCredentials> CreateAppAsync(string? audience = null) =>
            await CreateAppAsync(new { name = "Integration App", audience });

        public async Task<AppCredentials> CreatePublicAppAsync(string origin)
        {
            var app = await CreateAppAsync(new { name = "Integration SPA", isPublic = true, allowedOrigins = new[] { origin } });
            return app with { Origin = origin };
        }

        private async Task<AppCredentials> CreateAppAsync(object body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/application-client")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("X-Admin-Key", AuthApiFactory.AdminKey);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AppCredentials>())!;
        }

        public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

        public Task<AuthResult> RegisterAsync(AppCredentials app, string email, string password = Password) =>
            SendAuthAsync(HttpMethod.Post, "/api/auth/register", app, new { fullName = "Jane Doe", email, city = "Colombo", password });

        public Task<AuthResult> LoginAsync(AppCredentials app, string email, string password = Password) =>
            SendAuthAsync(HttpMethod.Post, "/api/auth/login", app, new { email, password });

        public Task<AuthResult> RefreshAsync(string? refreshToken) =>
            SendAuthAsync(HttpMethod.Post, "/api/auth/refresh", null, null, refreshToken: refreshToken);

        public Task<AuthResult> LogoutAsync(AppCredentials app, string accessToken, string? refreshToken) =>
            SendAuthAsync(HttpMethod.Post, "/api/auth/logout", app, null, accessToken, refreshToken);

        public Task<AuthResult> SessionsAsync(string accessToken, string? refreshToken = null) =>
            SendAuthAsync(HttpMethod.Get, "/api/auth/sessions", null, null, accessToken, refreshToken);

        public Task<AuthResult> RevokeSessionAsync(string accessToken, int sessionId) =>
            SendAuthAsync(HttpMethod.Delete, $"/api/auth/sessions/{sessionId}", null, null, accessToken);

        public Task<AuthResult> MeAsync(string accessToken) =>
            SendAuthAsync(HttpMethod.Get, "/api/auth/me", null, null, accessToken);

        public async Task<AuthResult> SendAuthAsync(HttpMethod method, string path, AppCredentials? app, object? body,
            string? accessToken = null, string? refreshToken = null)
        {
            using var request = new HttpRequestMessage(method, path);
            if (body != null)
                request.Content = JsonContent.Create(body);
            if (app != null)
            {
                request.Headers.Add("X-App-Id", app.Id.ToString());
                if (app.RawApiKey != null)
                    request.Headers.Add("X-Api-Key", app.RawApiKey);
                if (app.Origin != null)
                    request.Headers.Add("Origin", app.Origin);
            }
            if (accessToken != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (refreshToken != null)
                request.Headers.Add("Cookie", $"refreshToken={refreshToken}");

            var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            var json = string.IsNullOrWhiteSpace(text) || !text.TrimStart().StartsWith('{') && !text.TrimStart().StartsWith('[')
                ? default
                : JsonDocument.Parse(text).RootElement.Clone();

            string? newAccessToken = null;
            if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("accessToken", out var at))
                newAccessToken = at.GetString();

            return new AuthResult(response.StatusCode, newAccessToken, ReadRefreshCookie(response), json);
        }

        // Returns the cookie value, "" if the cookie was cleared, or null if it wasn't touched
        private static string? ReadRefreshCookie(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
                return null;

            var cookie = cookies.FirstOrDefault(c => c.StartsWith("refreshToken=", StringComparison.Ordinal));
            if (cookie == null)
                return null;

            var value = cookie.Split(';')[0]["refreshToken=".Length..];
            return Uri.UnescapeDataString(value);
        }
    }
}
