using Windows.Security.Credentials;

namespace GreenHill.Services;

public class CredentialService : ICredentialService {
    public PasswordVault Vault { get; } = new();

    public async Task<PasswordCredential?> GetInitialCredentialAsync() {
        var credentials = await GetAllCredentialsAsync();

        if (credentials.Count() == 1) return credentials.First();

        // if (!credentials.Any()) return null;

        return null;
    }

    public async Task<IEnumerable<PasswordCredential>> GetAllCredentialsAsync() {
        List<PasswordCredential> credentials;

        try {
            credentials = [.. Vault.FindAllByResource(Constants.PackageId)];
        }
        catch {
            credentials = [];
        }

        return await Task.FromResult(credentials);
    }

    public async Task<PasswordCredential> CreateNewCredentialAsync(string username, string password) {
        var credential = new PasswordCredential(Constants.PackageId, username, password);

        Vault.Add(credential);

        return await Task.FromResult(credential);
    }

    public async Task DeleteCredentialAsync(PasswordCredential credential) {
        Vault.Remove(credential);

        await Task.CompletedTask;
    }
}