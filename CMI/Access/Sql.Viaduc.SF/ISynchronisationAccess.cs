using CMI.Contract.Common.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMI.Access.Sql.Viaduc.EF;

public interface ISynchronisationAccess
{
    ViaducDb Context { get; }
    Task<List<SyncActionLogDto>> LogData(long? id);

    Task<List<SyncActionDto>> SyncData(int filterId);
    Task<List<VSyncNumberPerHourDto>> SyncNumberPerHour(int days);
}