using SmartX.Application.Authentication;
using SmartX.Domain.Enums;
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
        _authenticationService = authenticationService;
        _apiClient = apiClient;
        _session = session;
        _cacheSyncService = cacheSyncService;
        _credentialStore = credentialStore;
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
                await _authenticationService.RefreshTokenAsync(
                    refreshToken);

            if (!result.Success ||
                string.IsNullOrWhiteSpace(result.UserId) ||
                string.IsNullOrWhiteSpace(result.IdToken))
            {
                await _credentialStore.DeleteAsync();
                return false;
            }

            var user =
                await _apiClient.GetUserByFirebaseUidAsync(
                    result.UserId,
                    result.IdToken);

            if (user is null ||
                !user.IsActive)
            {
                await _credentialStore.DeleteAsync();
                return false;
            }

            if (user.Role != UserRole.SuperAdmin &&
                user.CompanyId == Guid.Empty)
            {
                await _credentialStore.DeleteAsync();
                return false;
            }

            _session.SignIn(
                user,
                result.IdToken,
                result.RefreshToken ?? refreshToken);

            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                await _credentialStore.SaveAsync(
                    result.RefreshToken);
            }

            if (user.Role != UserRole.SuperAdmin)
            {
                var company =
                    await _cacheSyncService.GetCompanyAsync(
                        user.CompanyId);

                if (company is null)
                {
                    _session.SignOut();
                    await _credentialStore.DeleteAsync();
                    return false;
                }

                _session.SetCompanyName(company.Name);

                await _cacheSyncService.SyncGatewaysAsync(
                    user.CompanyId);

                await _cacheSyncService.SyncSensorsAsync();
            }

            await _cacheSyncService.SyncUserAsync(
                user.Id);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Session restore failed: {ex}");

            return false;
        }
    }

    public async Task LogoutAsync()
    {
        var isGuest = _session.IsGuest;
        var userId = _session.UserId;
        var companyId = _session.CompanyId;

        try
        {
            if (isGuest &&
                userId != Guid.Empty &&
                companyId != Guid.Empty)
            {
                await _apiClient.DeleteCompanyAsync(
                    companyId);
            }
        }
        finally
        {
            await _credentialStore.DeleteAsync();
            _session.SignOut();
        }
    }

}
