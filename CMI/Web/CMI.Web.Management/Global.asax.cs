using System;
using System.Web;
using System.Web.Mvc;

namespace CMI.Web.Management
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            MvcHandler.DisableMvcResponseHeader = true;
        }
    }
}