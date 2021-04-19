using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using Serilog;

// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

namespace CMI.Manager.Order
{
    public class DigitalisierungsTerminManager
    {
        private readonly IDigipoolDataAccess orderDataAccess;

        public DigitalisierungsTerminManager(IDigipoolDataAccess orderDataAccess)
        {
            this.orderDataAccess = orderDataAccess;
        }

        public DateTime GetNextPossibleTermin(DateTime orderDate, List<DigitalisierungsTermin> nextTermine, DigitalisierungsKontingent kontingent)
        {
            var isWeekendDay = IsWeekendDay(orderDate);
            var termin = CreateReferenzTermin(orderDate, isWeekendDay);

            var limite = termin.AddDays(-kontingent.InAnzahlTagen);
            var nextTermineRegardingLimit = nextTermine.Where(t => t.Termin >= limite).OrderByDescending(t => t.Termin).ToList();

            // Beträgt die Anzahl Aufträge pro Anzahl Arbeitstage (X Aufträge in Y Arbeitstage) X > 1, werden für die Berechnung des Termins  X-1 vorangehende Aufträge nicht berücksichtigt
            var toSkip = Math.Max(kontingent.AnzahlAuftraege - 1, 0);

            if (nextTermineRegardingLimit.Any() && (toSkip == 0 || toSkip == nextTermineRegardingLimit.Sum(t => t.AnzahlAuftraege)))
            {
                var nextTermin = nextTermineRegardingLimit.First();
                var anzahlAuftraege = GetAnzahlAuftraegeAnDiesemTag(nextTermine, nextTermin.Termin);

                GetPossibleTerminInternal(anzahlAuftraege, kontingent, nextTermine, nextTermin.Termin, ref termin, isWeekendDay);
            }
            else if (toSkip > 0 && nextTermineRegardingLimit.Any())
            {
                var latestTermin = termin;

                foreach (var nextTermin in nextTermineRegardingLimit.SkipWhile((dt, i) => SkipAuftraege(i, dt, toSkip)))
                {
                    var anzahlAuftraege = GetAnzahlAuftraegeAnDiesemTag(nextTermine, nextTermin.Termin);

                    if (toSkip > 0)
                    {
                        toSkip = Math.Max(toSkip - anzahlAuftraege, 0);
                        continue;
                    }

                    if (anzahlAuftraege < kontingent.AnzahlAuftraege)
                    {
                        // Es hat noch freien Platz an diesem Tag
                        termin = CreateReferenzTermin(nextTermin.Termin, isWeekendDay);
                        EnsureIsWorkingDay(ref termin);
                        return termin;
                    }

                    latestTermin = nextTermin.Termin;
                    break;
                }

                var anzahl = GetAnzahlAuftraegeAnDiesemTag(nextTermine, latestTermin);
                GetPossibleTerminInternal(anzahl, kontingent, nextTermine, latestTermin, ref termin, isWeekendDay);
            }

            EnsureIsWorkingDay(ref termin);
            return termin;
        }

        private static int GetAnzahlAuftraegeAnDiesemTag(List<DigitalisierungsTermin> nextTermine, DateTime nextTermin)
        {
            var termineVomGleichenTag = nextTermine.Where(nt => nt.Termin.Date == nextTermin.Date);
            var anzahlAuftraege = termineVomGleichenTag.Sum(t => t.AnzahlAuftraege);
            return anzahlAuftraege;
        }

        private static bool SkipAuftraege(int i, DigitalisierungsTermin digitalisierungsTermin, int toSkip)
        {
            i += digitalisierungsTermin.AnzahlAuftraege;
            return i < toSkip;
        }

        private void GetPossibleTerminInternal(int anzahlAuftraege, DigitalisierungsKontingent kontingent, List<DigitalisierungsTermin> nextTermine,
            DateTime nextTermin, ref DateTime termin, bool isWeekendDay)
        {
            if (anzahlAuftraege < kontingent.AnzahlAuftraege)
            {
                // Es hat noch freien Platz an diesem Tag
                termin = CreateReferenzTermin(nextTermin, isWeekendDay);
                return;
            }

            termin = CreateReferenzTermin(nextTermin, isWeekendDay);
            while (anzahlAuftraege >= kontingent.AnzahlAuftraege)
            {
                Hochzaehlen(kontingent, ref termin);
                termin = CreateReferenzTermin(termin, isWeekendDay);
                anzahlAuftraege = GetAnzahlAuftraegeAnDiesemTag(nextTermine, termin);
            }
        }

