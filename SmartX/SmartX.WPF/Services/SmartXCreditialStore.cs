using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SmartX.WPF.Services;

public class SmartXCredentialStore
{
    private readonly string _filePath;

    public SmartXCredentialStore()
    {
        var appData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        var directory =
            Path.Combine(
                appData,
                "SmartX");

        Directory.CreateDirectory(directory);

        _filePath =
            Path.Combine(
                directory,
                "remembered-login.dat");
    }

    public async Task SaveAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException(
                "Refresh token is required.",nameof(refreshToken));

        var bytes =
            Encoding.UTF8.GetBytes(refreshToken);

        var protectedBytes =
            ProtectedData.Protect(
                bytes,
                null,
                DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(
            _filePath,
            protectedBytes);
    }

    public async Task<string?> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var protectedBytes =
                await File.ReadAllBytesAsync(
                    _filePath);

            var bytes =
                ProtectedData.Unprotect(
                    protectedBytes,
                    null,
                    DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // If the stored credentials cannot
            // be decrypted, remove them.
            await DeleteAsync();

            return null;
        }
    }

    public Task DeleteAsync()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);

        return Task.CompletedTask;
    }

    public bool Exists =>
        File.Exists(_filePath);
}