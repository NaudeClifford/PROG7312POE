using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartX.Domain.Interfaces;

namespace SmartX.Infrastructure.Authentication.Firebase;

public class FirebaseAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly FirebaseAuthService _firebaseAuthService;
    private readonly IUserRepository _userRepository;

    public FirebaseAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        FirebaseAuthService firebaseAuthService,
        IUserRepository userRepository)
        : base(options, logger, encoder, clock)
    {
        _firebaseAuthService = firebaseAuthService;
        _userRepository = userRepository;
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
                await _firebaseAuthService.VerifyTokenAsync(idToken);

            var user =
                await _userRepository.GetByFirebaseUidAsync(
                    decodedToken.Uid);

            if (user is null)
            {
                return AuthenticateResult.Fail(
                    "SmartX user was not found.");
            }

            if (!user.IsActive)
            {
                return AuthenticateResult.Fail(
                    "SmartX user is inactive.");
            }

            var claims = new List<Claim>
    {
        new(
            ClaimTypes.NameIdentifier,
            decodedToken.Uid),

        new(
            ClaimTypes.Role,
            user.Role.ToString())
    };

            var identity = new ClaimsIdentity(
                claims,
                Scheme.Name);

            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(
                principal,
                Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (FirebaseAuthException ex)
        {
            return AuthenticateResult.Fail(
                $"Firebase authentication failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(
                $"Authentication handler failed: {ex.Message}");
        }

    }
}