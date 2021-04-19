using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;

namespace CMI.Engine.MailTemplate
{
    public class Person
    {
        private User user;

        private Person()
        {
        }


        public string EmailAddress => user.EmailAddress;

        public string Name => user.FamilyName;

        public string Vorname => user.FirstName;

        public string Ländercode => user.CountryCode;

        public string Strasse => user.Street;

        public string Plz => user.ZipCode;

        public string Ort => user.Town;

        public string Organisation => user.Organization;

        public string Sprache => user.Language;

        public bool IstDeutsch => user.Language == "de" || string.IsNullOrWhiteSpace(user.Language);

        public bool IstFranzösisch => user.Language == "fr";

        public bool IstItalienisch => user.Language == "it";

        public bool IstEnglisch => user.Language == "en";

        public string Geburtsdatum => user.Birthday?.ToString("d.M.yyyy") ?? string.Empty;

        public bool HatFlagBarInterneKonsultation => user.BarInternalConsultation;

        public string Id => user.Id;

        public bool IstAusland => !string.IsNullOrWhiteSpace(user.CountryCode) && user.CountryCode != "CH";

        public bool IstÖ2 => user.RolePublicClient == AccessRoles.RoleOe2;


        public static Person FromUser(User user)
        {
            var p = new Person();
            p.user = user;
            return p;
        }
    }
}