using System;
using System.Collections.Generic;
using System.Dynamic;
using CMI.Contract.Common;
using CMI.Engine.Anonymization.Tests.Mocks;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace CMI.Engine.Anonymization.Tests
{
    [TestFixture]
    public class AnonymizationEngineTests
    {

        [Test]
        public void Anonymization_with_empty_ElasticArchiveDbRecord()
        {
            var engine = new AnonymizationEngineMock(null);
            var result = engine.AnonymizeArchiveRecordAsync(new ElasticArchiveDbRecord());
            result.Result.IsAnonymized.Should().Be(false);
        }

        [Test]
        public void Anonymization_orginalText_in_copy_to_protectedFields()
        {
            // arrange
            var ac = new List<ElasticArchiveplanContextItem>();
            var pc = new List<ElasticParentContentInfo>();
            var references = new List<ElasticReference>();
            for (int index = 0; index < 5; index++)
            {
                ac.Add(new ElasticArchiveplanContextItem
                {
                    Title = "Der schnelle Hans " + (index + 1),
                    ArchiveRecordId = (index * 11).ToString(),
                    Protected = true
                });
                pc.Add(new ElasticParentContentInfo
                {
                    Title = "Der graue Franz " + (index + 1)
                });
                references.Add(new ElasticReference
                {
                    ReferenceName = "Test Text",
                    Protected = true,
                    ArchiveRecordId = (1001 * (index + 2)).ToString()
                });
            }

            references.Add(new ElasticReference
            {
                ReferenceName = "Weiterer Text",
                Protected = true,
                ArchiveRecordId = "123458"
            });

            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "3452",
                MetadataAccessTokens = new List<string> { "BAR" },
                PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
                PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                FieldAccessTokens = new List<string> { "BAR" },
                IsAnonymized = false,
                Title = "Klaus Reiner Ichzerts",
                WithinInfo = "Ist drin",
                ParentContentInfos = pc,
                ArchiveplanContext = ac,
                References = references
            };

            // act
            var engine = new AnonymizationEngineMock(null);
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            result.Result.UnanonymizedFields.ParentContentInfos.Count.Should().Be(5);
            result.Result.UnanonymizedFields.ArchiveplanContext.Count.Should().Be(5);
            result.Result.UnanonymizedFields.References.Count.Should().Be(6);
            result.Result.Title.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.UnanonymizedFields.Title.Should().Be("Klaus Reiner Ichzerts");
            result.Result.WithinInfo.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.UnanonymizedFields.WithinInfo.Should().Be("Ist drin");
            for (int index = 0; index < 5; index++)
            {
                result.Result.UnanonymizedFields.ArchiveplanContext[index].Title.Should().Be("Der schnelle Hans " + (index + 1));
                result.Result.UnanonymizedFields.References[index].ReferenceName.Should().Be("Test Text");
                result.Result.UnanonymizedFields.ParentContentInfos[index].Title.Should().NotContain("███");

                result.Result.ArchiveplanContext[index].Title.Should().Be(engine.AnonymTagWithBlockqute);
                result.Result.References[index].ReferenceName.Should().Be(engine.AnonymTagWithBlockqute);
                // Must be the same title like archiveplanContext
                result.Result.ParentContentInfos[index].Title.Should().Be(result.Result.ArchiveplanContext[index].Title);
            }

            result.Result.References[5].ReferenceName.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.UnanonymizedFields.References[5].ReferenceName.Should().Be("Weiterer Text");
            result.Result.IsAnonymized.Should().Be(true);
        }


        [Test]
        public void Anonymization_orginalText_has_no_Text_which_must_be_anonymized()
        {
            // arrange
            var ac = new List<ElasticArchiveplanContextItem>();
            var pc = new List<ElasticParentContentInfo>();
            var references = new List<ElasticReference>();
            for (int index = 0; index < 5; index++)
            {
                ac.Add(new ElasticArchiveplanContextItem
                {
                    Title = "Der schnelle Hans " + (index + 1),
                    ArchiveRecordId = (index * 11).ToString(),
                    Protected = true
                });
                pc.Add(new ElasticParentContentInfo
                {
                    Title = "Der graue Franz " + (index + 1)
                });
                references.Add(new ElasticReference
                {
                    ReferenceName = "Test Text",
                    Protected = true,
                    ArchiveRecordId = (1001 * (index + 2)).ToString()
                });
            }

            references.Add(new ElasticReference
            {
                ReferenceName = "Weiterer Text",
                Protected = true,
                ArchiveRecordId = "87892"
            });

            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                ArchiveRecordId = "3452",
                MetadataAccessTokens = new List<string> { "BAR" },
                PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
                PrimaryDataDownloadAccessTokens = new List<string> { "BAR" },
                FieldAccessTokens = new List<string> { "BAR" },
                IsAnonymized = false,
                Title = "Klaus Reiner Ichzerts",
                WithinInfo = "Ist drin",
                ParentContentInfos = pc,
                ArchiveplanContext = ac,
                References = references
            };

            // act
            var engine = new AnonymizationEngineMock(null);
            engine.ReturnValueFromServiceCall = "nothing to anonymize";
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            result.Result.UnanonymizedFields.ParentContentInfos.Count.Should().Be(5);
            result.Result.UnanonymizedFields.ArchiveplanContext.Count.Should().Be(5);
            result.Result.UnanonymizedFields.References.Count.Should().Be(6);
            result.Result.Title.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.UnanonymizedFields.Title.Should().Be("Klaus Reiner Ichzerts");
            result.Result.WithinInfo.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.UnanonymizedFields.WithinInfo.Should().Be("Ist drin");
            for (int index = 0; index < 5; index++)
            {
                result.Result.UnanonymizedFields.ArchiveplanContext[index].Title.Should().Be("Der schnelle Hans " + (index + 1));
                result.Result.UnanonymizedFields.References[index].ReferenceName.Should().Be("Test Text");
                result.Result.UnanonymizedFields.ParentContentInfos[index].Title.Should().NotContain("███");

                result.Result.ArchiveplanContext[index].Title.Should().Be(engine.AnonymTagWithBlockqute);
                result.Result.References[index].ReferenceName.Should().Be(engine.AnonymTagWithBlockqute);
                // Must be the same title like archiveplanContext
                result.Result.ParentContentInfos[index].Title.Should().Be(result.Result.ArchiveplanContext[index].Title);
            }

            result.Result.References[5].ReferenceName.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.UnanonymizedFields.References[5].ReferenceName.Should().Be("Weiterer Text");
            result.Result.IsAnonymized.Should().Be(false);
        }

        [Test]
        public void Anonymization_with_protected_and_notProtected_Items()
        {
            // arrange
            var ac = new List<ElasticArchiveplanContextItem>();
            var pc = new List<ElasticParentContentInfo>();
            var references = new List<ElasticReference>();
            for (int index = 0; index < 6; index++)
            {
                ac.Add(new ElasticArchiveplanContextItem
                {
                    Title = "Der schnelle Hans " + (index + 1),
                    Protected = index % 2 == 0,
                    ArchiveRecordId = (1002 * (index + 2)).ToString()
                });
                pc.Add(new ElasticParentContentInfo
                {
                    Title = "Der graue Franz " + (index + 1)
                });
                references.Add(new ElasticReference
                {
                    ReferenceName = "Test Text",
                    Protected = index % 2 == 1,
                    ArchiveRecordId = (1001 * (index + 2)).ToString()
                });
            }

            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                ArchiveplanContext = ac,
                References = references,
                ParentContentInfos = pc
            };

            // act
            var engine = new AnonymizationEngineMock(null);
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            result.Result.UnanonymizedFields.ArchiveplanContext.Count.Should().Be(6);
            result.Result.UnanonymizedFields.References.Count.Should().Be(6);
            result.Result.UnanonymizedFields.ParentContentInfos.Count.Should().Be(6);
            result.Result.ArchiveplanContext.Count.Should().Be(6);
            result.Result.References.Count.Should().Be(6);
            result.Result.ParentContentInfos.Count.Should().Be(6);

            for (int index = 0; index < 6; index++)
            {
                if (result.Result.ArchiveplanContext[index].Protected)
                {
                    result.Result.ArchiveplanContext[index].Title.Should().Contain("███");
                    result.Result.ArchiveplanContext[index].Title.Should().Be(engine.AnonymTagWithBlockqute);
                    result.Result.UnanonymizedFields.ArchiveplanContext[index].Title.Should().Be("Der schnelle Hans " + (index + 1));
                }
                else
                {
                    result.Result.ArchiveplanContext[index].Title.Should().Be("Der schnelle Hans " + (index + 1));
                    result.Result.ArchiveplanContext[index].Title.Should().Be(result.Result.UnanonymizedFields.ArchiveplanContext[index].Title);
                }

                if (result.Result.References[index].Protected)
                {
                    result.Result.References[index].ReferenceName.Should().Be(engine.AnonymTagWithBlockqute);
                    result.Result.UnanonymizedFields.References[index].ReferenceName.Should().Be("Test Text");
                }
                else
                {
                    result.Result.References[index].ReferenceName.Should().Be(result.Result.UnanonymizedFields.References[index].ReferenceName);
                    result.Result.References[index].ReferenceName.Should().Be("Test Text");
                }

                // Must be the same title like archiveplanContext
                result.Result.ParentContentInfos[index].Title.Should().Be(result.Result.ArchiveplanContext[index].Title);
            }

            result.Result.IsAnonymized.Should().Be(true);
        }

        [Test]
        public void Anonymization_with_CustomFields()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;

            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("zuständigeStelle", "zuständigeStelle");
            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                CustomFields = customFields
            };

            // act
            var engine = new AnonymizationEngineMock(null);
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            var zusätzlicheInformationen = result.Result.ZusätzlicheInformationen();
            if (!string.IsNullOrWhiteSpace(zusätzlicheInformationen))
            {
                zusätzlicheInformationen.Should().Be(engine.AnonymTagWithBlockqute);
            }

            var zusatzmerkmal = result.Result.Zusatzmerkmal();
            if (!string.IsNullOrWhiteSpace(zusatzmerkmal))
            {
                zusätzlicheInformationen.Should().Be(engine.AnonymTagWithBlockqute);
            }

            var verwandteVe = result.Result.VerwandteVe();
            if (!string.IsNullOrWhiteSpace(verwandteVe))
            {
                verwandteVe.Should().Be(engine.AnonymTagWithBlockqute);
            }

            result.Result.IsAnonymized.Should().Be(true);
        }

        [Test]
        public void Anonymization_ArchiveplanContext_with_protected_and_notProtected_Fields()
        {
            // arrange
            var ac = new List<ElasticArchiveplanContextItem>();
            for (int index = 0; index < 7; index++)
            {
                ac.Add(new ElasticArchiveplanContextItem
                {
                    Title = "Der schnelle Hans " + (index + 1),
                    Protected = index % 2 == 0,
                    RefCode = (101 * (index + 1)).ToString(),
                    ArchiveRecordId = (1001 * (index + 2)).ToString()
                });
            }

            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                ArchiveplanContext = ac
            };

            // act
            var engine = new AnonymizationEngineMock(null);
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            result.Result.UnanonymizedFields.ArchiveplanContext.Count.Should().Be(7);
            for (int index = 0; index < 7; index++)
            {
                if (result.Result.ArchiveplanContext[index].Protected)
                {
                    result.Result.ArchiveplanContext[index].Title.Should().Contain("███");
                    result.Result.ArchiveplanContext[index].Title.Should().Be(engine.AnonymTagWithBlockqute);
                    result.Result.ArchiveplanContext[index].Title.Should().NotBe(result.Result.UnanonymizedFields.ArchiveplanContext[index].Title);
                }
                else
                {
                    result.Result.ArchiveplanContext[index].Title.Should().Be("Der schnelle Hans " + (index + 1));
                    result.Result.ArchiveplanContext[index].Title.Should().NotBe(engine.AnonymTagWithBlockqute);
                    result.Result.ArchiveplanContext[index].Title.Should().NotContain("███");
                    result.Result.ArchiveplanContext[index].Title.Should().Be(result.Result.UnanonymizedFields.ArchiveplanContext[index].Title);
                }

                // Must be the same title like archiveplanContext
                result.Result.ParentContentInfos[index].Title.Should().Be(result.Result.ArchiveplanContext[index].Title);
            }

            result.Result.IsAnonymized.Should().Be(true);
        }

        [Test]
        public void Anonymization_ServiceResult_with_Line_breaks()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("zuständigeStelle", "zuständigeStelle");
            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                Title = "To anonymize",
                CustomFields = customFields

            };
            var engine = new AnonymizationEngineMock(null);
            engine.ReturnValueFromServiceCall = $"Dies ist ein <anonym type='n'>Test{Environment.NewLine}geschwärzter</anonym> Text";

            // act
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            result.Result.Title.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.Title.Should().Contain("███");
            result.Result.CustomFields.bemerkungZurVe.Equals(engine.AnonymTagWithBlockqute);
            result.Result.CustomFields.zusatzkomponenteZac1.Equals(engine.AnonymTagWithBlockqute);
            result.Result.CustomFields.zuständigeStelle.Equals(engine.AnonymTagWithBlockqute);
        }

        [Test]
        public void Anonymization_ServiceResult_with_two_Line_breaks_outside_anonymization()
        {
            // arrange
            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                Title = "To anonymize",

            };
            var engine = new AnonymizationEngineMock(null);
            engine.ReturnValueFromServiceCall =
                $"Dies {Environment.NewLine} ist ein <anonym type='n'>Test geschwärzter</anonym> {Environment.NewLine} Text";

            // act
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            result.Result.Title.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.Title.Should().Contain("███");
            result.Result.Title.Should().Contain(Environment.NewLine);
        }

        [Test]
        public void Anonymization_ServiceResult_with_one_Line_breaks_in_and_one_outside()
        {
            // arrange
            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                Title = "To anonymize",

            };
            var engine = new AnonymizationEngineMock(null);
            engine.ReturnValueFromServiceCall =
                $"Dies ist ein <anonym type='n'>Test {Environment.NewLine} geschwärzter</anonym> {Environment.NewLine} Text";

            // act
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            result.Result.Title.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.Title.Should().Contain("███");
            result.Result.Title.Should().Contain(Environment.NewLine);
        }

        [Test]
        public void Anonymization_ServiceResult_with_special_Line_break()
        {
            // arrange
            var customFields = new ExpandoObject() as IDictionary<string, object>;
            customFields.Add("bemerkungZurVe", "bemerkungZurVe");
            customFields.Add("zusatzkomponenteZac1", "zusatzkomponenteZac1");
            customFields.Add("zuständigeStelle", "zuständigeStelle");
            var elasticArchiveRecord = new ElasticArchiveDbRecord
            {
                Title = "To anonymize",
                CustomFields = customFields

            };
            var engine = new AnonymizationEngineMock(null);
            engine.ReturnValueFromServiceCall = "Dies ist ein <anonym type='n'>Test \n geschwärzter</anonym> Text";

            // act
            var result = engine.AnonymizeArchiveRecordAsync(elasticArchiveRecord);

            // assert
            result.Result.Title.Should().Be(engine.AnonymTagWithBlockqute);
            result.Result.Title.Should().Contain("███");
            result.Result.CustomFields.bemerkungZurVe.Equals(engine.AnonymTagWithBlockqute);
            result.Result.CustomFields.zusatzkomponenteZac1.Equals(engine.AnonymTagWithBlockqute);
            result.Result.CustomFields.zuständigeStelle.Equals(engine.AnonymTagWithBlockqute);
        }
    }
}
