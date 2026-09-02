using System.Collections.Generic;
using System.Dynamic;
using CMI.Contract.Common;
using CMI.Contract.Common.Extensions;
using CMI.Web.Frontend.api.Elastic;
using NUnit.Framework;
using Shouldly;

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
            record.Title.ShouldBe("Nur fuer Berechtigete");
            record.WithinInfo.ShouldBe("Nicht  mehr anonymiziert");
            ((IDictionary<string, object>) record.CustomFields)["verwandteVe"].ShouldBe("Geheim VerwandteVe");
            ((IDictionary<string, object>) record.CustomFields)["zusatzkomponenteZac1"].ShouldBe("Geheim Zusatzmerkmal");
            ((IDictionary<string, object>) record.CustomFields)["bemerkungZurVe"].ShouldBe("Geheim ZusätzlicheInformationen");
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
            record.Title.ShouldBe("Original");
            record.WithinInfo.ShouldBe("Bin drin");
            ((IDictionary<string, object>) record.CustomFields)["verwandteVe"].ShouldBe("verwandteVe");
            ((IDictionary<string, object>) record.CustomFields)["zusatzkomponenteZac1"].ShouldBe("zusatzkomponenteZac1");
            ((IDictionary<string, object>) record.CustomFields)["bemerkungZurVe"].ShouldBe("bemerkungZurVe");
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
            record.Title.ShouldBe("Original");
            record.WithinInfo.ShouldBe("Bin drin");
            ((IDictionary<string, object>) record.CustomFields)["verwandteVe"].ShouldBe("verwandteVe");
            ((IDictionary<string, object>) record.CustomFields)["zusatzkomponenteZac1"].ShouldBe("zusatzkomponenteZac1");
            ((IDictionary<string, object>) record.CustomFields)["bemerkungZurVe"].ShouldBe("bemerkungZurVe");
        }
    }
}
