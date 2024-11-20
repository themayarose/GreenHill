using GreenHill.Helpers;
using Windows.Security.Credentials;

namespace GreenHill.Services;

public class ConnectionService : IConnectionService {
    public async Task<SkyConnection> GetOrCreateAsync(PasswordCredential credential, CancellationToken? ct = default) =>
        await SkyConnection.GetOrCreateAsync(credential, ct);
}