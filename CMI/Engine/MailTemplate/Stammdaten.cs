using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Messaging;
using MassTransit;

namespace CMI.Engine.MailTemplate
{
    public class Stammdaten
    {
        private readonly string bezeichnungDerStammdaten;
        private readonly IEnumerable<int> idList;


        public Stammdaten(IEnumerable<int> idList, string bezeichnungDerStammdaten)
        {
            this.idList = idList;
            this.bezeichnungDerStammdaten = bezeichnungDerStammdaten;
        }


        public static IBus Bus { get; set; }


        public string Deutsch => GetText("de");

        public string Französisch => GetText("fr");

        public string Italienisch => GetText("it");

        public string Englisch => GetText("en");


        private string GetText(string language)
        {
            var client = DataBuilder.CreateRequestClient<GetStammdatenRequest, GetStammdatenResponse>(Bus, BusConstants.ReadStammdatenQueue);
            var result = client.Request(new GetStammdatenRequest {BezeichnungDerStammdaten = bezeichnungDerStammdaten, Language = language})
                .GetAwaiter().GetResult();

            var names = result.NamesAndIds.Where(e => idList.Contains(e.Id)).Select(e => e.Name);

            return string.Join(" / ", names);
        }
    }
}