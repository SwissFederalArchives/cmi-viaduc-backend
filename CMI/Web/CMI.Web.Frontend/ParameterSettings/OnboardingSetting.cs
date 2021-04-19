using System.ComponentModel;
using CMI.Contract.Parameter;

namespace CMI.Web.Frontend.ParameterSettings
{
    public class OnboardingSetting : ISetting
    {
        [Description("URI, an welche die Benutzer weitergeleitet werden für das Onboarding")]
        [DefaultValue("https://dis.swisscom.ch/service/index/ti/{userextid}/cn/001029/act/pass?lang={language}")]
        public string UriTemplate { get; set; }
    }
}