using CMI.Access.Common;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Manager.Order.Consumers;
using CMI.Manager.Order.Status;
using FluentAssertions;
using MassTransit;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CMI.Manager.Order.Tests
{
    [TestFixture]
    public class RegisterActionForIndivTokensRefreshTests
    {
        private UpdateIndivTokens testResultTokens;
        private Mock<IBus> busMock;
        private StatuswechselContext auftragStatusContext;
        private Mock<IOrderDataAccess> orderDataAccess;
        private Mock<ISearchIndexDataAccess> searchIndexAccess;
        private Mock<ISendEndpoint> sendEndpoint;
        private AuftragStatus auftragStatus;
        private PostCommitActionsRegistry postCommitActionsRegistry;
        private ElasticArchiveRecord record;
        private OrderItem orderItem;

        [Test]
        public async Task FreigabePruefenStatus_StatusWechsel_change_the_individual_tokens_even_if_the_order_was_with_ScopeId()
        {
            // arrange
            var existingIndivTokens = new IndivTokens(
                ["EB_123456789"],
                ["EB_123456789"],
                ["EB_123456789"]
            );
            var newTokens = new IndivTokens(
                [AccessRoles.RoleBAR],
                [AccessRoles.RoleBAR],
                [AccessRoles.RoleBAR]
            );

            orderItem = new OrderItem
            {
                VeId = "123456789"
            };
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
                PrimaryDataFulltextAccessTokens = [AccessRoles.RoleBAR]
            };
            CreatingMocksWithCallbackData(newTokens, existingIndivTokens);

           
            // act
            UpdateIndivTokensHelper.RegisterActionForIndivTokensRefresh(auftragStatus);
            await((IRunAll) postCommitActionsRegistry).RunAll();

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = "Arch 9090",
                CombinedFieldAccessTokens = ["EB_123456789", AccessRoles.RoleBAR],
                CombinedMetadataAccessTokens = [AccessRoles.RoleBAR],
                CombinedPrimaryDataDownloadAccessTokens = ["EB_123456789", AccessRoles.RoleBAR],
                CombinedPrimaryDataFulltextAccessTokens = ["EB_123456789", AccessRoles.RoleBAR]
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }



        [Test]
        public async Task FreigabePruefenStatus_StatusWechsel_change_the_individual_tokens_even_if_the_order_was_with_ActaProId()
        {
            // arrange
            var existingIndivTokens = new IndivTokens(
                ["EB_PET24"],
                ["EB_PET24"],
                ["EB_PET24"]
            );
            var newTokens = new IndivTokens(
                [AccessRoles.RoleBAR],
                [AccessRoles.RoleBAR],
                [AccessRoles.RoleBAR, AccessRoles.RoleAS]
            );

            orderItem = new OrderItem
            {
                VeId = "Arch 8080"
            };
            record = new ElasticArchiveRecord()
            {
                ExternalKeys =
                [
                    new()
                    {
                        Value = "432156789",
                        Key = "scopeArchiv"
                    }
                ],
                ArchiveRecordId = "Arch 8080",
                FieldAccessTokens = ["EB_PET24", AccessRoles.RoleBAR, AccessRoles.RoleAS],
                MetadataAccessTokens = [AccessRoles.RoleBAR, AccessRoles.RoleBVW, AccessRoles.RoleAS],
                PrimaryDataDownloadAccessTokens = ["EB_PET24", AccessRoles.RoleBAR],
                PrimaryDataFulltextAccessTokens = ["EB_PET24", AccessRoles.RoleBAR]
            };
            CreatingMocksWithCallbackData(newTokens, existingIndivTokens);


            // act
            UpdateIndivTokensHelper.RegisterActionForIndivTokensRefresh(auftragStatus);
            await ((IRunAll) postCommitActionsRegistry).RunAll();

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = "Arch 8080",
                CombinedFieldAccessTokens = existingIndivTokens.FieldDataAccessTokens.Union(newTokens.FieldDataAccessTokens).ToArray(),
                CombinedMetadataAccessTokens = record.MetadataAccessTokens.ToArray(),
                CombinedPrimaryDataDownloadAccessTokens = existingIndivTokens.PrimaryDataDownloadAccessTokens.Union(newTokens.PrimaryDataDownloadAccessTokens).ToArray(),
                CombinedPrimaryDataFulltextAccessTokens = newTokens.PrimaryDataFulltextAccessTokens.Union(existingIndivTokens.PrimaryDataFulltextAccessTokens).ToArray()
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }

        /// <summary>
        /// Creating Mocks with data response
        /// </summary>
        /// <param name="indivTokens">the tokens how dataAccess answered by call GetIndividualAccessTokens</param>
        private void CreatingMocksWithCallbackData(IndivTokens indivTokensArchiv, IndivTokens indivTokensScope)
        {
            orderDataAccess = new Mock<IOrderDataAccess>();
            busMock = new Mock<IBus>();

          
            orderDataAccess.Setup(m => m.GetIndividualAccessTokens(record.ArchiveRecordId, It.IsAny<int>())).Returns(Task.FromResult(indivTokensArchiv));
            orderDataAccess.Setup(m => m.GetIndividualAccessTokens(orderItem.VeId, It.IsAny<int>())).Returns(Task.FromResult(indivTokensScope));

            sendEndpoint = new Mock<ISendEndpoint>();
            sendEndpoint.Setup(ep =>
                    ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()))
                .Callback<UpdateIndivTokens, CancellationToken>(UpdateIndivTokensConsumerTest);
            busMock.SetupGet(s => s.Address).Returns(new Uri("https://cmiag.ch/"));
            busMock.Setup(m => m.GetSendEndpoint(It.IsAny<Uri>())).Returns(Task.FromResult(sendEndpoint.Object));

            postCommitActionsRegistry = new PostCommitActionsRegistry();

            searchIndexAccess = new Mock<ISearchIndexDataAccess>();
            searchIndexAccess.Setup(x => x.FindDocument(orderItem.VeId, MetadataToExclude.OCRContentAndFiles)).Returns(record);

            auftragStatusContext = new StatuswechselContext(orderItem, null, null, new User(), new User(), new List<StatusHistory>(),
                DateTime.Now, searchIndexAccess.Object, busMock.Object, orderDataAccess.Object, postCommitActionsRegistry);
            auftragStatus = new FreigabePruefenStatusTest(auftragStatusContext);

        }


        /// <summary>
        ///  Keeps the forwarded UpdateIndivTokens
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void UpdateIndivTokensConsumerTest(UpdateIndivTokens arg1, CancellationToken arg2)
        {
            testResultTokens = arg1;
        }

    }
    public class FreigabePruefenStatusTest : FreigabePruefenStatus
    {
        public override StatuswechselContext Context { get; protected set; }

        public FreigabePruefenStatusTest(StatuswechselContext Context)
        {
            this.Context = Context;
        }
    }
}

