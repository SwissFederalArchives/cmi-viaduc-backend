using System;
using System.Linq;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Management;
using CMI.Web.Management.Auth;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    public class NewsController : ApiManagementControllerBase
    {
        private readonly NewsDataAccess newsDataAccess;

        public NewsController(NewsDataAccess newsDataAccess)
        {
            this.newsDataAccess = newsDataAccess;
        }

        [HttpGet]
        public IHttpActionResult GetAllNewsForManagementClient()
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationNewsEinsehen);

            try
            {
                var result = newsDataAccess.GetAllNewsForManagementClient();
                return Ok(result);
            }
            catch (Exception e)
            {
                Log.Error(e, "(NewsController:GetAllNewsForManagementClient())");
                return InternalServerError(e);
            }
        }

        [HttpPost]
        public void DeleteNews([FromBody] string[] idsToDelete)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationNewsBearbeiten);

            try
            {
                newsDataAccess.DeleteNews(idsToDelete.ToList());
            }
            catch (Exception e)
            {
                Log.Error(e, "(NewsController:DeleteNews())");
            }
        }

        [HttpPost]
        public IHttpActionResult InsertOrUpdateNews([FromBody] News news)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationNewsBearbeiten);

            const string messageTemplate = "(NewsController:InsertOrUpdateNews())";

            try
            {
                newsDataAccess.InsertOrUpdateNews(news);
                return Ok(new { });
            }
            catch (ArgumentException ae)
            {
                Log.Error(ae, messageTemplate);
                return BadRequest("Datum in falschem Format. Verwenden Sie das Format TT.MM.JJJJ HH:MM");
            }
            catch (Exception e)
            {
                Log.Error(e, messageTemplate);
                return InternalServerError(e);
            }
        }

        [HttpGet]
        public IHttpActionResult GetSingleNews(int id)
        {
            try
            {
                var result = newsDataAccess.GetSingleNews(id);
                return Ok(result);
            }
            catch (Exception e)
            {
                Log.Error(e, $"(NewsController:GetNews({id}))");
                return InternalServerError(e);
            }
        }
    }
}