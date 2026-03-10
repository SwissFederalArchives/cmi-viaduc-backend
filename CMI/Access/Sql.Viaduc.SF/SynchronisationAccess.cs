using System;
using CMI.Contract.Common.Entities;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace CMI.Access.Sql.Viaduc.EF;

public class SynchronisationAccess : ISynchronisationAccess
{
    private readonly ViaducDb dbContext;

    public SynchronisationAccess(ViaducDb dbContext)
    {
        this.dbContext = dbContext;
    }

    public ViaducDb Context => dbContext;

    public async Task<List<SyncActionLogDto>> LogData(long? id)
    {
        var result = await dbContext.SyncActionLogs.Where(s => s.SyncActionId == id).ToListAsync();
        return result.ToDtos();
    }

    public Task<List<SyncActionDto>> SyncData(int filterId)
    {
        DateTime dateFilter = DateTime.Now;
        switch (filterId)
        {
            case 1:
                dateFilter = DateTime.Now.AddHours(-1);
                break;
            case 2:
                dateFilter = DateTime.Now.AddHours(-2);
                break;
            case 3:
                dateFilter = DateTime.Now.AddHours(-12);
                break;
            case 4:
                dateFilter = DateTime.Now.AddDays(-1);
                break;
        }
        
        var filteredResult = dbContext.SyncActions.Where(s => s.CreatedOn > dateFilter);
        return Task.FromResult(filteredResult.OrderByDescending(x => x.SyncActionId).ToDtos());
    }

    public Task<List<VSyncNumberPerHourDto>> SyncNumberPerHour(int days)
    {
        var dateFilter = DateTime.Today.AddDays(-days);

        var filteredResult = dbContext.VSyncNumberPerHours.Where(s => s.LastModifiedDay >= dateFilter);

        return Task.FromResult(filteredResult.OrderByDescending(x => x.LastModified).ToDtos());
    }
}