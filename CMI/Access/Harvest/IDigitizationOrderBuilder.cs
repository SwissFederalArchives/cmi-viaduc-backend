using CMI.Contract.Common;
using System.Threading.Tasks;

namespace CMI.Access.Harvest
{
    public interface IDigitizationOrderBuilder
    {
        Task<DigitalisierungsAuftrag> Build(string recordId);
    }
}
