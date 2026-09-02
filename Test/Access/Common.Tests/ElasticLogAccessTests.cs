using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using Shouldly;
using NUnit.Framework;

namespace CMI.Access.Common.Tests
{
    [Ignore("Diese Tests müssen bewusst bzw. Bedarf ausgeführt werden")]
    [TestFixture]
    public class ElasticLogAccessTests
    {
        [SetUp]
        public void SetUp()
        {
            var uri = "(change here, but do not commit)";
            string username = "(change here, but do not commit)";
            string pwd = "(change here, but do not commit)";
            helper = new LogDataAccess(uri, username, pwd);
        }

        private LogDataAccess helper;

        [Test]
        public async Task GetLogRecords()
        {
           var result = await helper.GetLogData(new LogDataFilter(){ StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow });
           
           result.ShouldNotBeNull();
           result.Count.ShouldBeGreaterThan(0);
        }

        [Test]
        public void TryDeleteOldIndices()
        {
            helper.DeleteLogIndexes(DateTime.UtcNow.AddDays(-30));
        }

    }
}