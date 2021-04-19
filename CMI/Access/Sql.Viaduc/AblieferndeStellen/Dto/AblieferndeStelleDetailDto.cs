using System;
using System.Collections.Generic;
using System.Text;

namespace CMI.Access.Sql.Viaduc.AblieferndeStellen.Dto
{
    public class AblieferndeStelleDetailDto : AblieferndeStelleDto
    {
        public List<UserInfo> ApplicationUserList { get; set; } = new List<UserInfo>();

        public List<AblieferndeStelleTokenDto> AblieferndeStelleTokenList { get; set; } = new List<AblieferndeStelleTokenDto>();

        /// <summary>Das Feld enthält 0 bis n Mailadressen</summary>
        public List<string> Kontrollstellen { get; set; } = new List<string>();


        public DateTime CreatedOn { get; set; }

        public string CreatedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public string ModifiedBy { get; set; }

        public string CreateModifyData
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Erstellt am:\t\t\t\t{CreatedOn:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Erstellt von:\t\t\t\t{CreatedBy}");
                sb.AppendLine($"Letzte Änderung am:\t{ModifiedOn:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Letzte Änderung von:\t{ModifiedBy}");
                return sb.ToString();
            }
        }
    }
}