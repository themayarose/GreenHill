using GreenHill.Helpers;
using Windows.Security.Credentials;

namespace GreenHill.Services;

public interface IConnectionService {
    Task<SkyConnection> GetOrCreateAsync(PasswordCredential credential, CancellationToken? ct = default);
}