        public async Task RecalcTermine(DigitalisierungsKategorie kategorie, DigitalisierungsKontingent kontingent)
        {
            var digipool = (await orderDataAccess.GetDigipool()).Where(d => d.Digitalisierunskategorie == (int) kategorie).ToArray();
            var newTermine = new List<DigitalisierungsTermin>();

            foreach (var digiItem in digipool)
            {
                var letzterTerminDesBenutzers = GetAeltesterDigitalisierungsTerminDesBenutzers(kategorie, digipool, digiItem);

                var grenze = letzterTerminDesBenutzers == DateTime.MinValue
                    ? DateTime.MinValue
                    : letzterTerminDesBenutzers.AddDays(-kontingent.InAnzahlTagen);
                var termineToCheck = await orderDataAccess.GetLatestDigitalisierungsTermine(digiItem.UserId, grenze, kategorie);

                var foundEntry = termineToCheck.Where(t => t.Termin < letzterTerminDesBenutzers).OrderBy(t => t.Termin).FirstOrDefault();
                DateTime referenzTermin;
                if (foundEntry != null)
                    // Wenn ja, wird der jüngste Termin als Referenz genommen
                {
                    referenzTermin = foundEntry.Termin;
                }
                else
                    // Wenn nein, wird der älteste Termin aus dem digipool genommen
                {
                    referenzTermin = digipool.Where(d => d.UserId == digiItem.UserId).Min(d => d.TerminDigitalisierung);
                }

                var termin = referenzTermin;
                if (!newTermine.Any() && referenzTermin != letzterTerminDesBenutzers)
                {
                    Hochzaehlen(kontingent, ref termin);
                    EnsureIsWorkingDay(ref termin);
                }
                else
                {
                    termin = GetNextPossibleTermin(referenzTermin, newTermine.Where(t => t.UserId == digiItem.UserId).ToList(), kontingent);
                }

                var index = newTermine.FindIndex(n => n.Termin == termin && n.UserId == digiItem.UserId);
                if (index >= 0)
                {
                    newTermine[index].AnzahlAuftraege++;
                }
                else
                {
                    newTermine.Add(new DigitalisierungsTermin
                    {
                        AnzahlAuftraege = 1,
                        Termin = termin,
                        UserId = digiItem.UserId
                    });
                }

                Log.Debug("Neuberechnung für Auftrag {ID} beendet, vorher: {TERMINALT}, jetzt: {TERMINNEU}", digiItem.OrderItemId,
                    digiItem.TerminDigitalisierung, termin);
                await orderDataAccess.UpdateTermin(digiItem.OrderItemId, termin);
            }
        }

        private static DateTime GetAeltesterDigitalisierungsTerminDesBenutzers(DigitalisierungsKategorie kategorie, DigipoolEntry[] digipool,
            DigipoolEntry digiItem)
        {
            return digipool
                .Where(d => d.UserId == digiItem.UserId && d.Digitalisierunskategorie == (int) kategorie)
                .OrderBy(d => d.TerminDigitalisierung).FirstOrDefault()?.TerminDigitalisierung ?? DateTime.MinValue;
        }

        private static DateTime CreateReferenzTermin(DateTime basisTermin, bool zeroTime)
        {
            var termin = basisTermin;

            if (zeroTime)
                // Für Bestellungen am Wochenende wird sichergestellt, das der Termin auf den nächsten Arbeitstag als erstes drankommen
                // in dem die Zeit auf 0 gestellt wird
            {
                termin = new DateTime(basisTermin.Year, basisTermin.Month, basisTermin.Day, 0, 0, 0);
            }

            return termin;
        }

        private void Hochzaehlen(DigitalisierungsKontingent kontingent, ref DateTime termin)
        {
            for (var i = 0; i < kontingent.InAnzahlTagen; i++)
            {
                termin = termin.AddDays(1);
                EnsureIsWorkingDay(ref termin);
            }
        }

        private static bool IsWeekendDay(DateTime dt)
        {
            return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
        }

        private void EnsureIsWorkingDay(ref DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                    date = date.AddDays(2);
                    return;
                case DayOfWeek.Sunday:
                    date = date.AddDays(1);
                    break;
                default:
                    return;
            }
        }
    }
}