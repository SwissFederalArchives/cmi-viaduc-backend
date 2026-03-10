using System.Threading;
using System.Threading.Tasks;

namespace CMI.Access.Harvest.ActaPro;

public interface IAuthService
{
    Task<ActaProToken> GetNewTokenAsync(CancellationToken ct);
    Task<ActaProToken> RefreshAsync(string refreshToken, CancellationToken ct);
}