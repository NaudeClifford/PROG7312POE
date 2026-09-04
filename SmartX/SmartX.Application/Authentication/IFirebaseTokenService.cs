namespace SmartX.Application.Authentication;

public interface IFirebaseTokenService
{
    Task<FirebaseUserIdentity> VerifyIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(
    string firebaseUid,
    CancellationToken cancellationToken = default);
}
