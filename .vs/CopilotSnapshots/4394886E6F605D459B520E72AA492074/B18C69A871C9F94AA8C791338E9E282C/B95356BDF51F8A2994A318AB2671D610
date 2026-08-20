using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace SmartX.Infrastructure.Authentication.Firebase;

public class FirebaseAuthService
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
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ServiceAccountPath))
        {
            throw new InvalidOperationException(
                "Firebase ServiceAccountPath is not configured.");
        }

        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(_options.ServiceAccountPath),

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
}