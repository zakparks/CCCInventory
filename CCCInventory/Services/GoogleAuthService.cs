using CCCInventory.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Docs.v1;
using Google.Apis.Drive.v3;
using Google.Apis.Json;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;

namespace CCCInventory.Services
{
    // Bound from the "Google" configuration section. ClientId/ClientSecret come from a
    // Google Cloud "Web application" OAuth client; supply them via user-secrets or the
    // Google__ClientId / Google__ClientSecret environment variables, not appsettings.json.
    public class GoogleSettings
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        // Optional. Defaults to {scheme}://{host}/api/google/callback of the incoming request.
        // Must exactly match an "Authorized redirect URI" on the OAuth client.
        public string? RedirectUri { get; set; }
        // Google Doc ID of the wedding contract template (the part of the URL after /d/).
        public string? WeddingContractTemplateId { get; set; }
        // Optional Drive folder ID for generated contracts. Defaults to the template's folder.
        public string? WeddingContractFolderId { get; set; }
    }

    // Persists the Google OAuth token in the GoogleTokens table (IDataStore for Google.Apis).
    public class EfDataStore : IDataStore
    {
        private readonly DataContext _context;
        public EfDataStore(DataContext context) => _context = context;

        public async Task StoreAsync<T>(string key, T value)
        {
            var json = NewtonsoftJsonSerializer.Instance.Serialize(value);
            var row = await _context.GoogleTokens.FindAsync(key);
            if (row == null)
                _context.GoogleTokens.Add(new GoogleToken { Key = key, Value = json, UpdatedAt = DateTime.UtcNow });
            else
            {
                row.Value = json;
                row.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync<T>(string key)
        {
            var row = await _context.GoogleTokens.FindAsync(key);
            if (row != null)
            {
                _context.GoogleTokens.Remove(row);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var row = await _context.GoogleTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Key == key);
            return row == null ? default! : NewtonsoftJsonSerializer.Instance.Deserialize<T>(row.Value);
        }

        public async Task ClearAsync()
        {
            await _context.GoogleTokens.ExecuteDeleteAsync();
        }
    }

    public class GoogleAuthService
    {
        // Single app-wide connection.
        private const string UserId = "bakery";
        private const string AccountEmailKey = "account_email";

        private static readonly string[] Scopes = [DriveService.Scope.Drive, DocsService.Scope.Documents];

        private readonly DataContext _context;
        private readonly GoogleSettings _settings;

        public GoogleAuthService(DataContext context, IConfiguration config)
        {
            _context = context;
            _settings = config.GetSection("Google").Get<GoogleSettings>() ?? new GoogleSettings();
        }

        public GoogleSettings Settings => _settings;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.ClientId) && !string.IsNullOrWhiteSpace(_settings.ClientSecret);

        private GoogleAuthorizationCodeFlow CreateFlow() =>
            new(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = _settings.ClientId, ClientSecret = _settings.ClientSecret },
                Scopes = Scopes,
                DataStore = new EfDataStore(_context),
                // Always re-prompt so Google issues a refresh token on every connect.
                Prompt = "consent"
            });

        public string BuildAuthorizationUrl(string redirectUri, string state)
        {
            var request = CreateFlow().CreateAuthorizationCodeRequest(redirectUri);
            request.State = state;
            return request.Build().AbsoluteUri;
        }

        public async Task ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct)
        {
            var flow = CreateFlow();
            var token = await flow.ExchangeCodeForTokenAsync(UserId, code, redirectUri, ct);

            // Remember which Google account is connected so the Management page can show it.
            var credential = new UserCredential(flow, UserId, token);
            var drive = new DriveService(new BaseClientService.Initializer { HttpClientInitializer = credential });
            var about = drive.About.Get();
            about.Fields = "user(emailAddress)";
            var result = await about.ExecuteAsync(ct);
            await new EfDataStore(_context).StoreAsync(AccountEmailKey, result.User?.EmailAddress ?? "");
        }

        public async Task<UserCredential?> GetCredentialAsync(CancellationToken ct)
        {
            if (!IsConfigured) return null;
            var flow = CreateFlow();
            TokenResponse? token = await flow.LoadTokenAsync(UserId, ct);
            return token == null ? null : new UserCredential(flow, UserId, token);
        }

        public async Task<string?> GetAccountEmailAsync() =>
            await new EfDataStore(_context).GetAsync<string?>(AccountEmailKey);

        public async Task DisconnectAsync(CancellationToken ct)
        {
            var credential = await GetCredentialAsync(ct);
            if (credential != null)
            {
                try { await credential.RevokeTokenAsync(ct); }
                catch { /* token may already be revoked/expired; still clear it locally */ }
            }
            await new EfDataStore(_context).ClearAsync();
        }

        public async Task<(DriveService Drive, DocsService Docs)?> CreateServicesAsync(CancellationToken ct)
        {
            var credential = await GetCredentialAsync(ct);
            if (credential == null) return null;
            var init = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CCCInventory"
            };
            return (new DriveService(init), new DocsService(init));
        }
    }
}
