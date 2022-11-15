using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Order.Consumers;
using FluentAssertions;
using MassTransit;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Order.Tests
{
    /// <summary>
    /// Tests ob Tokens  aus persoenlichen und allgemeinen  Tokens richtig neu zusammengesetzt werden
    /// und dann auch korrekt weitergeleitet
    /// Mit verschiedenen Tokens
    /// </summary>
    [TestFixture]
    public class UpdateIndivTokensHelperTests
    {
        private UpdateIndivTokens testResultTokens;
        private Mock<IOrderDataAccess> dataAccess;
        private Mock<ISendEndpoint> sendEndpoint;
        private Mock<ISendEndpointProvider> sendEndpointProvider;

        [Test]
        public async Task If_new_BAR_Tokens_set_and_forwarded_correctly_without_individual_tokens()
        {
            // arrange
            var existingIndivTokens = new IndivTokens(
                new string[] { },
                new string[] { },
                new string[] { }
            );
            CreatingMocksWithCallbackData(existingIndivTokens);

            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 12345,
                ExistingFieldAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingMetadataAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleBAR }
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 12345,
                CombinedFieldAccessTokens = new[] { AccessRoles.RoleBAR },
                CombinedMetadataAccessTokens = new[] { AccessRoles.RoleBAR },
                CombinedPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleBAR },
                CombinedPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleBAR }
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }


        [Test]
        public async Task If_existing_OE3_Tokens_set_and_forwarded_correctly_wit_individual_tokens()
        {
            // arrange
            var existingIndivTokens = new IndivTokens(
                new string[] { },
                new string[] { },
                new[] { "DDS" }
            );
            CreatingMocksWithCallbackData(existingIndivTokens);

            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 12345,
                ExistingFieldAccessTokens = new string[] { AccessRoles.RoleBAR },
                ExistingMetadataAccessTokens = new[] { AccessRoles.RoleOe3 },
                ExistingPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleOe3 },
                ExistingPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleOe3 }
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 12345,
                CombinedFieldAccessTokens = new[] { AccessRoles.RoleBAR, "DDS" },
                CombinedMetadataAccessTokens = new[] { AccessRoles.RoleOe3 },
                CombinedPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleOe3 },
                CombinedPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleOe3 }
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }


        [Test]
        public async Task If_existing_OE3_Tokens_set_and_forwarded_correctly_wit_individual_tokens_not_Anonymized_Record()
        {
            // arrange
            var existingIndivTokens = new IndivTokens(
                new string[] { },
                new string[] { },
                new[] { "DDS" }
            );
            CreatingMocksWithCallbackData(existingIndivTokens);

            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 12345,
                ExistingFieldAccessTokens = new string[] { },
                ExistingMetadataAccessTokens = new[] { AccessRoles.RoleOe3 },
                ExistingPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleOe3 },
                ExistingPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleOe3 }
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 12345,
                CombinedFieldAccessTokens = new string[] { },
                CombinedMetadataAccessTokens = new[] { AccessRoles.RoleOe3 },
                CombinedPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleOe3 },
                CombinedPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleOe3 }
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }


        [Test]
        public async Task If_existing_BAR_Tokens_and_set_IndivToken_are_added_not_Anonymized_Record()
        {
            // arrange
            var existingIndivTokens = new IndivTokens(
                new[] { "EB_12345" },
                new[] { "FG_12345" },
                new[] { "EB_12345" }
            );
            CreatingMocksWithCallbackData(existingIndivTokens);

            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 12345,
                ExistingFieldAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingMetadataAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleBAR }
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 12345,
                CombinedFieldAccessTokens = new[] { AccessRoles.RoleBAR, "EB_12345" },
                CombinedMetadataAccessTokens = new[] { AccessRoles.RoleBAR},
                CombinedPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleBAR, "FG_12345" },
                CombinedPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleBAR, "EB_12345" }
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }


        [Test]
        public async Task If_existing_BAR_Tokens_and_set_IndivToken_are_added()
        {
            // arrange
            var existingIndivTokens = new IndivTokens(
                new[] { "EB_12345" },
                new[] { "FG_12345" },
                new[] { "EB_12345" }
            );
            CreatingMocksWithCallbackData(existingIndivTokens);

            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 12345,
                ExistingFieldAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingMetadataAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleBAR }
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 12345,
                CombinedFieldAccessTokens = new[] { AccessRoles.RoleBAR, "EB_12345" },
                CombinedMetadataAccessTokens = new[] { AccessRoles.RoleBAR },
                CombinedPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleBAR, "FG_12345" },
                CombinedPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleBAR, "EB_12345" }
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }

        [Test]
        public async Task If_new_BAR_Tokens_set_and_forwarded_correctly_with_existing_individual_tokens()
        {
            // arrange
            var existingIndivTokens = new IndivTokens(
                new[] { "EB_12345", AccessRoles.RoleBAR },
                new[] { "EB_12345", AccessRoles.RoleBAR },
                new[] { AccessRoles.RoleBAR }
            );
            CreatingMocksWithCallbackData(existingIndivTokens);


            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 12345,
                ExistingFieldAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingMetadataAccessTokens = new[] { AccessRoles.RoleBAR },
                ExistingPrimaryDataDownloadAccessTokens = new[] { AccessRoles.RoleBAR, AccessRoles.RoleAS },
                ExistingPrimaryDataFulltextAccessTokens = new[] { AccessRoles.RoleBAR, AccessRoles.RoleAS }
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 12345,
                CombinedFieldAccessTokens = new[] { AccessRoles.RoleBAR },
                CombinedMetadataAccessTokens = new[] { AccessRoles.RoleBAR },
                CombinedPrimaryDataDownloadAccessTokens = new[] { "EB_12345", AccessRoles.RoleBAR, AccessRoles.RoleAS },
                CombinedPrimaryDataFulltextAccessTokens = new[] { "EB_12345", AccessRoles.RoleBAR, AccessRoles.RoleAS }
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }


        [Test]
        public async Task If_set_All_Tokens_check_is_UpdateIndivTokens_created_and_forwarded_correctly_with_existing_individual_tokens()
        {
            // arrange
            var indivRoles = new[]
            {
                "EB_45123"
            };
            var roles = new[]
            {
               AccessRoles.RoleAS, AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo, AccessRoles.RoleOe1,
                AccessRoles.RoleOe2, AccessRoles.RoleOe3, AccessRoles.RoleBAR
            };

            var existingIndivTokens = new IndivTokens(
                indivRoles, indivRoles, indivRoles);
            CreatingMocksWithCallbackData(existingIndivTokens);


            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 12345,
                ExistingFieldAccessTokens = roles,
                ExistingMetadataAccessTokens = roles,
                ExistingPrimaryDataDownloadAccessTokens = roles,
                ExistingPrimaryDataFulltextAccessTokens = roles
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 12345,
                CombinedFieldAccessTokens = indivRoles.Union(roles).ToArray(),
                // es benötigt keine individuellen MetadatenAccessTokens
                CombinedMetadataAccessTokens = tokensFromDb.ExistingMetadataAccessTokens,
                // Besitzt die VE ein Ö2 Token , so benötigt es keine Individuellen Token
                CombinedPrimaryDataDownloadAccessTokens = tokensFromDb.ExistingPrimaryDataDownloadAccessTokens,
                CombinedPrimaryDataFulltextAccessTokens = tokensFromDb.ExistingPrimaryDataFulltextAccessTokens
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }

        [Test]
        public async Task If_set_only_PrimaryDataFulltextAccessTokens_check_is_UpdateIndivTokens_created_and_forwarded_correctly_with_existing_individual_tokens()
        {
            // arrange
            var indivRoles = new[]
            {
                "EB_45123"
            };
            var roles = new[]
            {
                AccessRoles.RoleAS, AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo, AccessRoles.RoleOe1,
                AccessRoles.RoleOe2, AccessRoles.RoleOe3, AccessRoles.RoleBAR
            };

            var existingIndivTokens = new IndivTokens(
                indivRoles, new string[] { }, new string[] { });
            CreatingMocksWithCallbackData(existingIndivTokens);


            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 45123,
                ExistingFieldAccessTokens = new string[] { },
                ExistingMetadataAccessTokens = new string[] { },
                ExistingPrimaryDataDownloadAccessTokens = new string[] { },
                ExistingPrimaryDataFulltextAccessTokens = roles
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 45123,
                CombinedFieldAccessTokens = new string[] { },
                CombinedMetadataAccessTokens = new string[] { },
                CombinedPrimaryDataDownloadAccessTokens = new string[] { },
                CombinedPrimaryDataFulltextAccessTokens = roles
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }


        [Test]
        public async Task If_set_only_PrimaryDataFulltextAccessTokens_and_FieldAccessTokens_are_null_check_is_UpdateIndivTokens_created_and_forwarded_correctly_with_existing_individual_tokens()
        {
            // arrange
            var indivRoles = new[]
            {
                "EB_45123"
            };
            var roles = new[]
            {
               AccessRoles.RoleAS, AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo, AccessRoles.RoleOe1,
                AccessRoles.RoleOe2, AccessRoles.RoleOe3, AccessRoles.RoleBAR
            };
            var existingIndivTokens = new IndivTokens(
                indivRoles, new string[] { },new string[] { });
            CreatingMocksWithCallbackData(existingIndivTokens);

            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 45123,
                ExistingFieldAccessTokens = new string[] { },
                ExistingMetadataAccessTokens = new string[] { },
                ExistingPrimaryDataDownloadAccessTokens = new string[] { },
                ExistingPrimaryDataFulltextAccessTokens = roles
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 45123,
                CombinedFieldAccessTokens = new string[] { },
                CombinedMetadataAccessTokens = new string[] { },
                CombinedPrimaryDataDownloadAccessTokens = new string[] { },
                // not Combined
                CombinedPrimaryDataFulltextAccessTokens = roles
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }


        [Test]
        public async Task If_RecalcIndivTokens_and_IndivTokens_same_check_is_UpdateIndivTokens_created_and_forwarded_correctly_with_existing_individual_tokens()
        {
            // arrange
            var roles = new[]
            {
               AccessRoles.RoleAS, "FG_12345", AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo, AccessRoles.RoleOe1,
                AccessRoles.RoleOe2, AccessRoles.RoleOe3, AccessRoles.RoleBAR
            };

            var rollsWithoutIndiv = new[]
            {
                AccessRoles.RoleAS, AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo, AccessRoles.RoleOe1,
                AccessRoles.RoleOe2, AccessRoles.RoleOe3, AccessRoles.RoleBAR
            };

            var existingIndivTokens = new IndivTokens(
                roles, roles, roles);
            CreatingMocksWithCallbackData(existingIndivTokens);

            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 45123,
                ExistingFieldAccessTokens = roles,
                ExistingMetadataAccessTokens = roles,
                ExistingPrimaryDataDownloadAccessTokens = roles,
                ExistingPrimaryDataFulltextAccessTokens = roles
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 45123,
                CombinedFieldAccessTokens = roles,
                CombinedMetadataAccessTokens = roles,
                CombinedPrimaryDataDownloadAccessTokens = rollsWithoutIndiv,
                CombinedPrimaryDataFulltextAccessTokens = rollsWithoutIndiv
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            // Oe2 User benötigen keine indiv Token
            expected.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            expected.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }

        [Test]
        public async Task Is_one_of_the_RecalcIndivTokens_is_individual_token_then_whole_list_is_not_added()
        {
            // arrange
            var indivRoles = new[]
            {
                  AccessRoles.RoleAS, AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo,  AccessRoles.RoleBAR
            };

            var roles = new[]
            {
                AccessRoles.RoleOe1, "EB_45123",
                AccessRoles.RoleOe2, AccessRoles.RoleOe3
            };
            var existingIndivTokens = new IndivTokens(
                indivRoles, indivRoles, indivRoles);
            CreatingMocksWithCallbackData(existingIndivTokens);

            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 45123,
                ExistingFieldAccessTokens = roles,
                ExistingMetadataAccessTokens = roles,
                ExistingPrimaryDataDownloadAccessTokens = roles,
                ExistingPrimaryDataFulltextAccessTokens = roles
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert

            // without  "EB_45123",
            var resultRolls = new[]
            {
                AccessRoles.RoleAS, AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo,  AccessRoles.RoleBAR,
                AccessRoles.RoleOe1,
                AccessRoles.RoleOe2, AccessRoles.RoleOe3
            };
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 45123,
                CombinedFieldAccessTokens = resultRolls,
                // es benötigt keine individuellen MetadatenAccessTokens
                CombinedMetadataAccessTokens = tokensFromDb.ExistingMetadataAccessTokens,
                CombinedPrimaryDataDownloadAccessTokens = resultRolls,
                CombinedPrimaryDataFulltextAccessTokens = resultRolls
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            expected.CombinedFieldAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedFieldAccessTokens);
            expected.CombinedMetadataAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedMetadataAccessTokens);
            // Besitzt die VE ein Ö2 Token , so benötigt es keine Individuellen Token
            testResultTokens.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(AccessRoles.RoleOe1, AccessRoles.RoleOe2, AccessRoles.RoleOe3);
            testResultTokens.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(AccessRoles.RoleOe1, AccessRoles.RoleOe2, AccessRoles.RoleOe3);
        }

        [Test]
        public async Task Is_one_of_the_IndivTokens_is_individual_token_then_all_tokens_added()
        {
            // arrange
            var roles = new[]
            {
               AccessRoles.RoleAS, AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo,  AccessRoles.RoleBAR, AccessRoles.RoleOe3
            };

            var indivRoles = new[]
            {
               "EB_45123"
            };
            var existingIndivTokens = new IndivTokens(
                indivRoles, indivRoles, indivRoles);
            CreatingMocksWithCallbackData(existingIndivTokens);


            var tokensFromDb = new RecalcIndivTokens
            {
                ArchiveRecordId = 45123,
                ExistingFieldAccessTokens = roles,
                ExistingMetadataAccessTokens = roles,
                ExistingPrimaryDataDownloadAccessTokens = roles,
                ExistingPrimaryDataFulltextAccessTokens = roles
            };
            // act
            await UpdateIndivTokensHelper.SendToIndexManager(tokensFromDb, dataAccess.Object, sendEndpointProvider.Object, new Uri("https://cmiag.ch/"));

            //assert
            // with 
            var resultRolls = new[]
            {
                AccessRoles.RoleAS, AccessRoles.RoleBVW, AccessRoles.RoleMgntAllow, AccessRoles.RoleMgntAppo,  AccessRoles.RoleBAR,
                "EB_45123",  AccessRoles.RoleOe3
            };
            var expected = new UpdateIndivTokens
            {
                ArchiveRecordId = 45123,
                CombinedFieldAccessTokens = resultRolls,
                // es benötigt keine individuellen MetadatenAccessTokens
                CombinedMetadataAccessTokens = roles,
                CombinedPrimaryDataDownloadAccessTokens = resultRolls,
                CombinedPrimaryDataFulltextAccessTokens = resultRolls
            };
            sendEndpoint.Verify(ep => ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()), Times.Once());
            expected.ArchiveRecordId.Should().Be(testResultTokens.ArchiveRecordId);
            testResultTokens.CombinedFieldAccessTokens.Should().BeEquivalentTo(expected.CombinedFieldAccessTokens);
            testResultTokens.CombinedMetadataAccessTokens.Should().BeEquivalentTo(expected.CombinedMetadataAccessTokens);
            testResultTokens.CombinedPrimaryDataDownloadAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataDownloadAccessTokens);
            testResultTokens.CombinedPrimaryDataFulltextAccessTokens.Should().BeEquivalentTo(testResultTokens.CombinedPrimaryDataFulltextAccessTokens);
        }


        /// <summary>
        /// Creating Mocks with data response
        /// </summary>
        /// <param name="indivTokens">the tokens how dataAccess answered by call GetIndividualAccessTokens</param>
        private void CreatingMocksWithCallbackData(IndivTokens indivTokens)
        {
            dataAccess = new Mock<IOrderDataAccess>();
            sendEndpoint = new Mock<ISendEndpoint>();

            sendEndpoint.Setup(ep =>
                    ep.Send(It.IsAny<UpdateIndivTokens>(), It.IsAny<CancellationToken>()))
                .Callback<UpdateIndivTokens, CancellationToken>(UpdateIndivTokensConsumerTest);
            dataAccess.Setup(m => m.GetIndividualAccessTokens(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.FromResult(indivTokens));
            sendEndpointProvider = new Mock<ISendEndpointProvider>();
            sendEndpointProvider.Setup(m => m.GetSendEndpoint(It.IsAny<Uri>())).Returns(Task.FromResult(sendEndpoint.Object));
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
}
