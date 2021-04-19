using System.Collections.Generic;
using CMI.Contract.Order;

namespace CMI.Engine.MailTemplate
{
    public interface IDataBuilder
    {
        IDataBuilder AddUser(string userId);
        IDataBuilder AddBesteller(string bestellerId);
        IDataBuilder AddBestellung(Ordering ordering);
        IDataBuilder AddVe(string archiveRecordId);
        IDataBuilder AddVeList(IEnumerable<string> archiveRecordIdList);
        IDataBuilder AddVeList(List<InElasticIndexierteVe> veList);

        /// <param name="sprachCode">Z.B. de</param>
        IDataBuilder AddSprache(string sprachCode);

        IDataBuilder AddAuftraege(IEnumerable<int> orderItemIds);
        IDataBuilder AddAuftraege(Ordering ordering, IEnumerable<OrderItem> items, string propertyName);
        IDataBuilder AddAuftrag(Ordering ordering, OrderItem item);
        IDataBuilder AddValue(string propertyName, object value);
        IDataBuilder AddBestellerMitAuftraegen(int[] orderItemIds);

        List<Auftrag> GetAuftraege(IEnumerable<int> orderItemIds);

        /// <summary>
        ///     Entfernt sämtliche zuvor hinzugefügte Objekte (Aufträge, Benutzer) aus dem Builder
        /// </summary>
        void Reset();

        dynamic Create();
    }
}