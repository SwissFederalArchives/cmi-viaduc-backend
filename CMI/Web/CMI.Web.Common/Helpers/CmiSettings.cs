using System.Collections.Specialized;
using System.Configuration;

namespace CMI.Web.Common.Helpers
{
    public class CmiSettings : ICmiSettings
    {
        private NameValueCollection cmiSettings;

        public string this[string name]
        {
            get
            {
                if (cmiSettings == null)
                {
                    cmiSettings =
                        (NameValueCollection) ConfigurationManager
                            .GetSection("cmiSettings"); // it's a ReadOnlyNameValueCollection, but that's internal, so ... cast!
                    if (cmiSettings == null)
                    {
                        cmiSettings = new NameValueCollection();
                    }
                }

                return cmiSettings[name] ?? ConfigurationManager.AppSettings[name];
            }
            set
            {
                if (cmiSettings == null)
                {
                    cmiSettings = new NameValueCollection();
                }

                cmiSettings.Set(name, value);
            }
        }
    }
}