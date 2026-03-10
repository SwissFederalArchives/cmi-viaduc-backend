using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace CMI.Web.Management.api.Controllers;

[Authorize]
public class SynchronisationenController : ApiManagementControllerBase
{
    private readonly ISynchronisationManager synchronisationManager;
    public SynchronisationenController(ISynchronisationManager synchronisationManager)
    {
        this.synchronisationManager = synchronisationManager;
    }


    [HttpGet]
    public async Task<IEnumerable<SyncActionLogDto>> LogData(string id)
    {
        if (long.TryParse(id.Replace("’", string.Empty), out long syncActionLogId))
        {
            var result = await synchronisationManager.GetLogData(syncActionLogId);
            return result;
        }
        return null;
    }

    [HttpGet]
    [Route("api/Synchronisationen/SyncData/{filterId}")]
    public async Task<IEnumerable<SyncActionDto>> SyncData(int filterId)
    {
        var result = await synchronisationManager.GetSyncData(filterId);
        return result;

    }

    [HttpPost]
    [Route("api/Synchronisationen/BatchAddSyncActions/{actionId}")]

    public async Task<IHttpActionResult> BatchAddSyncActions([FromBody] string[] ids, int actionId)
    {
        var access = ManagementControllerHelper.GetUserAccess();
        access.AssertFeatureOrThrow(ApplicationFeature.SynchronizationHinzufuegenBearbeiten);
        // Filter out empty lines and duplicates
        ids = ids.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();
        var isSuccess = await synchronisationManager.BatchAddSyncActions(ids.Select(WebUtility.UrlDecode).ToArray(), actionId);
        if (isSuccess)
        {
            return Ok();
        }

        return BadRequest("Die Daten konnten nicht hinzugefügt werden.");
    }

    [HttpGet]
    [Route("api/Synchronisationen/SyncNumberPerHour/{days}")]
    public async Task<IEnumerable<VSyncNumberPerHourDto>> SyncNumberPerHour(int days)
    {
        var access = ManagementControllerHelper.GetUserAccess();
        access.AssertFeatureOrThrow(ApplicationFeature.SynchronizationUeberwachenEinsehen);
        var result = await synchronisationManager.GetSyncNumberPerHour(days);
        return result;
    }
}