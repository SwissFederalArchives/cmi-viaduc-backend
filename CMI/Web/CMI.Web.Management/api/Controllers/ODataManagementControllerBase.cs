using System.Security.Claims;
using CMI.Web.Common.api;
using Microsoft.AspNet.OData;

namespace CMI.Web.Management.api.Controllers
{
    public abstract class ODataManagementControllerBase : ODataController
    {
        public ControllerHelper ControllerHelper => new ControllerHelper(((ClaimsIdentity) this.User.Identity).Claims);
        public ManagementControllerHelper ManagementHelper => new ManagementControllerHelper(this, ControllerHelper);
    }
}