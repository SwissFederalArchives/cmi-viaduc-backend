
using System.Collections.Generic;
using CMI.Contract.Common;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Manager.Index.Tests
{
    [TestFixture]
    public class IndexManagerTest
    {
        [Test]
        public void CalculateCreationPeriodBuckets2DecadesTest()
        {
            // Arrange
            var list = new List<int>() { 1979, 1980, 1981, 1982, 1983, 1984 };
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> {1975, 1980} );
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1970, 1980 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1975 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1900 });
        }

        [Test]
        public void CalculateCreationPeriodBuckets3DecadesTest()
        {
            // Arrange
            var list = new List<int> {1969, 1970, 1971, 1972, 1973, 1974, 1975, 1976, 1977, 1978, 1979, 1980, 1981, 1982, 1983, 1984, 1985, 1986 };
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> { 1965, 1970, 1975, 1980, 1985 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1960, 1970, 1980 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1950, 1975 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1900 });
        }

        [Test]
        public void CalculateCreationPeriodBuckets25YearTest()
        {
            // Arrange
            var list = new List<int> { 1979, 1980, 1981, 1982, 1983, 1984, 1985, 1986, 1987, 1988, 1989, 1989, 1990, 1991, 1992, 1993, 1994, 1995, 1996, 1997, 1998, 1999, 2000, 2001, 2002, 2003, 2004 };
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> { 1975, 1980, 1985, 1990, 1995, 2000 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1970, 1980, 1990, 2000 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1975, 2000 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1900, 2000 });
        }

        [Test]
        public void CalculateCreationPeriodBuckets79Until98Test()
        {
            // Arrange
            var list = new List<int>
            {
                1979,
                1980,
                1981,
                1982,
                1983,
                1984,
                1985,
                1986,
                1987,
                1988,
                1989,
                1990,
                1991,
                1992,
                1993,
                1994,
                1995,
                1996,
                1997,
                1998
            };
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> { 1975, 1980, 1985, 1990, 1995 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1970, 1980, 1990 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1975 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1900 });
        }


        [Test]
        public void CalculateCreationPeriodBucketsOneYearTest()
        {
            // Arrange
            var list = new List<int> { 1974, 1975};
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> { 1970, 1975});
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1970});
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1950,  1975});
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1900});
        }

        [Test]
        public void CalculateCreationPeriodBucketsTwoYearTwoCenturiesTest()
        {
            // Arrange
            var list = new List<int> { 1899, 1900 };
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> { 1895, 1900 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1890, 1900 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1875, 1900 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1800, 1900 });
        }

        [Test]
        public void CalculateCreationPeriodBucketsTwoYearTwoDecadesTest()
        {
            // Arrange
            var list = new List<int> { 1989, 1990 };
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> { 1985, 1990 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1980, 1990 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1975 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1900 });

        }



        [Test]
        public void CalculateCreationPeriodBucketsUntypicalDataTest()
        {
            // Arrange
            var list = new List<int> { 1789, 1982 };
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> { 1785, 1980 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1780, 1980 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1775, 1975 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1700, 1900 });
        }

        [Test]
        public void CalculateCreationPeriodBucketsEvery8From1949YearsTest()
        {
            // Arrange
            var list = new List<int> { 1949, 1957, 1965, 1973, 1981, 1989, 1997, 2005, 2013, 2021 };
            var elasticArchiveRecord = CreateElasticArchiveRecord(list);

            // Act
            IndexManager.CalculateCreationPeriodBuckets(elasticArchiveRecord);

            // Assert
            elasticArchiveRecord.AggregationFields.CreationPeriodYears001.Should().BeEquivalentTo(list);
            elasticArchiveRecord.AggregationFields.CreationPeriodYears005.Should().BeEquivalentTo(new List<int> { 1945, 1955, 1965, 1970, 1980, 1985, 1995, 2005, 2010, 2020});
            elasticArchiveRecord.AggregationFields.CreationPeriodYears010.Should().BeEquivalentTo(new List<int> { 1940, 1950, 1960, 1970, 1980, 1990,2000, 2010, 2020 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears025.Should().BeEquivalentTo(new List<int> { 1925, 1950, 1975, 2000 });
            elasticArchiveRecord.AggregationFields.CreationPeriodYears100.Should().BeEquivalentTo(new List<int> { 1900, 2000 });
        }

        private static ElasticArchiveRecord CreateElasticArchiveRecord(List<int> list)
        {
            // Arrange
            var elasticArchiveRecord = new ElasticArchiveRecord()
            {
                ArchiveRecordId = "Start1",
                PrimaryDataLink = "DifferentAip@DossierId",
                AggregationFields = new ElasticAggregationFields(),
                CreationPeriod = new ElasticTimePeriod()
            };


            elasticArchiveRecord.CreationPeriod.Years = list;
            return elasticArchiveRecord;
        }
    }
}
