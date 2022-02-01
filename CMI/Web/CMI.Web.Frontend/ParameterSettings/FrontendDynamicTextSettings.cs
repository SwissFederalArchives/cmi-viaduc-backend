using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using CMI.Contract.Parameter;

namespace CMI.Web.Frontend.ParameterSettings
{
    public class FrontendDynamicTextSettings : ISetting
    {

        #region Lieferart Auswahl 'Digital erhalten'

        [Description("Definiert den Text in der Lieferart Auswahl 'Digital erhalten'. Sprache: DE")]
        [DefaultValue("<strong>digital</strong> erhalten. Sie erhalten das digitalisierte Dossier in rund 30 Tagen. Alles Weitere zur Digitalisierung finden Sie unter <a href=\"https://www.recherche.bar.admin.ch/recherche/#/de/informationen/bestellen-und-konsultieren\" target=\"_blank\" rel=\"noopener noreferrer\">Bestellen und Konsultieren</a>.")]
        public string DeliveryTypeDigitalDE { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'Digital erhalten'. Sprache: FR")]
        [DefaultValue("Livraison <strong>numérique</strong>. Vous recevrez le dossier numérisé dans un délai d'environ 30 jours. Vous trouverez toutes les informations concernant la numérisation dans la rubrique <a href=\"https://www.recherche.bar.admin.ch/recherche/#/fr/informations/commande-et-consultation\" target=\"_blank\" rel=\"noopener noreferrer\">Commande et consultation</a>.")]
        public string DeliveryTypeDigitalFR { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'Digital erhalten'. Sprache: IT")]
        [DefaultValue("in forma <strong>digitale</strong>. Riceverai il dossier digitalizzato tra circa 30 giorni. Tutte le informazioni riguardanti la digitalizzazione le trovi alla voce <a href=\"https://www.recherche.bar.admin.ch/recherche/#/it/informazioni/ordinazione-e-consultazione\" target=\"_blank\" rel=\"noopener noreferrer\">Ordinazione e consultazione</a>.")]
        public string DeliveryTypeDigitalIT { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'Digital erhalten'. Sprache: EN")]
        [DefaultValue("for <strong>digital</strong> delivery. You will receive the digitised dossier in approximately 30 days. For more information about digitisation, go to <a href=\"https://www.recherche.bar.admin.ch/recherche/#/en/information/ordering-and-consulting\" target=\"_blank\" rel=\"noopener noreferrer\">Ordering and consulting</a>.")]
        public string DeliveryTypeDigitalEN { get; set; }

        #endregion


        #region Lieferart Auswahl 'In den Lesesaal bestellen'

        [Description("Definiert den Text in der Lieferart Auswahl 'In den Lesesaal bestellen'. Sprache: DE")]
        [DefaultValue("zur Konsultation in den <strong>Lesesaal</strong> bestellen. Bestellen Sie 24 Stunden im Voraus, damit Ihnen die Unterlagen am gewünschten Tag zur Verfügung stehen (Dienstag, Mittwoch und Donnerstag).")]
        public string DeliveryTypeReadingRoomDE { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'In den Lesesaal bestellen'. Sprache: FR")]
        [DefaultValue("Consultation en <strong>salle de lecture</strong>. Prière de commander 24h à l’avance pour recevoir les documents le jour désiré (mardi, mercredi et jeudi).")]
        public string DeliveryTypeReadingRoomFR { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'In den Lesesaal bestellen'. Sprache: IT")]
        [DefaultValue("per consultazione nelle <strong>sale di lettura</strong>. Se la richiesta è effettuata 24 ore prima, i documenti sono disponibili il giorno desiderato nelle sale di lettura (martedì, mercoledì e giovedì).")]
        public string DeliveryTypeReadingRoomIT { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'In den Lesesaal bestellen'. Sprache: EN")]
        [DefaultValue("for consultation in the <strong>reading room</strong>. Order 24 hours in advance to ensure that your documents are ready for you on your desired day (Tuesday, Wednesday and Thursday).")]
        public string DeliveryTypeReadingRoomEN { get; set; }

        #endregion


        #region Lieferart Auswahl 'Ins Amt bestellen'

        [Description("Definiert den Text in der Lieferart Auswahl 'Ins Amt bestellen'. Sprache: DE")]
        [DefaultValue("ins <strong>Amt</strong> bestellen (Lieferfrist: ein bis zwei Arbeitstage)")]
        public string DeliveryTypeCommissionDE { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'Ins Amt bestellen'. Sprache: FR")]
        [DefaultValue("Livraison dans votre <strong>service</strong> (délai de livraison: un à deux jours ouvrés)")]
        public string DeliveryTypeCommissionFR { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'Ins Amt bestellen'. Sprache: IT")]
        [DefaultValue("per consultazione nell'<strong>unità amministrativa</strong> (termine di consegna: 1-2 giorni lavorativi)")]
        public string DeliveryTypeCommissionIT { get; set; }

        [Description("Definiert den Text in der Lieferart Auswahl 'Ins Amt bestellen'. Sprache: EN")]
        [DefaultValue("for delivery to the <strong>office</strong> (delivery takes one to two working days)")]
        public string DeliveryTypeCommissionEN { get; set; }

        #endregion
    }
}