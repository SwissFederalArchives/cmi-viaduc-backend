using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMI.Contract.Parameter
{
    public class OnboardingSettings: CentralizedSetting
    {
        [Description("URI, an welche die Benutzer weitergeleitet werden für das Onboarding")]
        [Default("https://dis.swisscom.ch/service/index/cn/001029/ti/{userid}/act/pass?lang={language}")]
        public string UriTemplate { get; set; }
    }
}
