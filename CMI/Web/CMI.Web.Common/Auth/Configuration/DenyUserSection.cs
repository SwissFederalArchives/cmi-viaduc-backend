using System.Configuration;

namespace CMI.Web.Common.Auth.Configuration
{
    public class DenyUserSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public DenyUserCollection Instances
        {
            get => (DenyUserCollection) this[""];
            set => this[""] = value;
        }
    }

    public class DenyUserCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new DenyUserElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DenyUserElement) element).IfRole;
        }
    }

    public class DenyUserElement : ConfigurationElement
    {
        [ConfigurationProperty("ifRole", IsKey = true, IsRequired = true)]
        public string IfRole
        {
            get => (string) base["ifRole"];
            set => base["ifRole"] = value;
        }

        [ConfigurationProperty("doesNotAuthenticateWith", IsRequired = true)]
        public string DoesNotAuthenticateWith
        {
            get => (string) base["doesNotAuthenticateWith"];
            set => base["doesNotAuthenticateWith"] = value;
        }
    }
}