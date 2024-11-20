using Windows.Security.Credentials;

namespace GreenHill.Services;

public interface ICredentialService {
    public PasswordVault Vault { get; }
    Task<PasswordCredential?> GetInitialCredentialAsync();
    Task<IEnumerable<PasswordCredential>> GetAllCredentialsAsync();
    Task<PasswordCredential> CreateNewCredentialAsync(string username, string password);
    Task DeleteCredentialAsync(PasswordCredential credential);
}