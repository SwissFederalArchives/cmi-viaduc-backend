using System.Threading.Tasks;

namespace CMI.Contract.Harvest
{
    public interface IDbTestAccess
    {
        Task<string> GetDbVersion();
    }
}