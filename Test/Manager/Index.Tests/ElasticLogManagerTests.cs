using System;
using CMI.Manager.Index;
using Shouldly;
using NUnit.Framework;

namespace CMI.Manager.Index.Tests
{
    [TestFixture]
    public class ElasticLogManagerTests
    {
        [Test]
        public void NormalizeElasticsearchTimestampToLocal_WhenKindIsUtc_ConvertsToLocalTime()
        {
            // Arrange
            var utcTimestamp = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

            // Act
            var result = ElasticLogManager.NormalizeElasticsearchTimestampToLocal(utcTimestamp);

            // Assert
            result.ShouldBe(utcTimestamp.ToLocalTime());
            result.Kind.ShouldBe(DateTimeKind.Local);
        }

        [Test]
        public void NormalizeElasticsearchTimestampToLocal_WhenKindIsUnspecified_TreatsAsUtcAndConvertsToLocalTime()
        {
            // Arrange – Elasticsearch @timestamp values parsed without a timezone suffix arrive as Unspecified.
            // They must be interpreted as UTC before converting to local time.
            var unspecifiedTimestamp = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Unspecified);
            var expectedUtcTimestamp = DateTime.SpecifyKind(unspecifiedTimestamp, DateTimeKind.Utc);

            // Act
            var result = ElasticLogManager.NormalizeElasticsearchTimestampToLocal(unspecifiedTimestamp);

            // Assert
            result.ShouldBe(expectedUtcTimestamp.ToLocalTime());
            result.Kind.ShouldBe(DateTimeKind.Local);
        }

        [Test]
        public void NormalizeElasticsearchTimestampToLocal_WhenKindIsLocal_ReturnsLocalTime()
        {
            // Arrange
            var localTimestamp = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Local);

            // Act
            var result = ElasticLogManager.NormalizeElasticsearchTimestampToLocal(localTimestamp);

            // Assert
            result.ShouldBe(localTimestamp.ToLocalTime());
            result.Kind.ShouldBe(DateTimeKind.Local);
        }

        [Test]
        public void NormalizeElasticsearchTimestampToLocal_UnspecifiedAndUtcWithSameValue_ProduceSameResult()
        {
            // Arrange – Unspecified timestamps from Elasticsearch represent the same UTC instant
            // as an explicitly UTC-tagged timestamp with the same value.
            var utcTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
            var unspecifiedTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Unspecified);

            // Act
            var resultFromUtc = ElasticLogManager.NormalizeElasticsearchTimestampToLocal(utcTime);
            var resultFromUnspecified = ElasticLogManager.NormalizeElasticsearchTimestampToLocal(unspecifiedTime);

            // Assert
            resultFromUnspecified.ShouldBe(resultFromUtc);
        }
    }
}
