using System;
using System.Linq;
using CMI.Contract.Common;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Access.Common.Tests
{
    [Ignore("Diese Tests müssen bewusst bzw. Bedarf ausgeführt werden")]
    [TestFixture]
    public class ElasticIndexHelperTests
    {
        [SetUp]
        public void SetUp()
        {
            var uri = "(change here, but do not commit)";
            var node = new Uri(uri);

            helper = new ElasticIndexHelper(node, "test");
            if (helper.IndexExists("test"))
            {
                helper.DeleteIndex("test");
            }

            helper.CreateIndex("test");
        }

        private ElasticIndexHelper helper;

        [Test]
        public void ShouldInsertARecord()
        {
            helper.CountDocuments.Should().Be(0);
            helper.Index(new ElasticArchiveRecord {ArchiveRecordId = "100"});
            helper.CountDocuments.Should().Be(1);
        }

        [Test]
        public void ShouldInsert1000Records()
        {
            var records = Enumerable.Range(1, 10000).Select(i => TestDataGenerator.Generate(i));
            helper.IndexBulk(records);
            helper.CountDocuments.Should().Be(10000);
        }
    }
}