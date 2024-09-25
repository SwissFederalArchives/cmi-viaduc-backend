﻿using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Cors;

namespace CMI.Web.Common.api
{
    [CamelCaseJson]
    [EnableCors("*", "*", "*")]
    public abstract class ApiControllerBase : ApiController
    {
        protected ControllerHelper ControllerHelper => new ControllerHelper(((ClaimsIdentity)this.User.Identity).Claims);
    }
}