using CMI.Web.Common.api;
using Microsoft.AspNet.OData;

namespace CMI.Web.Management.api.Controllers
{
    public abstract class ODataManagementControllerBase : ODataController
    {
        public ControllerHelper ControllerHelper => new ControllerHelper(this);
        public ManagementControllerHelper ManagementHelper => new ManagementControllerHelper(this, ControllerHelper);
    }
}