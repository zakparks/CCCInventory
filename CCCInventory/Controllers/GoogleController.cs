using System.Security.Cryptography;
using CCCInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CCCInventory.Controllers
{
    // One-time "Connect Google" flow for the bakery account that owns the contract templates.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GoogleController : ControllerBase
    {
        private const string StateCachePrefix = "google-oauth-state:";

        private readonly GoogleAuthService _google;
        private readonly WeddingContractService _contracts;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GoogleController> _logger;

        public GoogleController(GoogleAuthService google, WeddingContractService contracts,
            IMemoryCache cache, ILogger<GoogleController> logger)
        {
            _google = google;
            _contracts = contracts;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status(CancellationToken ct)
        {
            var credential = await _google.GetCredentialAsync(ct);
            return Ok(new
            {
                configured = _google.IsConfigured,
                connected = credential != null,
                accountEmail = credential != null ? await _google.GetAccountEmailAsync() : null,
                templateConfigured = !string.IsNullOrWhiteSpace(_google.Settings.WeddingContractTemplateId)
            });
        }

        private record PendingAuth(string RedirectUri, string ReturnOrigin);

        // returnOrigin: the SPA's window.location.origin. Unless Google:RedirectUri is configured, the
        // callback URL is built from it, because the browser's address is what Google redirects to: the
        // backend's own view of the request can differ (behind the Angular dev proxy it sees https on the
        // dev server's port, which serves plain http). Only the API's own hostname is accepted, so this
        // can't be used as an open redirect.
        [HttpGet("authorize-url")]
        public IActionResult AuthorizeUrl([FromQuery] string? returnOrigin)
        {
            if (!_google.IsConfigured)
                return BadRequest(new { message = "Google OAuth is not configured (Google:ClientId / Google:ClientSecret)." });

            var origin = "";
            if (Uri.TryCreate(returnOrigin, UriKind.Absolute, out var o)
                && (o.Scheme == "http" || o.Scheme == "https")
                && string.Equals(o.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
                origin = o.GetLeftPart(UriPartial.Authority);

            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var redirectUri =
                !string.IsNullOrWhiteSpace(_google.Settings.RedirectUri) ? _google.Settings.RedirectUri!
                : origin != "" ? $"{origin}/api/google/callback"
                : $"{Request.Scheme}://{Request.Host}/api/google/callback";
            _cache.Set(StateCachePrefix + state, new PendingAuth(redirectUri, origin), TimeSpan.FromMinutes(10));
            return Ok(new { url = _google.BuildAuthorizationUrl(redirectUri, state) });
        }

        // Google redirects the browser here. The auth cookie is SameSite=Strict, so it is not sent on
        // this cross-site redirect; the one-time state value issued by authorize-url protects it instead.
        [AllowAnonymous]
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state,
            [FromQuery] string? error, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(state) || !_cache.TryGetValue(StateCachePrefix + state, out PendingAuth? pending) || pending == null)
                return Redirect("/management?google=error&reason=state");
            _cache.Remove(StateCachePrefix + state);

            var back = $"{pending.ReturnOrigin}/management";
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                return Redirect($"{back}?google=error&reason={Uri.EscapeDataString(error ?? "no_code")}");

            try
            {
                await _google.ExchangeCodeAsync(code, pending.RedirectUri, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google OAuth code exchange failed");
                return Redirect($"{back}?google=error&reason=exchange");
            }
            return Redirect($"{back}?google=connected");
        }

        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect(CancellationToken ct)
        {
            await _google.DisconnectAsync(ct);
            return Ok();
        }

        [HttpGet("template-check")]
        public async Task<IActionResult> TemplateCheck(CancellationToken ct)
        {
            try
            {
                return Ok(await _contracts.CheckTemplateAsync(ct));
            }
            catch (Exception ex) when (ContractErrors.UserMessage(ex) is { } message)
            {
                return BadRequest(new { message });
            }
        }

        [HttpGet("tokens")]
        public IActionResult Tokens() => Ok(WeddingContractService.KnownTokens());
    }
}
