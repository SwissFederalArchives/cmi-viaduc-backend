using System.Collections.Generic;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Configuration;

namespace CMI.Web.Management.Helpers
{
    public class ManagementTranslationHelper : TranslationHelper
    {
        protected override void Initialize()
        {
            base.Initialize();

            var vdInfo = Viaduc = new AppInfo(this, "viaducclient", "vd", "app;client");
            vdInfo.AppDatas = new List<AppData>
            {
                new JsonAppData
                {
                    Info = "Public/Settings",
                    Root = ManagementSettingsViaduc.Instance.GetServerSettings(),
                    Mappings = new List<JsonDataMapping>()
                }
            };
        }
    }
}