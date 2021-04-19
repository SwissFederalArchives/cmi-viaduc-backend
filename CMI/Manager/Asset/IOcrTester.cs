using System.Threading.Tasks;
using CMI.Manager.Asset.Consumers;

namespace CMI.Manager.Asset
{
    public interface IOcrTester
    {
        Task<TestConversionResult> TestConversion();
    }
}