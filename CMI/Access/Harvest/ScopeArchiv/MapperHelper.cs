using System;
using CMI.Contract.Common;

namespace CMI.Access.Harvest.ScopeArchiv
{
    internal static class MapperHelper
    {
        public static DateRangeDateOperator MapDateOperator(ScopeArchivDateOperator scopeArchivDateOperator)
        {
            switch (scopeArchivDateOperator)
            {
                case ScopeArchivDateOperator.Between:
                    return DateRangeDateOperator.between;
                case ScopeArchivDateOperator.FromTo:
                    return DateRangeDateOperator.fromTo;
                case ScopeArchivDateOperator.After:
                    return DateRangeDateOperator.after;
                case ScopeArchivDateOperator.From:
                    return DateRangeDateOperator.startingWith;
                case ScopeArchivDateOperator.Before:
                    return DateRangeDateOperator.before;
                case ScopeArchivDateOperator.To:
                    return DateRangeDateOperator.to;
                case ScopeArchivDateOperator.SineDato:
                    return DateRangeDateOperator.sd;
                case ScopeArchivDateOperator.Exact:
                    return DateRangeDateOperator.exact;
                case ScopeArchivDateOperator.None:
                    return DateRangeDateOperator.na;
                case 0:
                    return DateRangeDateOperator.exact;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scopeArchivDateOperator), scopeArchivDateOperator, null);
            }
        }

        public static DataElementElementType MapDataElementType(ScopeArchivDatenElementTyp datenElementTyp)
        {
            switch (datenElementTyp)
            {
                case ScopeArchivDatenElementTyp.DateiVerknuepfung:
                    return DataElementElementType.fileLink;
                case ScopeArchivDatenElementTyp.Datumsbereich:
                    return DataElementElementType.dateRange;
                case ScopeArchivDatenElementTyp.EinzeldatumPraezis:
                    return DataElementElementType.datePrecise;
                case ScopeArchivDatenElementTyp.FestkommaZahl:
                    return DataElementElementType.@float;
                case ScopeArchivDatenElementTyp.GanzeZahl:
                    return DataElementElementType.integer;
                case ScopeArchivDatenElementTyp.JaNein:
                    return DataElementElementType.boolean;
                case ScopeArchivDatenElementTyp.Text:
                    return DataElementElementType.text;
                case ScopeArchivDatenElementTyp.Memo:
                    return DataElementElementType.memo;
                case ScopeArchivDatenElementTyp.Uhrzeit:
                    return DataElementElementType.time;
                case ScopeArchivDatenElementTyp.WebHyperlink:
                    return DataElementElementType.hyperlink;
                case ScopeArchivDatenElementTyp.Zwischentitel:
                    return DataElementElementType.header;
                case ScopeArchivDatenElementTyp.Auswahlliste:
                    return DataElementElementType.selection;
                case ScopeArchivDatenElementTyp.Zugänge:
                    return DataElementElementType.accrual;
                case ScopeArchivDatenElementTyp.Einzeldatum:
                    return DataElementElementType.date;
                case ScopeArchivDatenElementTyp.Bild:
                    return DataElementElementType.image;
                case ScopeArchivDatenElementTyp.MailLink:
                    return DataElementElementType.mailLink;
                case ScopeArchivDatenElementTyp.Verknüpfung:
                    return DataElementElementType.entityLink;
                case ScopeArchivDatenElementTyp.Spieldauer:
                    return DataElementElementType.timespan;
                case ScopeArchivDatenElementTyp.AudioVideo:
                    return DataElementElementType.media;
                default:
                    throw new ArgumentOutOfRangeException(nameof(datenElementTyp), datenElementTyp, null);
            }
        }
    }
}