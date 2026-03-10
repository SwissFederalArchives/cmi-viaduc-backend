using CMI.Contract.Common;
using System.Threading.Tasks;

namespace CMI.Access.Harvest
{
    public interface IArchiveRecordBuilder
    {
        Task<ArchiveRecord> Build(string archiveRecordId);
        Task<ArchiveRecordSecurity> BuildSecurityTokens(string archiveRecordId);
    }
}