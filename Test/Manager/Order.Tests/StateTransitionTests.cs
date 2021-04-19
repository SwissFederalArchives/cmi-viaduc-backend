using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Order.Tests
{
    [TestFixture]
    public class Test_der_automatischen_Statuswechsel_bei_NeuEingegangen
    {
        [SetUp]
        public void Init()
        {
            orderDataAccessMock = new Mock<IOrderDataAccess>();
        }

        private Mock<IOrderDataAccess> orderDataAccessMock;

        private async Task PerformTest(User currentUser, User besteller, Ordering ordering, OrderItem item, ElasticArchiveRecord elasticArchiveRecord,
            Action<IAuftragsAktionen> aktion, IBus bus = null)
        {
            ordering.Items = new[] {item};

            orderDataAccessMock.Setup(foo => foo.GetOrdering(ordering.Id, It.IsAny<bool>())).ReturnsAsync(ordering);
            orderDataAccessMock.Setup(foo =>
                    foo.GetLatestDigitalisierungsTermine(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DigitalisierungsKategorie>()))
                .ReturnsAsync(new List<DigitalisierungsTermin>());
            orderDataAccessMock.Setup(foo => foo.GetIndividualAccessTokens(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new IndivTokens(new string [0], new string [0], new string[0]));

            var userDataAccessMock = new Mock<IUserDataAccess>();
            userDataAccessMock.Setup(foo => foo.GetUser("besteller")).Returns(besteller);

            var idxSearchMock = new Mock<ISearchIndexDataAccess>();
            idxSearchMock.Setup(foo => foo.FindDocument(item.VeId.ToString(), false)).Returns(elasticArchiveRecord);

            var statusWechsler = new StatusWechsler(orderDataAccessMock.Object, userDataAccessMock.Object, idxSearchMock.Object, bus,
                new PostCommitActionsRegistry());
            await statusWechsler.Execute(aktion, new[] {item}, currentUser, new DateTime(2019, 1, 12));
        }

        [Test]
        public async Task Ein_Digitalisierungsauftrag_eines_BAR_Benutzers_erhaelt_die_Digitalisierungskategorie_Intern()
        {
            var currentUser = new User {Id = "current"};
            var besteller = new User
                {Id = "besteller", Access = new UserAccess("besteller", AccessRoles.RoleBAR, "ALLOW", new string [0], false, "de")};

            var item = new OrderItem {OrderId = 1, Id = 1001, VeId = 200, Status = OrderStatesInternal.ImBestellkorb};
            var ordering = new Ordering {Id = 1, UserId = "besteller", Type = OrderType.Digitalisierungsauftrag, OrderDate = DateTime.Now};

            var ear = new ElasticArchiveRecord
            {
                PrimaryDataDownloadAccessTokens = new List<string>(new[] {"BAR"}),
                ArchiveRecordId = item.VeId.Value.ToString()
            };

            await PerformTest(currentUser, besteller, ordering, item, ear, p => p.Bestellen());
            item.DigitalisierungsKategorie.Should().Be(DigitalisierungsKategorie.Intern);
        }

        [Test]
        public async Task Ein_Digitalisierungsauftrag_eines_BAR_Benutzers_wird_automatisch_freigegeben_wenn_die_DownloadTokens_BAR_enthalten()
        {
            var currentUser = new User {Id = "current", FamilyName = "Boumaa", FirstName = "Bob"};
            var besteller = new User
                {Id = "besteller", Access = new UserAccess("besteller", AccessRoles.RoleBAR, "ALLOW", new string[0], false, "de")};

            var item = new OrderItem {OrderId = 1, Id = 1001, VeId = 200, Status = OrderStatesInternal.ImBestellkorb};
            var ordering = new Ordering {Id = 1, UserId = "besteller", Type = OrderType.Digitalisierungsauftrag, OrderDate = DateTime.Now};

            var ear = new ElasticArchiveRecord
            {
                PrimaryDataDownloadAccessTokens = new List<string>(new[] {"BAR"}),
                ArchiveRecordId = item.VeId.Value.ToString()
            };

            await PerformTest(currentUser, besteller, ordering, item, ear, p => p.Bestellen());
            item.Status.Should().Be(OrderStatesInternal.FuerDigitalisierungBereit);
            item.ApproveStatus.Should().Be(ApproveStatus.FreigegebenDurchSystem);
            orderDataAccessMock.Verify(
                i => i.AddStatusHistoryRecord(It.IsAny<int>(), It.IsAny<OrderStatesInternal>(), It.IsAny<OrderStatesInternal>(), It.IsAny<string>()),
                Times.Exactly(2));
            orderDataAccessMock.Verify(
                i => i.AddStatusHistoryRecord(It.IsAny<int>(), OrderStatesInternal.ImBestellkorb, OrderStatesInternal.NeuEingegangen, "Boumaa Bob"),
                Times.Once);
            orderDataAccessMock.Verify(
                i => i.AddStatusHistoryRecord(It.IsAny<int>(), OrderStatesInternal.NeuEingegangen, OrderStatesInternal.FuerDigitalisierungBereit,
                    "System"), Times.Once);
        }


        [Test]
        public async Task
            Ein_Digitalisierungsauftrag_eines_BAR_Benutzers_wird_nicht_automatisch_freigegeben_wenn_die_DownloadTokens_nicht_BAR_enthalten()
        {
            var currentUser = new User {Id = "current"};
            var besteller = new User
                {Id = "besteller", Access = new UserAccess("besteller", AccessRoles.RoleBAR, "ALLOW", new string[0], false, "de")};

            var item = new OrderItem {OrderId = 1, Id = 1001, VeId = 200, Status = OrderStatesInternal.ImBestellkorb};
            var ordering = new Ordering {Id = 1, UserId = "besteller", Type = OrderType.Digitalisierungsauftrag, OrderDate = DateTime.Now};

            var ear = new ElasticArchiveRecord
            {
                PrimaryDataDownloadAccessTokens = new List<string>(),
                ArchiveRecordId = item.VeId.Value.ToString()
            };

            await PerformTest(currentUser, besteller, ordering, item, ear, p => p.Bestellen());
            item.Status.Should().Be(OrderStatesInternal.FreigabePruefen);
            item.ApproveStatus.Should().Be(ApproveStatus.NichtGeprueft);
        }

        [Test]
        public async Task Ein_Digitalisierungsauftrag_eines_Oe2_Benutzers_erhaelt_die_Digitalisierungskategorie_Oeffentlichkeit()
        {
            var currentUser = new User {Id = "current"};
            var besteller = new User
                {Id = "besteller", Access = new UserAccess("besteller", AccessRoles.RoleOe2, "ALLOW", new string[0], false, "de")};

            var item = new OrderItem {OrderId = 1, Id = 1001, VeId = 200, Status = OrderStatesInternal.ImBestellkorb};
            var ordering = new Ordering {Id = 1, UserId = "besteller", Type = OrderType.Digitalisierungsauftrag, OrderDate = DateTime.Now};

            var ear = new ElasticArchiveRecord
            {
                PrimaryDataDownloadAccessTokens = new List<string>(new[] {"BAR"}),
                ArchiveRecordId = item.VeId.Value.ToString()
            };

            await PerformTest(currentUser, besteller, ordering, item, ear, p => p.Bestellen());
            item.DigitalisierungsKategorie.Should().Be(DigitalisierungsKategorie.Oeffentlichkeit);
        }


        [Test]
        public async Task Eine_Lesesaalausleihen_eines_BAR_Benutzers_wird_automatisch_freigegeben_wenn_die_DownloadTokens_BAR_enthalten()
        {
            var currentUser = new User {Id = "current"};
            var besteller = new User
                {Id = "besteller", Access = new UserAccess("besteller", AccessRoles.RoleBAR, "ALLOW", new string[0], false, "de")};

            var item = new OrderItem {OrderId = 1, Id = 1001, VeId = 200, Status = OrderStatesInternal.ImBestellkorb};
            var ordering = new Ordering {Id = 1, UserId = "besteller", Type = OrderType.Lesesaalausleihen, OrderDate = DateTime.Now};

            var ear = new ElasticArchiveRecord
            {
                PrimaryDataDownloadAccessTokens = new List<string>(new[] {"BAR"}),
                ArchiveRecordId = item.VeId.Value.ToString()
            };

            await PerformTest(currentUser, besteller, ordering, item, ear, p => p.Bestellen());
            item.Status.Should().Be(OrderStatesInternal.FuerAushebungBereit);
            item.ApproveStatus.Should().Be(ApproveStatus.FreigegebenDurchSystem);
        }

        [Test]
        public async Task Eine_Lesesaalausleihen_eines_BAR_Benutzers_wird_nicht_automatisch_freigegeben_wenn_die_DownloadTokens_nicht_BAR_enthalten()
        {
            var currentUser = new User {Id = "current"};
            var besteller = new User
                {Id = "besteller", Access = new UserAccess("besteller", AccessRoles.RoleBAR, "ALLOW", new string[0], false, "de")};

            var item = new OrderItem {OrderId = 1, Id = 1001, VeId = 200, Status = OrderStatesInternal.ImBestellkorb};
            var ordering = new Ordering {Id = 1, UserId = "besteller", Type = OrderType.Lesesaalausleihen, OrderDate = DateTime.Now};

            var ear = new ElasticArchiveRecord
            {
                PrimaryDataDownloadAccessTokens = new List<string>(),
                ArchiveRecordId = item.VeId.Value.ToString()
            };

            await PerformTest(currentUser, besteller, ordering, item, ear, p => p.Bestellen());
            item.Status.Should().Be(OrderStatesInternal.FreigabePruefen);
            item.ApproveStatus.Should().Be(ApproveStatus.NichtGeprueft);
        }

        [Test]
        public async Task Statuswechsel_zu_Reponieren_bereit_löst_für_eine_Benutzungskopie_die_Ablage_aus()
        {
            var currentUser = Users.Vecteur;
            var besteller = new User {Id = "besteller"};

            var item = new OrderItem {OrderId = 1, Id = 1001, VeId = 200, Benutzungskopie = true, Status = OrderStatesInternal.DigitalisierungExtern};
            var ordering = new Ordering {Id = 1, UserId = "besteller", Type = OrderType.Digitalisierungsauftrag, OrderDate = DateTime.Now};

            var ear = new ElasticArchiveRecord
            {
                PrimaryDataDownloadAccessTokens = new List<string>(),
                ArchiveRecordId = item.VeId?.ToString()
            };

            var auftragErledigtMock = new Mock<IConsumer<IDigitalisierungsAuftragErledigt>>();
            auftragErledigtMock.Setup(e => e.Consume(It.IsAny<ConsumeContext<IDigitalisierungsAuftragErledigt>>())).Returns(Task.CompletedTask);
            var benutzungskopieErledigtMock = new Mock<IConsumer<IBenutzungskopieAuftragErledigt>>();
            benutzungskopieErledigtMock.Setup(e => e.Consume(It.IsAny<ConsumeContext<IBenutzungskopieAuftragErledigt>>()))
                .Returns(Task.CompletedTask);

            var harness = new InMemoryTestHarness();
            var auftragConsumer = harness.Consumer(() => auftragErledigtMock.Object, BusConstants.DigitalisierungsAuftragErledigtEvent);
            var benutzungConsumer = harness.Consumer(() => benutzungskopieErledigtMock.Object, BusConstants.BenutzungskopieAuftragErledigtEvent);

            await harness.Start();

            await PerformTest(currentUser, besteller, ordering, item, ear, p => p.SetStatusZumReponierenBereit(), harness.Bus);

            item.Status.Should().Be(OrderStatesInternal.ZumReponierenBereit);
            auftragConsumer.Consumed.Select<IDigitalisierungsAuftragErledigt>().Any().Should().BeFalse();
            auftragErledigtMock.Verify(e => e.Consume(It.IsAny<ConsumeContext<IDigitalisierungsAuftragErledigt>>()), Times.Never);
            benutzungConsumer.Consumed.Select<IBenutzungskopieAuftragErledigt>().Any().Should().BeTrue();
            benutzungskopieErledigtMock.Verify(e => e.Consume(It.IsAny<ConsumeContext<IBenutzungskopieAuftragErledigt>>()), Times.Once);
            await harness.Stop();
        }

        [Test]
        public async Task Statuswechsel_zu_Reponieren_bereit_löst_für_eine_Gebrauchskopie_download_aus()
        {
            var currentUser = Users.Vecteur;
            var besteller = new User {Id = "besteller"};

            var item = new OrderItem
                {OrderId = 1, Id = 1001, VeId = 200, Benutzungskopie = false, Status = OrderStatesInternal.DigitalisierungExtern};
            var ordering = new Ordering {Id = 1, UserId = "besteller", Type = OrderType.Digitalisierungsauftrag, OrderDate = DateTime.Now};

            var ear = new ElasticArchiveRecord
            {
                PrimaryDataDownloadAccessTokens = new List<string>(),
                ArchiveRecordId = item.VeId?.ToString()
            };


            var auftragErledigtMock = new Mock<IConsumer<IDigitalisierungsAuftragErledigt>>();
            auftragErledigtMock.Setup(e => e.Consume(It.IsAny<ConsumeContext<IDigitalisierungsAuftragErledigt>>())).Returns(Task.CompletedTask);
            var benutzungskopieErledigtMock = new Mock<IConsumer<IBenutzungskopieAuftragErledigt>>();
            benutzungskopieErledigtMock.Setup(e => e.Consume(It.IsAny<ConsumeContext<IBenutzungskopieAuftragErledigt>>()))
                .Returns(Task.CompletedTask);

            var harness = new InMemoryTestHarness();
            var auftragConsumer = harness.Consumer(() => auftragErledigtMock.Object, BusConstants.DigitalisierungsAuftragErledigtEvent);
            var benutzungConsumer = harness.Consumer(() => benutzungskopieErledigtMock.Object, BusConstants.BenutzungskopieAuftragErledigtEvent);

            await harness.Start();

            await PerformTest(currentUser, besteller, ordering, item, ear, p => p.SetStatusZumReponierenBereit(), harness.Bus);

            item.Status.Should().Be(OrderStatesInternal.ZumReponierenBereit);
            auftragConsumer.Consumed.Select<IDigitalisierungsAuftragErledigt>().Any().Should().BeTrue();
            auftragErledigtMock.Verify(e => e.Consume(It.IsAny<ConsumeContext<IDigitalisierungsAuftragErledigt>>()), Times.Once);
            benutzungConsumer.Consumed.Select<IBenutzungskopieAuftragErledigt>().Any().Should().BeFalse();
            benutzungskopieErledigtMock.Verify(e => e.Consume(It.IsAny<ConsumeContext<IBenutzungskopieAuftragErledigt>>()), Times.Never);
            await harness.Stop();
        }
    }
}