using System.Collections.Generic;
using System.IO;
using System.Text;
using CMI.Access.Repository;
using DotCMIS.Data.Extensions;
using Shouldly;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CMI.Manager.Repository.Tests
{
    [TestFixture]
    public class MetadataAccessTests
    {
        [Test]
        public void Get_simple_matadata_property_must_return_correct_value()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dateiTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetExtendedPropertyValue(data, "Fixity Value");

            // Assert
            value.ShouldBe("83ede739c7a1560b56b18d21db72b2fa");
        }

        [Test]
        public void Get_nested_matadata_property_must_return_correct_value()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dateiTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetExtendedPropertyValue(data, "ARELDA:datei/datei/originalName");

            // Assert
            value.ShouldBe("README.txt");
        }

        [Test]
        public void Get_nested_matadata_property_with_attribute_must_return_correct_value()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dateiTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetExtendedPropertyValue(data, "ARELDA:datei/datei@id");

            // Assert
            value.ShouldBe("_9ddrsOngEeW0aqy2QDXP4A");
        }


        [Test]
        public void Get_inexisting_simple_matadata_property_returns_null_or_empty()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dateiTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetExtendedPropertyValue(data, "Inexisting property");

            // Assert
            value.ShouldBeNullOrEmpty();
        }

        [Test]
        public void Get_inexisting_nested_matadata_property_with_attribute_returns_null_or_empty()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dateiTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetExtendedPropertyValue(data, "ARELDA:datei/datei@InexistingAttribute");

            // Assert
            value.ShouldBeNullOrEmpty();
        }

        [Test]
        public void Get_inexisting_nested_matadata_property_returns_null_or_empty()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dateiTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetExtendedPropertyValue(data, "ARELDA:datei/datei/Inexisting property");

            // Assert
            value.ShouldBeNullOrEmpty();
        }

        [Test]
        public void Casing_of_property_path_is_irrelevant_to_result()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dateiTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetExtendedPropertyValue(data, "Arelda:datei/datei/originalName");
            var value2 = sut.GetExtendedPropertyValue(data, "ArElDa:daTei/dAtei/ORIGINALNAME");

            // Assert
            value.ShouldBe("README.txt");
            value.ShouldBe(value2);
        }


        [Test]
        public void Get_nested_matadata_collection_property_must_return_correct_list()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dossierTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetExtendedPropertyValues(data, "ARELDA:dossier/dossier/dateiRef");

            // Assert
            value.Count.ShouldBe(2);
            value[0].ShouldBe("p00000003");
            value[1].ShouldBe("p00000004");
        }

        [Test]
        public void Historischer_zeitpunkt_from_inexisting_element_is_null()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dossierTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetHistorischerZeitpunkt(data, "ARELDA:Dossier/Dossier/Eroeffnungsdatum");

            // Assert
            value.ShouldBeNull();
        }


        [Test]
        public void Historischer_zeitpunkt_from_element_returns_value()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dossierTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetHistorischerZeitpunkt(data, "ARELDA:Dossier/Dossier/Entstehungszeitraum/von");

            // Assert
            value.Datum.ShouldBe("2009-03-18");
            value.Ca.ShouldBeFalse();
        }

        [Test]
        public void Historischer_Zeitraum_from_element_returns_value()
        {
            // Arrange
            var sut = new MetadataDataAccess();
            var jsonText = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "dossierTestData.json"), Encoding.UTF8);
            var data = new List<ICmisExtensionElement>
            {
                JsonConvert.DeserializeObject<CmisExtensionElement>(jsonText, new JsonSerializerSettings
                {
                    Converters = {new ExtensionElementConverter()},
                    TypeNameHandling = TypeNameHandling.Auto
                })
            };

            // Act
            var value = sut.GetHistorischerZeitraum(data, "ARELDA:Dossier/Dossier/Entstehungszeitraum");

            // Assert
            value.Von.Datum.ShouldBe("2009-03-18");
            value.Von.Ca.ShouldBeFalse();
            value.Bis.Datum.ShouldBe("2009-03-18");
            value.Bis.Ca.ShouldBeTrue();
        }
    }
}