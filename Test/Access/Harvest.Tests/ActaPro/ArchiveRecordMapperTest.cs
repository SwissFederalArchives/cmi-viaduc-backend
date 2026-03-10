﻿using CMI.Access.Harvest.ActaPro;
using CMI.Access.Harvest.ActaPro.Mapping;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Action = System.Action;

namespace CMI.Access.Harvest.Tests.ActaPro
{

    [TestFixture]
    public class ArchiveRecordMapperTest
    {
        #region Title Field

        [Test]
        public void Ve_With_Level_Vz_Map_Vz_Titel_Field_Must_Have_ElementName_Title_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField()
            {
                Type = "Vz_Titel",
                Value = "Die Id"
            });

            var level = "Vz";

            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);
            // Assert
            result.ElementName.Should().Be("TITEL");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Die Id");

        }

        [Test]
        public void Ve_With_Level_Doku_Map_Vz_Titel_Field_Must_Have_ElementName_Title_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField()
            {
                Type = "Vz_Titel",
                Value = "Die Id"
            });

            var level = "dOkum";

            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);
            // Assert
            result.ElementName.Should().Be("TITEL");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Die Id");

        }

        [Test]
        public void Ve_With_Level_Vor_Map_Vz_Titel_Field_Must_Have_ElementName_Title_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField
            {
                Type = "Vz_Titel",
                Value = "Die Id"
            });

            var level = "Vor";

            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);
            // Assert
            result.ElementName.Should().Be("TITEL");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Die Id");

        }

        [Test]
        public void Ve_With_Level_Klas_Map_Kl_Name_Field_Must_Have_ElementName_Title_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField
            {
                Type = "Kl_Name",
                Value = "Die Klas Id"
            });

            var level = "Klas";

            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);

            // Assert
            result.ElementName.Should().Be("TITEL");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Die Klas Id");

        }

        [Test]
        public void Ve_With_Level_TBest_Map_Best_Name_Field_Must_Have_ElementName_Title_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField
            {
                Type = "Bst_Name",
                Value = "Die TBest Id"
            });

            var level = "TBest";

            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);

            // Assert
            result.ElementName.Should().Be("TITEL");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Die TBest Id");

        }

        [Test]
        public void Ve_With_Level_Best_Map_Best_Name_Field_Must_Have_ElementName_Title_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField
            {
                Type = "Bst_Name",
                Value = "Die neue Id"
            });

            var level = "Best";

            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);

            // Assert
            result.ElementName.Should().Be("TITEL");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Die neue Id");

        }

        [Test]
        public void Ve_With_Level_Tekt_Map_Te_Name_Field_Must_Have_ElementName_Title_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField
            {
                Type = "Te_Name",
                Value = "Die Hauptabteilung"
            });

            var level = "Tekt";

            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);

            // Assert
            result.ElementName.Should().Be("TITEL");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Die Hauptabteilung");

        }

        [Test]
        public void Ve_With_Level_Arch_Map_Arch_Name_Field_Must_Have_ElementName_Title_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField
            {
                Type = "Ar_Name",
                Value = "Das Archiv"
            });

            var level = "Arch";

            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);

            // Assert
            result.ElementName.Should().Be("TITEL");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Das Archiv");

        }

        [Test]
        public void Ve_With_Level_OOO_Map_Arch_Name_Field_Must_Have_No_Title_Map()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var document = new Document();
            document.Block.Fields.Add(new DocumentField
            {
                Type = "Ar_Name",
                Value = "Das Archiv"
            });

            var level = "OOO";


            // Act
            var result = mapper.MapTitle(document.Block.Fields, level);

            // Assert
            result.ElementValue.Count.Should().Be(0, "Do not know the level");

        }

        #endregion

        #region Darin Field

        [Test]
        public void Ve_With_Level_Vz_Map_Vz_DarinGroup_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Vz_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Vz_Darin_1",
                            Value = "Ich bin drin"
                        },
                        new()
                        {
                            Type = "Vz_Darin_2",
                            Value = "Zweite Reihe"
                        }
                    }
                }
            };

            var fieldName = "Darin";
            var level = "Vz";
            var elementName = "DARIN";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);
            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Ich bin drin");
            result.ElementValue[1].TextValues[0].Value.Should().Be("Zweite Reihe");
        }

        [Test]
        public void Ve_With_Level_Doku_Map_Vz_DarinGroup_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Vz_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Vz_Darin_1",
                            Value = "1"
                        },
                        new()
                        {
                            Type = "Vz_Darin_2",
                            Value = "2"
                        }
                    }
                }
            };

            var fieldName = "Darin";
            var level = "dOkum";
            var elementName = "DARIN";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);
            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("1");
            result.ElementValue[1].TextValues[0].Value.Should().Be("2");
        }

        [Test]
        public void Ve_With_Level_Vor_Map_Vz_DarinGroup_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Vz_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Vz_Darin_1",
                            Value = "Nur einer"
                        }
                    }
                }
            };

            var fieldName = "Darin";
            var elementName = "DARIN";
            var level = "VOR";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);
            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Nur einer");

        }

        [Test]
        public void Ve_With_Level_Vor_Map_Vz_Darin_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Vz_Darin",
                    Value = "Nur einer"
                }
            };

            var fieldName = "Darin";
            var elementName = "DARIN";
            var level = "VOR";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);
            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Nur einer");

        }

        [Test]
        public void Ve_With_Level_Best_Map_Klas_DarinGroup_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Kl_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Darin_1",
                            Value = "Ich bin drin"
                        },
                        new()
                        {
                            Type = "Darin_2",
                            Value = "Zweite Reihe"
                        }
                    }
                }
            };
            var fieldName = "Darin";
            var elementName = "DARIN";
            var level = "Klas";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);

            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Ich bin drin");
            result.ElementValue[1].TextValues[0].Value.Should().Be("Zweite Reihe");
        }

        [Test]
        public void Ve_With_Level_TBest_Map_Best_DarinGroup_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Bst_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Darin_1",
                            Value = "Ich bin drin"
                        },
                        new()
                        {
                            Type = "Darin_2",
                            Value = "Zweite Reihe"
                        }
                    }
                }
            };
            var fieldName = "Darin";
            var level = "TBest";
            var elementName = "DARIN";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);

            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Ich bin drin");
            result.ElementValue[1].TextValues[0].Value.Should().Be("Zweite Reihe");
        }

        [Test]
        public void Ve_With_Level_Best_Map_Best_DarinGroup_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Bst_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Darin_1",
                            Value = "Test"
                        }
                    }
                }
            };
            var fieldName = "Darin";
            var level = "Best";
            var elementName = "DARIN";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);

            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Test");
        }

        [Test]
        public void Ve_With_Level_Tekt_Map_Te_DarinGroup_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Te_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Tekt",
                            Value = "Tekt"
                        }
                    }
                }
            };
            var fieldName = "Darin";
            var level = "Tekt";
            var elementName = "DARIN";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);

            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Tekt");
        }

        [Test]
        public void Ve_With_Level_Arch_Map_Arch_DarinGroup_Field_Must_Have_ElementName_DARIN_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Ar_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Arch",
                            Value = "Arch 1"
                        }
                    }
                }
            };
            var fieldName = "Darin";
            var level = "Arch";
            var elementName = "DARIN";

            // Act
            var result = MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);

            // Assert
            result.ElementName.Should().Be("DARIN");
            result.ElementValue[0].TextValues[0].Value.Should().Be("Arch 1");

        }

        [Test]
        public void Ve_With_Level_OOO_Map_Arch_DarinGroup_Field_Must_Have_No_Title_Map()
        {
            // Arrange
            var exceptionThrow = false;
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Tekt_Darin_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Tekt",
                            Value = "Tekt"
                        }
                    }
                }
            };
            var fieldName = "Darin";
            var level = "Level 3";
            var elementName = "DARIN";


            try
            {
                // Act
               MappingFunctions.MapGroupFieldLevelDependent(fields, level, fieldName, elementName);
            }
            catch (Exception e)
            {
                // Assert
                e.GetType().Should().Be(typeof(ArgumentException));
                e.Message.Should().Be("Level is not supported: Level 3");
                exceptionThrow = true;
            }

            // Assert
            exceptionThrow.Should().BeTrue();
        }


        #endregion

        [Test]
        public void Ve_With_Level_Vor_Map_Umfang_Field_Must_Have_ElementName_Umfang_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Vz_Umfang",
                    Value = "8,5 Meter"
                }
            };

            var fieldName = "Vz_Umfang";
            var elementName = "UMFANG";

            // Act
            var result = MappingFunctions.MapGroupField(fields, fieldName, elementName);
            // Assert
            result.ElementName.Should().Be(elementName);
            result.ElementValue[0].TextValues[0].Value.Should().Be("8,5 Meter");

        }

        [Test]
        public void Ve_With_Level_Vor_Map_Umfang_GP_Field_Must_Have_ElementName_Umfang_and_Right_Value()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Vz_Umfang_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "1",
                            Value = "12 Laufmeter"
                        },
                        new()
                        {
                            Type = "2",
                            Value = "17 Leitz Ordner"
                        }
                    }

                }
            };

            var fieldName = "Vz_Umfang";
            var elementName = "UMFANG";

            // Act
            var result = MappingFunctions.MapGroupField(fields, fieldName, elementName);
            // Assert
            result.ElementName.Should().Be(elementName);
            result.ElementValue[0].TextValues[0].Value.Should().Be("12 Laufmeter");
            result.ElementValue[1].TextValues[0].Value.Should().Be("17 Leitz Ordner");

        }

        [Test]
        public void Ve_With_Form_GP_Field_Must_Have_ElementName_Form_and_Correct_Value()
        {
            // Arrange
            var fields = new List<DocumentField>
            {
                new()
                {
                    Type = "Form_Gp",
                    Fields = new List<DocumentField>
                    {
                        new()
                        {
                            Type = "Form",
                            Value = "Video"
                        },
                        new()
                        {
                            Type = "Form",
                            Value = "Mikrofilm"
                        }
                    }

                }
            };

            var fieldName = "Form";
            var elementName = "FORM";

            // Act
            var result = MappingFunctions.MapGroupField(fields, fieldName, elementName);
            // Assert
            result.ElementName.Should().Be(elementName);
            result.ElementValue[0].TextValues[0].Value.Should().Be("Video");
            result.ElementValue[1].TextValues[0].Value.Should().Be("Mikrofilm");

        }

        [Test]
        public void FormatLaufzeitText_With_Empty_or_Null_string_returns_empty_string()
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();

            // Act
            var result = MappingFunctions.FormatLaufzeitText("");
            var result2 = MappingFunctions.FormatLaufzeitText(null);

            // Assert
            result.Should().Be(string.Empty);
            result2.Should().Be(string.Empty);
        }

        [Test]
        [TestCase("1234")]
        [TestCase("19000101-19000201")]
        [TestCase("abc-def")]
        [TestCase("abc def")]
        [TestCase("abcd0101 defg0101")]
        public void FormatLaufzeitText_with_an_invalid_text_should_raise_an_error(string laufzeitText)
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();

            // Act
            var action = (Action) (() => { MappingFunctions.FormatLaufzeitText(laufzeitText); });

            // assert
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        [TestCase("19500101 19600101")]
        public void FormatLaufzeitText_with_data_ranges_including_more_than_one_year_should_return_correct_data(string laufzeitText)
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();

            // Act
            var result = MappingFunctions.FormatLaufzeitText(laufzeitText);

            // assert
            result.Should().Be("1950-1960");
        }

        [Test]
        [TestCase("19500101 19500101")]
        public void FormatLaufzeitText_with_data_ranges_including_only_one_year_should_return_correct_data(string laufzeitText)
        {
            // Arrange
            var mapper = new ArchiveRecordMapper();

            // Act
            var result = MappingFunctions.FormatLaufzeitText(laufzeitText);

            // assert
            result.Should().Be("1950");
        }

        [Test]
        public void Test_if_extracted_security_attributes_are_correct()
        { 
            // Arrange
            var mapper = new ArchiveRecordMapper();
            var dataElementFileVz2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ActaPro\\Data",
                "Vz      685f0872-f984-4f66-88a4-e6cd873fb1d0.json");
            var settings = new JsonSerializerSettings
            {
                DateFormatString = "dd.MM.yyyy HH:mm:ss:fff"
            };
            var documentVz1 = JsonConvert.DeserializeObject<Document>(File.ReadAllText(dataElementFileVz2), settings);

            // ACT
            var result = mapper.ExtractSecurityRelevantAttributes(documentVz1);

            // Assert
            result.Stufe.Should().Be("Dossier");
            result.SchutzfristEnde.Should().Be(new DateTime(2020, 1, 1, 23, 59, 59));
            result.EntstehungszeitraumBis.Should().Be(new DateTime(1990, 12, 31, 00, 00, 00));
            result.BearbeitungsStatus.Should().Be(ActaProClientValues.StatusInBearbeitung);
            result.Zugaenglichkeit.Should().Be(ActaProClientValues.ZugaenglichkeitNichtOeffentlich);
            result.MetadatenPublizierbar.Should().BeTrue();
            result.Schutzfristkategorie.Should().Be(ActaProClientValues.SchutzfristKategorieArt91);
            result.ZugaenglichkeitGemaessBga.Should().Be(ActaProClientValues.ZugaenglichkeitBGAPruefungNoetig);
            result.Publikationsrechte.Should().Be(ActaProClientValues.PublikationsrechteBAR);
            result.ZustaendigeStellenKeys.Count.Should().Be(2);
        }
    }
}
