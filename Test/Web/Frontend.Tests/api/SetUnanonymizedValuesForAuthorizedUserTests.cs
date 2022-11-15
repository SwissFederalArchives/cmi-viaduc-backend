using System.Collections.Generic;
using System.Dynamic;
using CMI.Contract.Common;
using CMI.Contract.Common.Extensions;
using CMI.Web.Frontend.api.Elastic;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    internal class SetUnanonymizedValuesForAuthorizedUserTests
    {
       
        [Test]
        public void Test_if_UnanonymizedField_were_set()
        {
            // ARRANGE
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");
            var record = new SearchRecord
            {
                Title = "Original",
                WithinInfo = "Bin drin",
                CustomFields = customFields,
                IsAnonymized = true
            };
            var elasticDbRecord = new ElasticArchiveDbRecord
            {
                UnanonymizedFields = new UnanonymizedFields
                {
                    Title = "Nur fuer Berechtigete",
                    WithinInfo = "Nicht  mehr anonymiziert",
                    BemerkungZurVe = "Geheim ZusätzlicheInformationen",
                    ZusatzkomponenteZac1 = "Geheim Zusatzmerkmal",
                    VerwandteVe = "Geheim VerwandteVe"
                }
            };

            // Act
            record.SetUnanonymizedValuesForAuthorizedUser(elasticDbRecord);

            // Assert
            record.Title.Should().Be("Nur fuer Berechtigete");
            record.WithinInfo.Should().Be("Nicht  mehr anonymiziert");
            ((IDictionary<string, object>) record.CustomFields)["verwandteVe"].Should().Be("Geheim VerwandteVe");
            ((IDictionary<string, object>) record.CustomFields)["zusatzkomponenteZac1"].Should().Be("Geheim Zusatzmerkmal");
            ((IDictionary<string, object>) record.CustomFields)["bemerkungZurVe"].Should().Be("Geheim ZusätzlicheInformationen");
        }

        [Test]
        public void Test_if_fields_are_untouched_if_record_is_not_anonymized()
        {
            // ARRANGE
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");
            var record = new SearchRecord
            {
                Title = "Original",
                WithinInfo = "Bin drin",
                CustomFields = customFields,
                IsAnonymized = false
            };
            var elasticDbRecord = new ElasticArchiveDbRecord
            {
                UnanonymizedFields = new UnanonymizedFields
                {
                    Title = "Nur fuer Berechtigete",
                    WithinInfo = "Nicht  mehr anonymiziert",
                    BemerkungZurVe = "Geheim ZusätzlicheInformationen",
                    ZusatzkomponenteZac1 = "Geheim Zusatzmerkmal",
                    VerwandteVe = "Geheim VerwandteVe"
                }
            };

            // Act
            record.SetUnanonymizedValuesForAuthorizedUser(elasticDbRecord);

            // Assert
            record.Title.Should().Be("Original");
            record.WithinInfo.Should().Be("Bin drin");
            ((IDictionary<string, object>) record.CustomFields)["verwandteVe"].Should().Be("verwandteVe");
            ((IDictionary<string, object>) record.CustomFields)["zusatzkomponenteZac1"].Should().Be("zusatzkomponenteZac1");
            ((IDictionary<string, object>) record.CustomFields)["bemerkungZurVe"].Should().Be("bemerkungZurVe");
        }

        [Test]
        public void Test_if_fields_are_untouched_if_unprotectedFields_property_is_empty()
        {
            // ARRANGE
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("verwandteVe", "verwandteVe");
            var record = new SearchRecord
            {
                Title = "Original",
                WithinInfo = "Bin drin",
                CustomFields = customFields,
                IsAnonymized = true
            };
            var elasticDbRecord = new ElasticArchiveDbRecord
            {
                UnanonymizedFields = new UnanonymizedFields
                {
                    Title = null,
                    WithinInfo = null,
                    BemerkungZurVe = null,
                    ZusatzkomponenteZac1 = null,
                    VerwandteVe = null
                }
            };

            // Act
            record.SetUnanonymizedValuesForAuthorizedUser(elasticDbRecord);

            // Assert
            record.Title.Should().Be("Original");
            record.WithinInfo.Should().Be("Bin drin");
            ((IDictionary<string, object>) record.CustomFields)["verwandteVe"].Should().Be("verwandteVe");
            ((IDictionary<string, object>) record.CustomFields)["zusatzkomponenteZac1"].Should().Be("zusatzkomponenteZac1");
            ((IDictionary<string, object>) record.CustomFields)["bemerkungZurVe"].Should().Be("bemerkungZurVe");
        }
    }
}
