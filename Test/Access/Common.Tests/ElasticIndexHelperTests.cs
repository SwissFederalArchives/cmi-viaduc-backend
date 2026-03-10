using CMI.Contract.Common;
using FluentAssertions;
using Moq;
using Nest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CMI.Access.Common.Tests
{
    [TestFixture]
    public class ElasticIndexHelperTests
    {
        private Mock<IElasticClient> elasticClientMock;
       
        private ElasticIndexHelper helper;

        [SetUp]
        public void SetUp()
        {
           elasticClientMock = new Mock<IElasticClient>();
        }

        [Test]
        public void Should_Read_ElasticRecordDB_with_ScopeId_get_VE_with_NewAIS_And_Find_ScopeId_As_ExternalKey()
        {
            // Arrange
            var mockSearchResponseWithRecord = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponseWithRecord.Setup(x => x.Documents).Returns(new[]
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e",
                    Title = "TestTitle",
                    All = "TestAll",
                    ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "5690308" }]
                }
            }.ToList());
          
            elasticClientMock.Setup(x => x.Search<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest<ElasticArchiveDbRecord>>())).Returns(mockSearchResponseWithRecord.Object);

            helper = new ElasticIndexHelper(elasticClientMock.Object);

            // Act
            var record = helper.GetDbRecord("5690308", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.Should().NotBeNull();
            record.ArchiveRecordId.Should().NotBe("5690308");
            record.ArchiveRecordId.Should().Be("Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e");

            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>()), Times.Once);
        }

        [Test]
        public void Should_Read_ElasticRecordDB_with_ScopeId_get_VE_with_NewAIS_And_Find_ScopeId_As_DocId()
        {
            // Arrange
            var mockSearchResponse = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<ElasticArchiveDbRecord>());
           

            var mockSearchResponseWithRecord = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponseWithRecord.Setup(x => x.Documents).Returns(new[]
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e",
                    Title = "TestTitle",
                    All = "TestAll"
                }
            }.ToList());

            elasticClientMock.Setup(x => x.Search
                (It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>())).Returns(mockSearchResponseWithRecord.Object);
            elasticClientMock.Setup(x => x.Search<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest<ElasticArchiveDbRecord>>())).Returns(mockSearchResponse.Object);

            helper = new ElasticIndexHelper(elasticClientMock.Object);


            // Act
            var record = helper.GetDbRecord("5690308", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.Should().NotBeNull();
            record.ArchiveRecordId.Should().NotBe("5690308");
            record.ArchiveRecordId.Should().Be("Klas    c592a85a-2bf4-5931-9990-bd17eb36ac3e");

            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>()), Times.Exactly(1));
            elasticClientMock.Verify(e => e.Search<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>()), Times.Once);
        }

        [Test]
        public void Should_Read_ElasticRecordDB_with_Null_Id_get_Null()
        {
            // Arrange
            var mockSearchResponse = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<ElasticArchiveDbRecord>());
            elasticClientMock.Setup(x => x.Search
                (It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>())).Returns(mockSearchResponse.Object);
          
            elasticClientMock.Setup(x => x.Search<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest<ElasticArchiveDbRecord>>())).Returns(mockSearchResponse.Object);


            helper = new ElasticIndexHelper(elasticClientMock.Object);

            // Act
            var record = helper.GetDbRecord("", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.Should().BeNull();
            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>()), Times.Never);
        }

        [Test]
        public void Should_Read_ElasticRecord_with_Null_Id_get_Null()
        {
            // Arrange
            var mockSearchResponse = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<ElasticArchiveDbRecord>());
            elasticClientMock.Setup(x => x.Search
                (It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>())).Returns(mockSearchResponse.Object);
            var mockSearchResponseWithRecord = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            
            elasticClientMock.Setup(x => x.Search<ElasticArchiveRecord>
                (It.IsAny<SearchRequest<ElasticArchiveRecord>>())).Returns(mockSearchResponseWithRecord.Object);

            helper = new ElasticIndexHelper(elasticClientMock.Object);

            // Act
            var record = helper.GetRecord("", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.Should().BeNull();
            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>()), Times.Never);
        }

        [Test]
        public void Should_Read_ElasticRecordDB_with_NewAIS_get_VE_with_NewAIS()
        {
            // Arrange
            var mockSearchResponse = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<ElasticArchiveDbRecord>());
            var mockSearchResponseWithRecord = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();

            mockSearchResponseWithRecord.Setup(x => x.Documents).Returns(new[]
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "Best    9c427a63-b945-524c-820a-c411451025d9",
                    Title = "Niclas Beste",
                    All = "TestAll",
                    ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "5690308" }]
                }
            }.ToList());

            elasticClientMock.Setup(x => x.Search
                (It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>())).Returns(mockSearchResponseWithRecord.Object);

            elasticClientMock.Setup(x => x.Search<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest<ElasticArchiveDbRecord>>())).Returns(mockSearchResponseWithRecord.Object);

            helper = new ElasticIndexHelper(elasticClientMock.Object);

            // Act
            var record = helper.GetDbRecord("Best    9c427a63-b945-524c-820a-c411451025d9", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.Should().NotBeNull();
            record.ArchiveRecordId.Should().NotBe("5690308");
            record.ArchiveRecordId.Should().Be("Best    9c427a63-b945-524c-820a-c411451025d9");
            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>()), Times.Once);
            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>()), Times.Never);
        }


        [Test]
        public void Should_Read_ElasticRecordDB_with_Encode_NewAIS_get_VE_with_NewAIS()
        {
            // Arrange
            var mockSearchResponse = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<ElasticArchiveDbRecord>());
            var mockSearchResponseWithRecord = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();

            mockSearchResponseWithRecord.Setup(x => x.Documents).Returns(new[]
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "Best    9c427a63-b945-524c-820a-c411451025d9",
                    Title = "Niclas Beste",
                    All = "TestAll",
                    ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "5690308" }]
                }
            }.ToList());

            elasticClientMock.Setup(x => x.Search
                (It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>())).Returns(mockSearchResponseWithRecord.Object);

            elasticClientMock.Setup(x => x.Search<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest<ElasticArchiveDbRecord>>())).Returns(mockSearchResponseWithRecord.Object);

            helper = new ElasticIndexHelper(elasticClientMock.Object);

            // Act
            var record = helper.GetDbRecord("Best%20%20%20%209c427a63-b945-524c-820a-c411451025d9", MetadataToExclude.OCRContentAndFiles);

            //Assert
            record.Should().NotBeNull();
            record.ArchiveRecordId.Should().NotBe("5690308");
            record.ArchiveRecordId.Should().Be("Best    9c427a63-b945-524c-820a-c411451025d9");
            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>()), Times.Never);
        }

        [Test]
        public void Should_Read_ElasticRecord_with_ScopeId_get_VE_with_NewAIS()
        {
            // Arrange
            var mockSearchResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<ElasticArchiveRecord>());
            var mockSearchResponseWithRecord = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockSearchResponseWithRecord.Setup(x => x.Documents).Returns(new[]
            {
                new ElasticArchiveRecord
                {
                    ArchiveRecordId = "TBest   32da7073-4641-514c-a7eb-0c552e8675c7",
                    Title = "Heiz Kitle",
                    All = "TestAll",
                    ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "4641"}]
                }
            }.ToList());
            elasticClientMock.Setup(x => x.Search
                (It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>())).Returns(mockSearchResponse.Object);

            elasticClientMock.Setup(x => x.Search<ElasticArchiveRecord>
                (It.IsAny<SearchRequest<ElasticArchiveRecord>>())).Returns(mockSearchResponseWithRecord.Object);


            helper = new ElasticIndexHelper(elasticClientMock.Object);

            // Act
            var record = helper.GetRecord("4641", MetadataToExclude.OCRContentAndFiles);

            record.Should().NotBeNull();
            record.ArchiveRecordId.Should().NotBe("4641");
            record.ArchiveRecordId.Should().Be("TBest   32da7073-4641-514c-a7eb-0c552e8675c7");
            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>()), Times.Once);
        }

        [Test]
        public void Should_Read_ElasticRecord_with_NewAIS_get_VE_with_NewAIS()
        {
            // Arrange
            var mockSearchResponse = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<ElasticArchiveRecord>());
            var mockSearchResponseWithRecord = new Mock<ISearchResponse<ElasticArchiveRecord>>();
            mockSearchResponseWithRecord.Setup(x => x.Documents).Returns(new[]
            {
                new ElasticArchiveRecord
                {
                    ArchiveRecordId = "TBest   32da7073-4641-514c-a7eb-0c552e8675c7",
                    Title = "Heiz Kitle",
                    All = "TestAll",
                    ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "4641"}]
                }
            }.ToList());
            elasticClientMock.Setup(x => x.Search
                (It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>())).Returns(mockSearchResponseWithRecord.Object);

            elasticClientMock.Setup(x => x.Search<ElasticArchiveRecord>
                (It.IsAny<SearchRequest<ElasticArchiveRecord>>())).Returns(mockSearchResponseWithRecord.Object);

         
            helper = new ElasticIndexHelper(elasticClientMock.Object);

            // Act
            var record = helper.GetRecord("TBest   32da7073-4641-514c-a7eb-0c552e8675c7", MetadataToExclude.OCRContentAndFiles);

            record.Should().NotBeNull();
            record.ArchiveRecordId.Should().NotBe("4641");

            record.ArchiveRecordId.Should().Be("TBest   32da7073-4641-514c-a7eb-0c552e8675c7");

            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>()), Times.Once);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>()), Times.Never);
        }


        [Test]
        public void Should_Read_ElasticRecord_with_Signature()
        {
            // Arrange
            var mockSearchResponse = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<ElasticArchiveDbRecord>());
            var mockSearchResponseWithRecord = new Mock<ISearchResponse<ElasticArchiveDbRecord>>();
            mockSearchResponseWithRecord.Setup(x => x.Documents).Returns(new[]
            {
                new ElasticArchiveDbRecord
                {
                    ArchiveRecordId = "TBest   32da7073-4641-514c-a7eb-0c552e8675c7",
                    Title = "Heiz Lassak",
                    All = "Test All",
                    ReferenceCode = "E4320B#1987/187#310*",
                    ExternalKeys = [new ExternalKey {Key = "scopeArchiv", Value = "4641"}]
                }
            }.ToList());
            elasticClientMock.Setup(x => x.Search
                (It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>())).Returns(mockSearchResponseWithRecord.Object);

            elasticClientMock.Setup(x => x.Search<ElasticArchiveDbRecord>
                (It.IsAny<SearchRequest<ElasticArchiveDbRecord>>())).Returns(mockSearchResponseWithRecord.Object);


            helper = new ElasticIndexHelper(elasticClientMock.Object);

            // Act
            var record = helper.GetDbRecord("E4320B#1987/187#310*", MetadataToExclude.OCRContentAndFiles);

            record.Should().NotBeNull();
            record.ArchiveRecordId.Should().NotBe("4641");

            record.ArchiveRecordId.Should().Be("TBest   32da7073-4641-514c-a7eb-0c552e8675c7");

            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveDbRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search(It.IsAny<Func<SearchDescriptor<ElasticArchiveRecord>, ISearchRequest>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveRecord>(It.IsAny<SearchRequest<ElasticArchiveRecord>>()), Times.Never);
            elasticClientMock.Verify(e => e.Search<ElasticArchiveDbRecord>(It.IsAny<SearchRequest<ElasticArchiveDbRecord>>()), Times.Once);
        }
    }
}