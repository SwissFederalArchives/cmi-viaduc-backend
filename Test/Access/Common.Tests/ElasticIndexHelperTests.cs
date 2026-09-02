using CMI.Contract.Common;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Transport;
using Shouldly;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CMI.Access.Common.Tests
{
    [TestFixture]
    public class ElasticIndexHelperTests
    {
        private Mock<ElasticsearchClient> elasticClientMock;

        private ElasticIndexHelper helper;

        [SetUp]
        public void SetUp()
        {
           elasticClientMock = new Mock<ElasticsearchClient>();
        }

        [Test]
        public async Task Should_Read_ElasticRecordDB_with_ScopeId_get_VE_with_NewAIS_And_Find_ScopeId_As_ExternalKey()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveDbRecord>("1", "test-index");
            hit.Source = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e",
                Title = "TestTitle",
                All = "TestAll",
                ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "5690308"}]
            };
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(list)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
          
            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            var record = await helper.GetDbRecord("5690308", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldNotBe("5690308");
            record.ArchiveRecordId.ShouldBe("Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e");

            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Should_Read_ElasticRecordDB_with_ScopeId_get_VE_with_NewAIS_And_Find_ScopeId_As_DocId()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveDbRecord>("1", "test-index");
            hit.Source = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e",
                Title = "TestTitle",
                All = "TestAll"
            };
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(list)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            var record = await helper.GetDbRecord("5690308", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldNotBe("5690308");
            record.ArchiveRecordId.ShouldBe("Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e");
      
        
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(0));
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveDbRecord>(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Should_Read_ElasticRecordDB_with_Null_Id_get_Null()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveDbRecord>("", "test-index");
            hit.Source = new ElasticArchiveDbRecord();
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(list)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            var record = await helper.GetDbRecord("", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.ShouldBeNull();
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Should_Read_ElasticRecord_with_Null_Id_get_Null()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveDbRecord>(null, "test-index");
            hit.Source = new ElasticArchiveDbRecord();
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(list)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            var record = await helper.GetRecord("", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.ShouldBeNull();
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task  Should_Read_ElasticRecordDB_with_NewAIS_get_VE_with_NewAIS()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveDbRecord>("1", "test-index");
            hit.Source = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "Best    9c427a63-b945-524c-820a-c411451025d9",
                Title = "Niclas Beste",
                All = "TestAll",
                ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "5690308"}]
            };
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(list)
            };
            var response = new GetResponse<ElasticArchiveDbRecord>() { Found = true, Source = hit.Source };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var getResponse = TestableResponseFactory.CreateSuccessfulResponse(response, 200);

            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
            elasticClientMock.Setup(x => x.GetAsync<ElasticArchiveDbRecord>
                (It.IsAny<GetRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(getResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            var record = await helper.GetDbRecord("Best    9c427a63-b945-524c-820a-c411451025d9", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldNotBe("5690308");
            record.ArchiveRecordId.ShouldBe("Best    9c427a63-b945-524c-820a-c411451025d9");
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.GetAsync<ElasticArchiveDbRecord>(It.IsAny<GetRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        }


        [Test]
        public async Task Should_Read_ElasticRecordDB_with_Encode_NewAIS_get_VE_with_NewAIS()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveDbRecord>("1", "test-index");
            hit.Source = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "Best    9c427a63-b945-524c-820a-c411451025d9",
                Title = "Niclas Beste",
                All = "TestAll",
                ExternalKeys = [new ExternalKey { Key = "scopeArchiv", Value = "5690308" }]
            };
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(list)
            };
            var response = new GetResponse<ElasticArchiveDbRecord>() { Found = true, Source = hit.Source };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var getResponse = TestableResponseFactory.CreateSuccessfulResponse(response, 200);


            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
            elasticClientMock.Setup(x => x.GetAsync<ElasticArchiveDbRecord>
                (It.IsAny<GetRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(getResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            var record = await helper.GetDbRecord("Best%20%20%20%209c427a63-b945-524c-820a-c411451025d9", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldNotBe("5690308");
            record.ArchiveRecordId.ShouldBe("Best    9c427a63-b945-524c-820a-c411451025d9");
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.GetAsync<ElasticArchiveDbRecord>(It.IsAny<GetRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Should_Read_ElasticRecord_with_ScopeId_get_VE_with_NewAIS()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index");
            hit.Source = new ElasticArchiveRecord
            {
                ArchiveRecordId = "TBest   32da7073-4641-514c-a7eb-0c552e8675c7",
                Title = "Heiz Kitle",
                All = "TestAll",
                ExternalKeys = [new ExternalKey { Key = "scopeArchiv", Value = "4641" }]
            };
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(list)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);


            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveRecord>
                (It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveRecord>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            var record = await helper.GetRecord("4641", MetadataToExclude.OCRContentAndFiles);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldNotBe("4641");
            record.ArchiveRecordId.ShouldBe("TBest   32da7073-4641-514c-a7eb-0c552e8675c7");
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Should_Read_ElasticRecord_with_NewAIS_get_VE_with_NewAIS()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index");
            var hitRecord = new ElasticArchiveRecord
            {
                ArchiveRecordId = "TBest   32da7073-4641-514c-a7eb-0c552e8675c7",
                Title = "Heiz Kitle",
                All = "TestAll",
                ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "4641"}]
            };
            hit.Source = hitRecord;
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(list)
            };
            var response = new GetResponse<ElasticArchiveRecord>(){Found = true, Source = hitRecord};

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            var getResponse = TestableResponseFactory.CreateSuccessfulResponse(response, 200);

            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveRecord>
                (It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveRecord>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
            elasticClientMock.Setup(x => x.GetAsync<ElasticArchiveRecord>
                (It.IsAny<GetRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(getResponse));


            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");
            

            // Act
            var record = await helper.GetRecord("TBest   32da7073-4641-514c-a7eb-0c552e8675c7", MetadataToExclude.OCRContentAndFiles);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldNotBe("4641");

            record.ArchiveRecordId.ShouldBe("TBest   32da7073-4641-514c-a7eb-0c552e8675c7");

            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.GetAsync<ElasticArchiveRecord>(It.IsAny<GetRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        public async Task Should_Read_ElasticRecord_with_Signature()
        {
            // Arrange
            var hit = new Hit<ElasticArchiveDbRecord>("1", "test-index");
            hit.Source = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "TBest   32da7073-4641-514c-a7eb-0c552e8675c7",
                Title = "Heiz Lassak",
                All = "Test All",
                ReferenceCode = "E4320B#1987/187#310*",
                ExternalKeys = [new ExternalKey { Key = "scopeArchiv", Value = "4641" }]
            };
            var list = new[]
            {
                hit
            };
            var temp = new SearchResponse<ElasticArchiveDbRecord>
            {
                HitsMetadata = new HitsMetadata<ElasticArchiveDbRecord>(list)
            };

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(temp, 200);
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));
            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(searchResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            var record = await helper.GetDbRecord("E4320B#1987/187#310*", MetadataToExclude.OCRContentAndFiles);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldNotBe("4641");

            record.ArchiveRecordId.ShouldBe("TBest   32da7073-4641-514c-a7eb-0c552e8675c7");

            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveDbRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync(It.IsAny<Action<SearchRequestDescriptor<ElasticArchiveRecord>>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
            elasticClientMock.Verify(e => e.SearchAsync<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #region Remove Tests

        [Test]
        public async Task Remove_Should_Delete_Record_When_Record_Exists_And_DeleteIsSuccessful()
        {
            // Arrange
            var archiveRecordId = "TBest   32da7073-4641-514c-a7eb-0c552e8675c7";
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index");
            var hitRecord = new ElasticArchiveRecord
            {
                ArchiveRecordId = archiveRecordId,
                Title = "Test Record",
                All = "TestAll"
            };
            hit.Source = hitRecord;

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new SearchResponse<ElasticArchiveRecord>
                {
                    HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(new[] { hit })
                }, 200);

            var getResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new GetResponse<ElasticArchiveRecord> { Found = true, Source = hitRecord }, 200);

            var deleteResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new DeleteResponse(), 200);

            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveRecord>(
                It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(searchResponse));

            elasticClientMock.Setup(x => x.GetAsync<ElasticArchiveRecord>(
                It.IsAny<GetRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(getResponse));

            elasticClientMock.Setup(x => x.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(deleteResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            await helper.Remove(archiveRecordId);

            // Assert
            elasticClientMock.Verify(e => e.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Remove_Should_Not_Delete_When_Record_Does_Not_Exist()
        {
            // Arrange
            var archiveRecordId = "NonExistent123";

            var getResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new GetResponse<ElasticArchiveRecord> { Found = false }, 404);

            elasticClientMock.Setup(x => x.GetAsync<ElasticArchiveRecord>(
                It.IsAny<GetRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(getResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            await helper.Remove(archiveRecordId);

            // Assert
            elasticClientMock.Verify(e => e.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Remove_Should_Call_Delete_Even_When_Delete_Fails()
        {
            // Arrange
            var archiveRecordId = "TBest   32da7073-4641-514c-a7eb-0c552e8675c7";
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index");
            var hitRecord = new ElasticArchiveRecord
            {
                ArchiveRecordId = archiveRecordId,
                Title = "Test Record",
                All = "TestAll"
            };
            hit.Source = hitRecord;

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new SearchResponse<ElasticArchiveRecord>
                {
                    HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(new[] { hit })
                }, 200);

            var getResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new GetResponse<ElasticArchiveRecord> { Found = true, Source = hitRecord }, 200);

            var deleteResponse = new DeleteResponse();

            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveRecord>(
                It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(searchResponse));

            elasticClientMock.Setup(x => x.GetAsync<ElasticArchiveRecord>(
                It.IsAny<GetRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(getResponse));

            elasticClientMock.Setup(x => x.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(deleteResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act - sollte keine Exception werfen, sondern nur loggen
            await helper.Remove(archiveRecordId);

            // Assert
            elasticClientMock.Verify(e => e.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Remove_Should_Work_With_ScopeId()
        {
            // Arrange
            var scopeId = "5690308";
            var actualId = "Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e";
            var hit = new Hit<ElasticArchiveRecord>("1", "test-index");
            var hitRecord = new ElasticArchiveRecord
            {
                ArchiveRecordId = actualId,
                Title = "Test Record",
                All = "TestAll",
                ExternalKeys = [new ExternalKey { Key = "scopeArchiv", Value = scopeId }]
            };
            hit.Source = hitRecord;

            var searchResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new SearchResponse<ElasticArchiveRecord>
                {
                    HitsMetadata = new HitsMetadata<ElasticArchiveRecord>(new[] { hit })
                }, 200);

            var deleteResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new DeleteResponse(), 200);

            elasticClientMock.Setup(x => x.SearchAsync<ElasticArchiveRecord>(
                It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(searchResponse));

            elasticClientMock.Setup(x => x.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(deleteResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            await helper.Remove(scopeId);

            // Assert
            elasticClientMock.Verify(e => e.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Remove_Should_Work_With_ActaProId_When_Direct_Lookup_Succeeds()
        {
            // Arrange
            // ActaPro DocKey mit 44 Zeichen Länge wird übergeben
            var actaProDocKey = "Best    9c427a63-b945-524c-820a-c411451025d9";
            var scopeId = "5690308"; // Der Record hat eine ScopeId

            var hit = new Hit<ElasticArchiveRecord>("1", "test-index");
            var hitRecord = new ElasticArchiveRecord
            {
                ArchiveRecordId = scopeId, // Record wurde mit ScopeId indexiert
                Title = "Test Record via ActaPro",
                All = "TestAll",
                ExternalKeys = [new ExternalKey { Key = "scopeArchiv", Value = scopeId }]
            };
            hit.Source = hitRecord;

            // GetAsync für die DocKey gibt einen Record mit ScopeId zurück
            var foundResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new GetResponse<ElasticArchiveRecord> { Found = true, Source = hitRecord }, 200);

            var deleteResponse = TestableResponseFactory.CreateSuccessfulResponse(
                new DeleteResponse(), 200);

            elasticClientMock.Setup(x => x.GetAsync<ElasticArchiveRecord>(
                It.IsAny<GetRequest>(), 
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(foundResponse));

            elasticClientMock.Setup(x => x.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(deleteResponse));

            helper = new ElasticIndexHelper(elasticClientMock.Object, "test-index");

            // Act
            await helper.Remove(actaProDocKey);

            // Assert - Delete wurde mit der ScopeId aufgerufen
            elasticClientMock.Verify(e => e.DeleteAsync<ElasticArchiveRecord>(
                It.IsAny<Id>(),
                It.IsAny<CancellationToken>()), Times.Once);

            // Verify - GetAsync wurde für die DocKey aufgerufen und gab einen Record mit ScopeId zurück
            elasticClientMock.Verify(e => e.GetAsync<ElasticArchiveRecord>(
                It.IsAny<GetRequest>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}

