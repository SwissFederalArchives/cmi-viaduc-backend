using System;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using Serilog;

namespace CMI.Manager.Order.Status
{
    public class ZumReponierenBereitStatus : AuftragStatus
    {
        private static readonly Lazy<ZumReponierenBereitStatus> lazy =
            new Lazy<ZumReponierenBereitStatus>(() => new ZumReponierenBereitStatus());

        private ZumReponierenBereitStatus()
        {
        }

        public static ZumReponierenBereitStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.ZumReponierenBereit;

        public override void Abschliessen()
        {
            this.AbschliessenInternal();
        }

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            this.AbbrechenInternal(abbruchgrund, bemerkungZumDossier, interneBemerkung);
        }

        public override void OnStateEnter()
        {
            Log.Information("Entering state {ZumReponierenBereitStatus}", nameof(ZumReponierenBereitStatus));

            // Wenn dieser Status eintritt, ist der Digitalisierungsauftrag "abgeschlossen"
            // Über den MessageBus event DigitalisierungsAuftragErledigt wird der Download ausgelöst, als hätte der Benutzer selber 
            // den Download Button im Web-Client geklickt. Entsprechend wird das Paket aufbereitet, im Cache abgelegt und
            // die Vollzugsmeldung über Email verschickt.
            if (Context.Ordering.Type == OrderType.Digitalisierungsauftrag)
            {
                if (!Context.OrderItem.Benutzungskopie.HasValue || !Context.OrderItem.Benutzungskopie.Value)
                {
                    var ep = Context.Bus.GetSendEndpoint(new Uri(Context.Bus.Address, BusConstants.DigitalisierungsAuftragErledigtEvent)).GetAwaiter()
                        .GetResult();
                    ep.Send<IDigitalisierungsAuftragErledigt>(new
                    {
                        ArchiveRecordId = Context.OrderItem.VeId,
                        OrderItemId = Context.OrderItem.Id,
                        Context.Ordering.OrderDate,
                        OrderUserId = Context.Ordering.UserId,
                        OrderUserRolePublicClient = Context.Besteller.RolePublicClient
                    }).Wait();
                    Log.Information("Sent {IDigitalisierungsAuftragErledigt} message on bus", nameof(IDigitalisierungsAuftragErledigt));
                }
                else
                {
                    var ep = Context.Bus.GetSendEndpoint(new Uri(Context.Bus.Address, BusConstants.BenutzungskopieAuftragErledigtEvent)).GetAwaiter()
                        .GetResult();
                    ep.Send<IBenutzungskopieAuftragErledigt>(new
                    {
                        ArchiveRecordId = Context.OrderItem.VeId,
                        OrderItemId = Context.OrderItem.Id,
                        OrderUserId = Context.Ordering.UserId,
                        OrderApproveStatus = Context.OrderItem.ApproveStatus
                    }).Wait();
                    Log.Information("Sent {IBenutzungskopieAuftragErledigt} message on bus", nameof(IBenutzungskopieAuftragErledigt));
                }
            }
        }
    }
}