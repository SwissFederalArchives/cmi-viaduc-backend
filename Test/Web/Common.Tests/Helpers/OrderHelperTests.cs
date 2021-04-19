using System.Collections.Generic;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Web.Common.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Web.Common.Tests.Helpers
{
    [TestFixture]
    public class OrderHelperTests
    {
        [Test]
        public void When_Providing_Unknown_Test_It_Should_Fill_Every_Secret_Property_With_UnknownText()
        {
            // arrange
            var record = new ElasticArchiveRecord();

            SetAllStringPropertiesWithThisText(record, "SECRET");

            record.Containers = new List<ElasticContainer>
            {
                new ElasticContainer
                {
                    ContainerCode = "SECRET",
                    ContainerCarrierMaterial = "SECRET",
                    ContainerLocation = "SECRET",
                    ContainerType = "SECRET",
                    IdName = "SECRET"
                }
            };

            // these fields are allowed to contain the secret texts
            var exceptions = new[]
            {
                nameof(OrderingIndexSnapshot.VeId),
                nameof(OrderingIndexSnapshot.Signatur),
                nameof(OrderingIndexSnapshot.ZugaenglichkeitGemaessBga)
            };

            // act
            var snapshot = OrderHelper.GetOrderingIndexSnapshot(record, "HIDDEN");

            // assert
            foreach (var prop in typeof(OrderingIndexSnapshot)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string) && !exceptions.Contains(p.Name)))
            {
                var value = prop.GetValue(snapshot);
                value.Should().Be("HIDDEN", $"Field {prop.Name} must not leak secret details");
            }
        }

        [Test]
        public void When_Applying_An_ElasticSnapshot_To_Detailitem_It_Should_Replace_Unknown_Texts_With_Real_Data()
        {
            // arrange
            var detailItem = new OrderingFlatItem();
            var snapshot = new OrderingIndexSnapshot();

            foreach (var prop in detailItem.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
                var isElasticProperty = typeof(OrderingIndexSnapshot).GetProperty(prop.Name) != null;
                if (!isElasticProperty)
                {
                    continue;
                }

                prop.SetValue(detailItem, "HIDDEN");
            }

            detailItem.BehaeltnisNummer = "HIDDEN";
            SetAllStringPropertiesWithThisText(snapshot, "SECRET");

            // act
            OrderHelper.ApplySnapshotToDetailItem(snapshot, detailItem);

            // assert
            foreach (var prop in typeof(OrderingFlatItem)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string)))
            {
                var value = prop.GetValue(detailItem) as string;
                if (value == null || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                value.Should().Be("SECRET", $"Field {prop.Name} must have the value of the snapshot");
            }
        }

        private void SetAllStringPropertiesWithThisText(object obj, string text)
        {
            foreach (var prop in obj.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
                prop.SetValue(obj, text);
            }
        }
    }
}