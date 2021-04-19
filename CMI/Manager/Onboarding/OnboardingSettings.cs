using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Onboarding
{
    public class OnboardingSettings : ISetting
    {
        [Description("Wird für den SFTP Zugriff verwendet")]
        [DefaultValue("138.190.190.21")]
        public string SftpServerName { get; set; }

        [DefaultValue("1029_BAR")] 
        public string SftpUserName { get; set; }

        [Description("Wird für das SFTP Login verwendet")]
        public string SshPrivateKey { get; set; }

        [Description("Wird für das SFTP Login verwendet")]
        public string SshPrivateKeyPassword { get; set; }

        [Description("Wird verwendet, um die ZIP Dateien zu entschlüsseln")]
        public string PgpPrivateKey { get; set; }

        [Description("Wird verwendet, um die ZIP Dateien zusammen mit dem PrivateKey zu entschlüsseln")]
        public string PgpPrivateKeyPassword { get; set; }
    }
}