using CMI.Contract.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Manager.Order.Status
{
    public class FuerAushebungBereitStatus : AuftragStatus
    {
        private static readonly Lazy<FuerAushebungBereitStatus> lazy =
            new Lazy<FuerAushebungBereitStatus>(() => new FuerAushebungBereitStatus());

        public static FuerAushebungBereitStatus Instance => lazy.Value;

        public override OrderStatesInternal OrderStateInternal => OrderStatesInternal.FuerAushebungBereit;

        public override void Abbrechen(Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung)
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Verwaltungsausleihe,
                OrderType.Digitalisierungsauftrag,
                OrderType.Lesesaalausleihen
            });
            this.AbbrechenInternal(abbruchgrund, bemerkungZumDossier, interneBemerkung);
        }

        public override void Zuruecksetzen()
        {
            Context.ThrowIfAuftragstypIsNot(new[]
            {
                OrderType.Verwaltungsausleihe,
                OrderType.Lesesaalausleihen
            });

            this.ZuruecksetzenInternal();
        }

        public override void AushebungsauftragDrucken()
        {
            Context.SetNewStatus(AuftragStatusRepo.AushebungsauftragErstellt);
        }

        public override void OnStateEnter()
        {
            /*

            Die Regeln um den Aushebungstyp zu bestimmen sind wie folgt:

            | Auftragstyp               | Zugang gem. BGA   | Freigabestatus                            | Inhalt                        | Aushebungstyp     |
            | ---                       | ---               | ---                                       | ---                           | ---               |
            | Lesesaal                  | Frei zugänglich   | -                                         | Kein gemischtes Behältnis     | Behältnis         |
            | Lesesaal                  | Frei zugänglich   | -                                         | Gemischtes Behältnis          | Dossier           |
            | Lesesaal                  | Prüfung nötig     | «Freigegeben (ausserhalb Schutzfrist)»    | Kein gemischtes Behältnis     | Behältnis         |
            | Lesesaal                  | Prüfung nötig     | «Freigegeben (ausserhalb Schutzfrist)»    | Gemischtes Behältnis          | Dossier           |
            | Lesesaal                  | Prüfung nötig     | Alle anderen Freigabestatus (ink. keine)  | -                             | Dossier           |
            | Lesesaal                  | In Schutzfrist    | -                                         | -                             | Dossier           |
            | Verwaltungsausleihe       | -                 | -                                         | -                             | Dossier           |
            | Digitalisierungsauftrag   | -                 | -                                         | -                             | Behältnis         |

            Ein Behältnis zählt als «gemischtes» Behältnis, sobald es Dossiers im Behältnis gibt, die unterschiedlichen Status
            enthalten (Frei zugänglich + Prüfung nötig; Frei zugänglich + In Schutzfrist; Prüfung nötig + In Schutzfrist; alle drei Status).
            Besteht ein Dossier aus mehreren Behältnissen, ist die Kontrolle für jedes einzelne Behältnis nötig.
             */
            
            // Bei Aufträgen ohne Ve (=Formularbestellung) -> immer Dossier
            if (Context.OrderItem.VeId == null || string.IsNullOrWhiteSpace(Context.OrderItem.BehaeltnisNummer))
            {
                Context.OrderItem.Aushebungstyp = Aushebungstyp.Dossier;
            }
            else switch (Context.Ordering.Type)
            {
                case OrderType.Verwaltungsausleihe:
                    Context.OrderItem.Aushebungstyp = Aushebungstyp.Dossier;
                    break;
                case OrderType.Digitalisierungsauftrag:
                    Context.OrderItem.Aushebungstyp = Aushebungstyp.Behältnis;
                    break;
                case OrderType.Lesesaalausleihen:
                {
                    if (Context.Besteller.BarInternalConsultation)
                    {
                        Context.OrderItem.Aushebungstyp = Aushebungstyp.Behältnis;
                    }
                    else
                    {
                        var record = Context.IndexAccess.FindDocumentWithoutSecurity(Context.OrderItem.VeId, MetadataToExclude.OCRContentAndFiles)
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();
                        if (record == null)
                        {
                            throw new InvalidOperationException($"VE mit Id {Context.OrderItem.VeId} konnte im Index nicht gefunden werden.");
                        }

                        // Get all ve's from all containers
                        var recordsInAllContainers = new List<ElasticArchiveRecord>();

                        foreach (var container in record.Containers)
                        {
                            recordsInAllContainers.AddRange(Context.IndexAccess.FindDocument(
                                    new Dictionary<string, string>
                                    {
                                        {"containers.containerCode", container.ContainerCode}
                                    },
                                    // lese alle Einträge aus
                                    10000).ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult());
                        }


                        var isApprovedOutsideProtectionPeriod =
                            record.CustomFields.zugänglichkeitGemässBga == "Prüfung nötig" &&
                            Context.OrderItem.ApproveStatus == ApproveStatus.FreigegebenAusserhalbSchutzfrist;

                        // Dann wird der Record wie frei zugänglich behandelt.
                        var effectiveAccessibility = isApprovedOutsideProtectionPeriod
                            ? "Frei zugänglich"
                            : record.CustomFields.zugänglichkeitGemässBga;

                        // If we have more than one group, when grouping by accessibility, then we have mixed container
                        var mixedContainer = recordsInAllContainers
                            .Select(v => v.ArchiveRecordId == record.ArchiveRecordId
                                ? effectiveAccessibility
                                : v.ZugaenglichkeitGemaessBga())
                            .Distinct()
                            .Count() != 1;

                        if ((effectiveAccessibility == "In Schutzfrist" ||
                             (effectiveAccessibility == "Prüfung nötig") ||
                             (effectiveAccessibility == "Frei zugänglich" && mixedContainer)))
                        {
                            Context.OrderItem.Aushebungstyp = Aushebungstyp.Dossier;
                        }
                        else
                        {
                            Context.OrderItem.Aushebungstyp = Aushebungstyp.Behältnis;
                        }
                    }

                    break;
                }
            }


            Context.OrderDataAccess.UpdateOrderItem(Context.OrderItem)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult(); // Update ist nötig, damit die Aufräge die in der selben Transaktion verarbeitet werden den richtigen Digitalisierungtermin erhalten
        }
    }
}
