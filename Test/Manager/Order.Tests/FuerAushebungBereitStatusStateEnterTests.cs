using CMI.Access.Common;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;
using MassTransit;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace CMI.Manager.Order.Tests
{
    [TestFixture]
    public class FuerAushebungBereitStatusStateEnterTests
    {
        private StatuswechselContext auftragStatusContext;
        private FuerAushebungBereitStatus fuerAushebungBereitStatus;
        private ElasticArchiveRecord record;
        private OrderItem orderItem;
        private Ordering ordering;
        private Mock<ISearchIndexDataAccess> searchIndexAccess;
        private Mock<IOrderDataAccess> orderDataAccess;
        private Mock<IBus> busMock;
        private IDictionary<string, object> customFields;

        [SetUp]
        public void Init()
        {
            // arrange
            customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");
            customFields.Add("publikationsrechte", "ABC");
            record = new ElasticArchiveRecord()
            {
                ExternalKeys =
                [
                    new()
                    {
                        Value = "123456789",
                        Key = "scopeArchiv"
                    }
                ],
                ArchiveRecordId = "Arch 9090",
                FieldAccessTokens = [AccessRoles.RoleBAR],
                MetadataAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataDownloadAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataFulltextAccessTokens = [AccessRoles.RoleBAR],
                Containers = [
                    new ElasticContainer()
                    {
                        ContainerCode = "Container123",
                        ContainerLocation = "Shelf A3",
                        ContainerType = "Box",
                        IdName = "ContainerId",
                        ContainerCarrierMaterial = "Cardboard"
                    }
                ],
            };
            // Zweite Ve ist mit "Container888" verbunden
            var customFields2 = new ExpandoObject() as IDictionary<string, object>;
            customFields2.Add("zugänglichkeitGemässBga", "Frei zugänglich");
            var record2 = new ElasticArchiveRecord()
            {
                ExternalKeys =
                [
                new()
                    {
                        Value = "78784545",
                        Key = "scopeArchiv"
                    }
                ],
                ArchiveRecordId = "VZ 90904040",
                FieldAccessTokens = [AccessRoles.RoleBAR],
                MetadataAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataDownloadAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataFulltextAccessTokens = [AccessRoles.RoleBAR],
                CustomFields = customFields2,
                Containers = [
                     new ElasticContainer()
                    {
                        ContainerCode = "Container888",
                        ContainerLocation = "Shelf A3",
                        ContainerType = "Box",
                        IdName = "ContainerId",
                        ContainerCarrierMaterial = "Cardboard"
                    }
                ]
            };
            orderItem = new OrderItem
            {
                VeId = record.ArchiveRecordId,
                BehaeltnisNummer = record.Containers[0].ContainerCode
            };

            ordering = new Ordering
            {
                Items =[orderItem]
            };

            var postCommitActionsRegistry = new PostCommitActionsRegistry();
            orderDataAccess = new Mock<IOrderDataAccess>();
            busMock = new Mock<IBus>();

            searchIndexAccess = new Mock<ISearchIndexDataAccess>();
            searchIndexAccess.Setup(x => x.FindDocumentWithoutSecurity(orderItem.VeId, MetadataToExclude.OCRContentAndFiles)).Returns(Task.FromResult(record));
            searchIndexAccess.Setup(x => x.FindDocument(
                new Dictionary<string, string>()
                {
                    {"containers.containerCode", "Container123"}
                }, 10000)).Returns(Task.FromResult(new List<ElasticArchiveRecord> { record }));


            searchIndexAccess.Setup(x => x.FindDocument(
              new Dictionary<string, string>()
              {
                  {"containers.containerCode", "Container888"}
              }, 10000)).Returns(Task.FromResult(new List<ElasticArchiveRecord> { record, record2 }));

            auftragStatusContext = new StatuswechselContext(orderItem, ordering, null, new User(), new User(), new List<StatusHistory>(),
                  DateTime.Now, searchIndexAccess.Object, busMock.Object, orderDataAccess.Object, postCommitActionsRegistry);

            fuerAushebungBereitStatus = new FuerAushebungBereitStatusTest(auftragStatusContext);
        }

        [Test]
        public void OnStateEnter_ShouldSetCorrectAushebungstyp_Dossier_ForLesesaalausleihenWith_One_Container_in_Schutzfrist()
        {
            // arrange          
            customFields.Add("zugänglichkeitGemässBga", "In Schutzfrist");
            ordering.Type = OrderType.Lesesaalausleihen;
            orderItem.ApproveStatus = ApproveStatus.FreigegebenAusserhalbSchutzfrist;
            record.CustomFields = customFields;

            // Act
            fuerAushebungBereitStatus.OnStateEnter();
            // Assert
            Assert.AreEqual(Aushebungstyp.Dossier, orderItem.Aushebungstyp);
        }
      
        [Test]
        public void OnStateEnter_ShouldSetCorrectAushebungstyp_Dossier_ForLesesaalausleihenWith_Two_Container_in_Schutzfrist()
        {
            // arrange          
            customFields.Add("zugänglichkeitGemässBga", "In Schutzfrist");
            ordering.Type = OrderType.Lesesaalausleihen;
            var recordWithTwoContainers = new ElasticArchiveRecord()
            {
                ExternalKeys =
                [
                    new()
                    {
                        Value = "159426",
                        Key = "scopeArchiv"
                    }
                ],
                ArchiveRecordId = "VZ 78sdsasfs-0815",
                FieldAccessTokens = [AccessRoles.RoleBAR],
                MetadataAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataDownloadAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataFulltextAccessTokens = [AccessRoles.RoleBAR],
                CustomFields = customFields,
                Containers = [
                    new ElasticContainer()
                    {
                        ContainerCode = "Container123",
                        ContainerLocation = "Shelf A3",
                        ContainerType = "Box",
                        IdName = "ContainerId",
                        ContainerCarrierMaterial = "Cardboard"
                    },
                     new ElasticContainer()
                    {
                        ContainerCode = "Container888",
                        ContainerLocation = "Shelf A3",
                        ContainerType = "Box",
                        IdName = "ContainerId",
                        ContainerCarrierMaterial = "Cardboard"
                    }
                ]
            };

            searchIndexAccess.Setup(x => x.FindDocumentWithoutSecurity(recordWithTwoContainers.ArchiveRecordId, MetadataToExclude.OCRContentAndFiles)).
                Returns(Task.FromResult(recordWithTwoContainers));

            orderItem.BehaeltnisNummer = recordWithTwoContainers.Containers[0].ContainerCode;
            orderItem.ApproveStatus = ApproveStatus.FreigegebenAusserhalbSchutzfrist;
            orderItem.VeId = recordWithTwoContainers.ArchiveRecordId;
            record.CustomFields = customFields;

            // Act
            fuerAushebungBereitStatus.OnStateEnter();
            // Assert
            Assert.AreEqual(Aushebungstyp.Dossier, orderItem.Aushebungstyp);
        }

        [Test]
        public void OnStateEnter_ShouldSetCorrectAushebungstyp_Behältnis_ForLesesaalausleihenWith_Two_Container_Frei_zugänglich()
        {
            // arrange          
            customFields.Add("zugänglichkeitGemässBga", "Frei zugänglich");
            ordering.Type = OrderType.Lesesaalausleihen;
            var recordWithTwoContainers = new ElasticArchiveRecord()
            {
                ExternalKeys =
                [
                    new()
                    {
                        Value = "159426",
                        Key = "scopeArchiv"
                    }
                ],
                ArchiveRecordId = "VZ 78sdsasfs-0815",
                FieldAccessTokens = [AccessRoles.RoleBAR],
                MetadataAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataDownloadAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataFulltextAccessTokens = [AccessRoles.RoleBAR],
                CustomFields = customFields,
                Containers = [
                    new ElasticContainer()
                    {
                        ContainerCode = "Container123",
                        ContainerLocation = "Shelf A3",
                        ContainerType = "Box",
                        IdName = "ContainerId",
                        ContainerCarrierMaterial = "Cardboard"
                    },
                     new ElasticContainer()
                    {
                        ContainerCode = "Container888",
                        ContainerLocation = "Shelf A3",
                        ContainerType = "Box",
                        IdName = "ContainerId",
                        ContainerCarrierMaterial = "Cardboard"
                    }
                ],
            };

            searchIndexAccess.Setup(x => x.FindDocumentWithoutSecurity(recordWithTwoContainers.ArchiveRecordId, MetadataToExclude.OCRContentAndFiles)).
                Returns(Task.FromResult(recordWithTwoContainers));

            orderItem.BehaeltnisNummer = recordWithTwoContainers.Containers[0].ContainerCode;
            orderItem.ApproveStatus = ApproveStatus.FreigegebenAusserhalbSchutzfrist;
            orderItem.VeId = recordWithTwoContainers.ArchiveRecordId;
            record.CustomFields = customFields;

            // Act
            fuerAushebungBereitStatus.OnStateEnter();
            // Assert
            Assert.AreEqual(Aushebungstyp.Behältnis, orderItem.Aushebungstyp);
        }

        [Test]
        public void OnStateEnter_ShouldSetCorrectAushebungstyp_Behältnis_ForLesesaalausleihenWith_One_Container_FreigegebenAusserhalbSchutzfrist_Prüfung_nötig()
        {
            // arrange          
            customFields.Add("zugänglichkeitGemässBga", "Prüfung nötig");
            record.CustomFields = customFields;
            ordering.Type = OrderType.Lesesaalausleihen;
            orderItem.ApproveStatus = ApproveStatus.FreigegebenAusserhalbSchutzfrist;

            // Act
            fuerAushebungBereitStatus.OnStateEnter();
            // Assert
            Assert.AreEqual(Aushebungstyp.Behältnis, orderItem.Aushebungstyp);
        }

        [Test]
        public void OnStateEnter_ShouldSetCorrectAushebungstyp_Dossier_ForLesesaalausleihenWith_Two_Container_With_Two_Ves_Prüfung_nötig_and_Freizugänglich()
        {
            // arrange          
            customFields.Add("zugänglichkeitGemässBga", "Prüfung nötig");
            ordering.Type = OrderType.Lesesaalausleihen;

            var recordWithTwoContainers = new ElasticArchiveRecord()
            {
                ExternalKeys =
                [
                    new()
                    {
                        Value = "159426",
                        Key = "scopeArchiv"
                    }
                ],
                ArchiveRecordId = "VZ 78sdsasfs-0815",
                FieldAccessTokens = [AccessRoles.RoleBAR],
                MetadataAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataDownloadAccessTokens = [AccessRoles.RoleBAR],
                PrimaryDataFulltextAccessTokens = [AccessRoles.RoleBAR],
                CustomFields = customFields,
                Containers = [
                    new ElasticContainer()
                    {
                        ContainerCode = "Container123",
                        ContainerLocation = "Shelf A3",
                        ContainerType = "Box",
                        IdName = "ContainerId",
                        ContainerCarrierMaterial = "Cardboard"
                    },
                     new ElasticContainer()
                    {
                        ContainerCode = "Container888",
                        ContainerLocation = "Shelf A3",
                        ContainerType = "Box",
                        IdName = "ContainerId",
                        ContainerCarrierMaterial = "Cardboard"
                    }
                ],
            };

            searchIndexAccess.Setup(x => x.FindDocumentWithoutSecurity(recordWithTwoContainers.ArchiveRecordId, MetadataToExclude.OCRContentAndFiles)).Returns(Task.FromResult(recordWithTwoContainers));

            orderItem.ApproveStatus = ApproveStatus.FreigegebenAusserhalbSchutzfrist;
            orderItem.VeId = recordWithTwoContainers.ArchiveRecordId;
            record.CustomFields = customFields;

            // Act
            fuerAushebungBereitStatus.OnStateEnter();
            // Assert
            Assert.AreEqual(Aushebungstyp.Dossier, orderItem.Aushebungstyp);
        }

        [Test]
        public void OnStateEnter_ShouldSetCorrectAushebungstyp_Dossier_ForLesesaalausleihenWith_one_Container_FreigegebenInSchutzfrist_and_Prüfung_nötig()
        {
            // arrange          
            customFields.Add("zugänglichkeitGemässBga", "Prüfung nötig");
            ordering.Type = OrderType.Lesesaalausleihen;
            orderItem.ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist;
            record.CustomFields = customFields;

            // Act
            fuerAushebungBereitStatus.OnStateEnter();
            // Assert
            Assert.AreEqual(Aushebungstyp.Dossier, orderItem.Aushebungstyp);
        }

        [Test]
        public void OnStateEnter_ShouldSetCorrectAushebungstyp_Dossier_ForLesesaalausleihenWith_one_Container_ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist_and_Prüfung_nötig()
        {
            // arrange          
            customFields.Add("zugänglichkeitGemässBga", "Prüfung nötig");
            ordering.Type = OrderType.Lesesaalausleihen;
            orderItem.ApproveStatus = ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist;
            record.CustomFields = customFields;

            // Act
            fuerAushebungBereitStatus.OnStateEnter();
            // Assert
            Assert.AreEqual(Aushebungstyp.Dossier, orderItem.Aushebungstyp);
        }

        [Test]
        public void OnStateEnter_ShouldSetCorrectAushebungstyp_Behältnis_ForLesesaalausleihenWith_One_Container_frei_zugänglich()
        {
            // arrange          
            customFields.Add("zugänglichkeitGemässBga", "Frei zugänglich");
            ordering.Type = OrderType.Lesesaalausleihen;
            
            record.CustomFields = customFields;

            // Act
            fuerAushebungBereitStatus.OnStateEnter();
            // Assert
            Assert.AreEqual(Aushebungstyp.Behältnis, orderItem.Aushebungstyp);
        }
    }


    public class FuerAushebungBereitStatusTest : FuerAushebungBereitStatus
    {
        public override StatuswechselContext Context { get; protected set; }

        public FuerAushebungBereitStatusTest(StatuswechselContext Context)
        {
            this.Context = Context;
        }
    }
}
