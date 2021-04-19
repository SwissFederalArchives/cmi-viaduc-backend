using System;
using CMI.Engine.MailTemplate.Properties;

namespace CMI.Engine.MailTemplate
{
    public class Global
    {
        public string HeutigesDatum => DateTime.Now.ToString("dd.MM.yyyy");

        public string PublicClientUrl => Settings.Default.PublicClientUrl;

        public string ManagementClientUrl => Settings.Default.ManagementClientUrl;
    }
}