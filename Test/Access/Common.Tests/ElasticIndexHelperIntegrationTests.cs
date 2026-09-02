using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using Shouldly;
using NUnit.Framework;

namespace CMI.Access.Common.Tests
{
     [Ignore("Diese Tests müssen bewusst bzw. Bedarf ausgeführt werden")]
    [TestFixture]
    public class ElasticIndexHelperIntegrationTests
    {
        [SetUp]
        public async Task SetUp()
        {
            var uri = "(change here, but do not commit)";
            var node = new Uri(uri);
            string username = "(change here, but do not commit)";
            string pwd = "(change here, but do not commit)";
            helper = new ElasticIndexHelper(node, username, pwd, "test");
            if (await helper.IndexExists("test"))
            {
                await helper.DeleteIndex("test");
            }

            await helper.CreateIndex("test");
        }

        private ElasticIndexHelper helper;

        [Test]
        public async Task ShouldInsertARecord()
        {
            helper.CountDocuments.ShouldBe(0);
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "100" });
            helper.CountDocuments.ShouldBe(1);
        }

        [Test]
        public async Task ShouldInsert1000Records()
        {
            var records = Enumerable.Range(1, 10000).Select(i => TestDataGenerator.Generate(i));
            await helper.IndexBulk(records);
            helper.CountDocuments.ShouldBe(10000);
        }

        [Test]
        public async Task FindByScopeArchiveIdUsingPrimaryKey()
        {
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "100" });

            var record = await helper.GetRecord("100", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("100");

        }

        [Test]
        public async Task FindByScopeArchiveIdUsingExternalKeys()
        {
            await helper.Index(new ElasticArchiveRecord
            {
                ArchiveRecordId = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3", ExternalKeys =
                [
                    new ExternalKey {Key = "scopeArchiv", Value = "100"},
                    new ExternalKey {Key = "ActaPro", Value = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3"}
                ]
            });

            var record = await helper.GetRecord("100", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("Vz      4e3e0b76-8f63-5a85-a018-289db658edd3");
        }


        [Test]
        public async Task FindByDocKeyUsingPrimaryKey()
        {
            await helper.Index(new ElasticArchiveRecord
            {
                ArchiveRecordId = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3", ExternalKeys =
                [
                    new ExternalKey {Key = "scopeArchiv", Value = "100"},
                    new ExternalKey {Key = "ActaPro", Value = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3"}
                ]
            });

            var record = await helper.GetRecord("Vz%20%20%20%20%20%204e3e0b76-8f63-5a85-a018-289db658edd3", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("Vz      4e3e0b76-8f63-5a85-a018-289db658edd3");
        }


        [Test]
        public async Task FindByDocKeyUsingMappingToScopeId()
        {
            // Wir suchen schon mit einem DocKey, aber der Record hat als ArchiveRecordId die ScopeArchivId,
            // damit wir testen, dass die Mapping-Funktion richtig funktioniert
            // Die angegebene ID für scopeArchiv muss so in der mappingTable.db stehen.

            await helper.Index(new ElasticArchiveRecord
            {
                ArchiveRecordId = "21875592", ExternalKeys =
                [
                    new ExternalKey {Key = "scopeArchiv", Value = "21875592"},
                    new ExternalKey {Key = "ActaPro", Value = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3"}
                ]
            });

            var record = await helper.GetRecord("Vz%20%20%20%20%20%204e3e0b76-8f63-5a85-a018-289db658edd3", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("21875592");
        }

        [Test]
        public async Task FindDbRecordByScopeArchiveIdUsingPrimaryKey()
        {
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "100" });

            var record = await helper.GetDbRecord("100", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("100");

        }

        [Test]
        public async Task FindDbRecordByScopeArchiveIdUsingExternalKeys()
        {
            await helper.Index(new ElasticArchiveRecord
            {
                ArchiveRecordId = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3", ExternalKeys =
                [
                    new ExternalKey {Key = "scopeArchiv", Value = "100"},
                    new ExternalKey {Key = "ActaPro", Value = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3"}
                ]
            });

            var record = await helper.GetDbRecord("100", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("Vz      4e3e0b76-8f63-5a85-a018-289db658edd3");
        }


        [Test]
        public async Task FindDbRecordByDocKeyUsingPrimaryKey()
        {
            await helper.Index(new ElasticArchiveRecord
            {
                ArchiveRecordId = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3", ExternalKeys =
                [
                    new ExternalKey {Key = "scopeArchiv", Value = "100"},
                    new ExternalKey {Key = "ActaPro", Value = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3"}
                ]
            });

            var record = await helper.GetDbRecord("Vz%20%20%20%20%20%204e3e0b76-8f63-5a85-a018-289db658edd3", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("Vz      4e3e0b76-8f63-5a85-a018-289db658edd3");
        }


        [Test]
        public async Task FindDbRecordByDocKeyUsingMappingToScopeId()
        {
            // Wir suchen schon mit einem DocKey, aber der Record hat als ArchiveRecordId die ScopeArchivId,
            // damit wir testen, dass die Mapping-Funktion richtig funktioniert
            // Die angegebene ID für scopeArchiv muss so in der mappingTable.db stehen.

            await helper.Index(new ElasticArchiveRecord
            {
                ArchiveRecordId = "21875592", ExternalKeys =
                [
                    new ExternalKey {Key = "scopeArchiv", Value = "21875592"},
                    new ExternalKey {Key = "ActaPro", Value = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3"}
                ]
            });

            var record = await helper.GetDbRecord("Vz%20%20%20%20%20%204e3e0b76-8f63-5a85-a018-289db658edd3", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("21875592");
        }

        [Test]
        public async Task FindDbRecordByDocKeyUsingReferenceCode()
        {
            await helper.Index(new ElasticArchiveRecord
            {
                ArchiveRecordId = "100", ExternalKeys =
                [
                    new ExternalKey {Key = "scopeArchiv", Value = "100"},
                    new ExternalKey {Key = "ActaPro", Value = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3"}
                ],
                ReferenceCode = "E12*"
            });

            var record = await helper.GetDbRecord("E12*", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.ArchiveRecordId.ShouldBe("100");
        }

        [Test]
        public async Task TestIfEncludeOfMetadataWorks()
        {
            await helper.Index(new ElasticArchiveRecord
            {
                ArchiveRecordId = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3", ExternalKeys =
                [
                    new ExternalKey {Key = "scopeArchiv", Value = "100"},
                    new ExternalKey {Key = "ActaPro", Value = "Vz      4e3e0b76-8f63-5a85-a018-289db658edd3"}
                ],

                PrimaryData =
                [
                    new ElasticArchiveRecordPackage()
                    {
                        FileCount = 1,
                        FulltextExtractionDuration = TimeSpan.FromSeconds(1500),
                        Items =
                        [
                            new ElasticRepositoryObject() {Name = "RepositoryObject1", Type = (long) ElasticRepositoryObjectType.File, LogicalName = "SomeLogicalName1", Content = "PrimaryDataContent1"},
                            new ElasticRepositoryObject() {Name = "RepositoryObject2", Type = (long) ElasticRepositoryObjectType.File, LogicalName = "SomeLogicalName2", Content = "PrimaryDataContent2"}
                        ]
                    }
                ]
            });

            var record = await helper.GetRecord("Vz%20%20%20%20%20%204e3e0b76-8f63-5a85-a018-289db658edd3", MetadataToExclude.Nothing);

            record.ShouldNotBeNull();
            record.PrimaryData.ShouldNotBeNull();
            record.PrimaryData.Count.ShouldBe(1);
            record.PrimaryData[0].Items.Count.ShouldBe(2);
            record.PrimaryData[0].Items[0].Content.ShouldBe("PrimaryDataContent1");
            record.PrimaryData[0].Items[1].Content.ShouldBe("PrimaryDataContent2");

            // Read it without the content of the OCR, but with the content of the other repository objects, to check that only the content of the OCR is excluded
            var record2 = await helper.GetRecord("Vz%20%20%20%20%20%204e3e0b76-8f63-5a85-a018-289db658edd3", MetadataToExclude.OCRContent);

            record2.ShouldNotBeNull();
            record2.PrimaryData.ShouldNotBeNull();
            record2.PrimaryData.Count.ShouldBe(1);
            record2.PrimaryData[0].Items.Count.ShouldBe(2);
            record2.PrimaryData[0].Items[0].Content.ShouldBeNull();
            record2.PrimaryData[0].Items[1].Content.ShouldBeNull();

            // Read it without the file details
            var record3 = await helper.GetRecord("Vz%20%20%20%20%20%204e3e0b76-8f63-5a85-a018-289db658edd3", MetadataToExclude.OCRContentAndFiles);

            record3.ShouldNotBeNull();
            record3.PrimaryData.ShouldNotBeNull();
            record3.PrimaryData.Count.ShouldBe(1);
            record3.PrimaryData[0].Items.Count.ShouldBe(0);
        }

        [Test]
        public async Task TestIfIndexIsReadOnly()
        {
            await helper.SetIndexReadOnly(true);
            var result = await helper.GetIndexHealth();

            result.IsReadOnly.ShouldBeTrue();

            await helper.SetIndexReadOnly(false);
            result = await helper.GetIndexHealth();

            result.IsReadOnly.ShouldBeFalse();
        }

        [Test]
        public async Task TestIfRecordCanBeRemoved()
        {
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "100" });
            helper.CountDocuments.ShouldBe(1);
            
            await helper.Remove("100");
            helper.CountDocuments.ShouldBe(0);
        }

        [Test]
        public async Task TestIfRemoveAllWorks()
        {
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "100" });
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "101" });
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "102" });
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "103" });
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "104" });
            helper.CountDocuments.ShouldBe(5);

            await helper.RemoveAll();
            helper.CountDocuments.ShouldBe(0);
        }

        [Test]
        public async Task TestUpdateTokens()
        {
            await helper.Index(new ElasticArchiveRecord { ArchiveRecordId = "100", 
                MetadataAccessTokens = ["BAR", "AS"],
                PrimaryDataDownloadAccessTokens = ["BAR", "AS"],
                PrimaryDataFulltextAccessTokens =  ["BAR", "AS"],
                FieldAccessTokens = ["BAR", "AS"],
            });
            
            await helper.UpdateTokens("100", 
                    primaryDataDownloadAccessTokens: ["NewToken1", "NewToken2"], 
                    primaryDataFulltextAccessTokens: ["NewToken3", "NewToken4"], 
                    fieldAccessTokens: ["NewToken5", "NewToken6"], 
                    metadataAccessTokens: ["NewToken7", "NewToken8"]);

            var result = await helper.GetRecord("100", MetadataToExclude.Nothing);

            result.PrimaryDataDownloadAccessTokens.ShouldBe(["NewToken1", "NewToken2"]);
            result.PrimaryDataFulltextAccessTokens.ShouldBe(["NewToken3", "NewToken4"]);
            result.FieldAccessTokens.ShouldBe(["NewToken5", "NewToken6"]);
            result.MetadataAccessTokens.ShouldBe(["NewToken7", "NewToken8"]);
        }

        [TearDown]
        public async Task TearDown()
        {
            if (helper != null)
            {
                if (await helper.IndexExists("test"))
                {
                    await helper.DeleteIndex("test");
                }
            }
        }
    }
}