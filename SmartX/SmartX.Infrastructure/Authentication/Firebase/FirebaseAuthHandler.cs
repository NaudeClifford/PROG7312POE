using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartX.Infrastructure.Authentication.Firebase;

public class FirebaseAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly FirebaseAuthService _firebaseAuthService;

    public FirebaseAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        FirebaseAuthService firebaseAuthService)
        : base(options, logger, encoder, clock)
    {
        _firebaseAuthService = firebaseAuthService;
    }

    protected override async Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                "Authorization",
                out var authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var header = authorizationHeader.ToString();

        if (!header.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var idToken =
            header["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(idToken))
        {
            return AuthenticateResult.Fail(
                "Firebase token is missing.");
        }

        try
        {
            var decodedToken =
                await _firebaseAuthService
                    .VerifyTokenAsync(idToken);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    decodedToken.Uid)
            };

            var identity =
                new ClaimsIdentity(
                    claims,
                    Scheme.Name);

            var principal =
                new ClaimsPrincipal(identity);

            var ticket =
                new AuthenticationTicket(
                    principal,
                    Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (FirebaseAuthException)
        {
            return AuthenticateResult.Fail(
                "Invalid Firebase authentication token.");
        }
        catch (Exception)
        {
            return AuthenticateResult.Fail(
                "Firebase authentication failed.");
        }
    }
}