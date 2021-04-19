using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;

namespace CMI.Manager.Order.Tests
{
    internal class MockDigipoolAccess : IDigipoolDataAccess
    {
        private readonly Dictionary<int, OrderItem> data;

        public MockDigipoolAccess(Dictionary<int, OrderItem> data)
        {
            this.data = data;
        }

        public Task<DigipoolEntry[]> GetDigipool()
        {
            return Task.FromResult(
                data.Values
                    .Where(d => d.Status == OrderStatesInternal.FuerDigitalisierungBereit)
                    .Select(d =>
                        new DigipoolEntry
                        {
                            OrderItemId = d.Id,
                            TerminDigitalisierung = d.TerminDigitalisierung.Value,
                            Digitalisierunskategorie = (int) d.DigitalisierungsKategorie,
                            UserId = d.SachbearbeiterId // Fake UserId
                        })
                    .ToArray());
        }

        public Task UpdateDigipool(List<int> orderItemIds, int? digitalisierungsKategorie, DateTime? terminDigitalisierung)
        {
            foreach (var orderItemId in orderItemIds)
            {
                data[orderItemId].TerminDigitalisierung = terminDigitalisierung;
            }

            return Task.CompletedTask;
        }

        public Task UpdateTermin(int orderItemId, DateTime terminDigitalisierung)
        {
            data[orderItemId].TerminDigitalisierung = terminDigitalisierung;
            return Task.CompletedTask;
        }

        public Task<List<DigitalisierungsTermin>> GetLatestDigitalisierungsTermine(string userId, DateTime fromDate,
            DigitalisierungsKategorie kategorie)
        {
            var termine = data.Values
                .Where(d => d.Status != OrderStatesInternal.Abgebrochen
                            && d.Status != OrderStatesInternal.DigitalisierungAbgebrochen
                            && d.TerminDigitalisierung.HasValue
                            && (string.IsNullOrWhiteSpace(userId) || d.SachbearbeiterId == userId)
                            && d.DigitalisierungsKategorie == kategorie
                            && d.TerminDigitalisierung >= fromDate)
                .GroupBy(d => d.TerminDigitalisierung)
                .Select(d => new DigitalisierungsTermin
                {
                    AnzahlAuftraege = d.Count(),
                    Termin = d.Key.Value
                })
                .ToList();

            return Task.FromResult(termine);
        }
    }
}