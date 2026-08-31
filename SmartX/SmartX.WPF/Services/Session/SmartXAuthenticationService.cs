using SmartX.Application.Authentication;
using SmartX.WPF.Services.Api;
using SmartX.WPF.Services.Sync;

namespace SmartX.WPF.Services.Session;

public class SmartXAuthenticationService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ISmartXApiClient _apiClient;
    private readonly SmartXSession _session;
    private readonly ICacheSyncService _cacheSyncService;
    private readonly SmartXCredentialStore _credentialStore;

    public SmartXAuthenticationService(
        IAuthenticationService authenticationService,
        ISmartXApiClient apiClient,
        SmartXSession session,
        ICacheSyncService cacheSyncService,
        SmartXCredentialStore credentialStore)
    {
        _authenticationService =
            authenticationService;

        _apiClient =
            apiClient;

        _session =
            session;

        _cacheSyncService =
            cacheSyncService;

        _credentialStore =
            credentialStore;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        var refreshToken =
            await _credentialStore.LoadAsync();

        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        try
        {
            var result =
                await _authenticationService
                    .RefreshTokenAsync(
                        refreshToken);

            if (!result.Success)
            {
                await _credentialStore.DeleteAsync();

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    result.UserId))
            {
                await _credentialStore.DeleteAsync();

                return false;
            }

            if (string.IsNullOrWhiteSpace(result.IdToken))
            {
                await _credentialStore.DeleteAsync();
                return false;
            }

            var user =
                await _apiClient.GetUserByFirebaseUidAsync(
                    result.UserId,
                    result.IdToken);

            if (user is null)
            {
                await _credentialStore.DeleteAsync();

                return false;
            }

            if (!user.IsActive)
            {
                await _credentialStore.DeleteAsync();

                return false;
            }

            _session.SignIn(
                user,
                result.IdToken ?? string.Empty,
                result.RefreshToken ??
                    refreshToken);

            var newRefreshToken =
                result.RefreshToken;

            if (!string.IsNullOrWhiteSpace(
                    newRefreshToken))
            {
                await _credentialStore
                    .SaveAsync(
                        newRefreshToken);
            }

            await _cacheSyncService
                .SyncUserAsync(user.Id);

            await _cacheSyncService
                .SyncCompanyAsync(user.CompanyId);

            await _cacheSyncService
                .SyncGatewaysAsync(user.CompanyId);

            await _cacheSyncService
                .SyncSensorsAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _credentialStore.DeleteAsync();

        _session.SignOut();
    }
}