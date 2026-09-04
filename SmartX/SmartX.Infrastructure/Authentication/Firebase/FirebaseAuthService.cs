using System.IO;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using SmartX.Application.Authentication;

namespace SmartX.Infrastructure.Authentication.Firebase;

public class FirebaseAuthService : IFirebaseTokenService
{
    private readonly FirebaseOptions _options;

    public FirebaseAuthService(IOptions<FirebaseOptions> options)
    {
        _options = options.Value;

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        if (FirebaseApp.DefaultInstance != null)
            return;

        if (string.IsNullOrWhiteSpace(_options.ServiceAccountPath))
        {
            throw new InvalidOperationException(
                "Firebase ServiceAccountPath is not configured.");
        }

        var serviceAccountPath = Path.Combine(
            AppContext.BaseDirectory,
            _options.ServiceAccountPath);

        if (!File.Exists(serviceAccountPath))
        {
            throw new FileNotFoundException(
                "Firebase service account file was not found.",
                serviceAccountPath);
        }

        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(serviceAccountPath),

            ProjectId = _options.ProjectId
        });
    }
    public async Task<FirebaseToken> VerifyTokenAsync(
    string idToken,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException(
                "Firebase ID token is required.",
                nameof(idToken));
        }

        return await FirebaseAuth.DefaultInstance
            .VerifyIdTokenAsync(idToken);
    }

    public async Task<FirebaseUserIdentity> VerifyIdTokenAsync(
    string idToken,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException(
                "Firebase ID token is required.",
                nameof(idToken));
        }

        var decodedToken =
            await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken);

        var email =
            decodedToken.Claims.TryGetValue(
                "email",
                out var emailClaim)
                    ? emailClaim?.ToString() ?? string.Empty
                    : string.Empty;

        return new FirebaseUserIdentity
        {
            FirebaseUid = decodedToken.Uid,
            Email = email
        };
    }

    public async Task DeleteUserAsync(
    string firebaseUid,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            throw new ArgumentException(
                "Firebase UID is required.",
                nameof(firebaseUid));
        }

        await FirebaseAuth.DefaultInstance
            .DeleteUserAsync(firebaseUid);
    }
}