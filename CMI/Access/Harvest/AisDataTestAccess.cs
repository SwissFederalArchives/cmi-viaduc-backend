using System.Threading.Tasks;
using CMI.Contract.Harvest;

namespace CMI.Access.Harvest
{
    public partial class AISDataAccess : IDbTestAccess
    {
        public Task<string> GetDbVersion()
        {
            return aisDataProvider.GetDbVersion();
        }
    }
}