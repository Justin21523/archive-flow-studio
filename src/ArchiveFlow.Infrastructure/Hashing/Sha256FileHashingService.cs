using System.Security.Cryptography;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Infrastructure.Hashing;

public class Sha256FileHashingService : IFileHashingService
{
    public async Task<string> ComputeSha256HashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
