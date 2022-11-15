using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Engine.Anonymization;
using CMI.Manager.Index.Config;
using FluentAssertions;
using Microsoft.CSharp.RuntimeBinder;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
namespace CMI.Manager.Index.Tests
{
    [TestFixture]
    public class CustomFieldTests
    {
        [Test]
        public void Test_Convert_ConfigFile_To_Object()
        {
            // Arrange
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customFieldsConfig.json");
            var config = new CustomFieldsConfiguration(configFile);

            // Act

            // Assert
            config.Fields.Count.Should().BeGreaterThan(1);
        }


        [Test]
        public void Test_IndexManager_Should_Fill_CustomFields_Correctly()
        {
            // Arrange
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customFieldsConfig.json");
            var mockSearchIndexAccess = new Mock<ISearchIndexDataAccess>();
            var mockAnonymisationEngine = new Mock<IAnonymizationEngine>();
            var mockManuelleKorrekturAccess = new Mock<IManuelleKorrekturAccess>();
            var mockAnonymizationWithManuelleKorrekturEngine = new Mock<IAnonymizationReferenceEngine>();

            mockSearchIndexAccess.Setup(s => s
                    .UpdateDocument(It.IsAny<ElasticArchiveRecord>()))
                .Callback(() => Console.WriteLine("Update Document was called"));

            mockSearchIndexAccess.Setup(s => s
                    .RemoveDocument(It.IsAny<string>()))
                .Callback(() => Console.WriteLine("Remove Document was called"));

            var config = new CustomFieldsConfiguration(configFile);
            var indexmanager = new IndexManager(mockSearchIndexAccess.Object, config, mockAnonymisationEngine.Object, mockManuelleKorrekturAccess.Object, mockAnonymizationWithManuelleKorrekturEngine.Object);
            var archiveRecord = new ElasticArchiveRecord();

            var dataElementFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dataElements.json");
            var json = File.ReadAllText(dataElementFile);

            var dataElements = JsonConvert.DeserializeObject<List<DataElement>>(json);

            // Act
            indexmanager.TransferDataFromPropertyBag(archiveRecord, dataElements);

            // Assert
            Assert.IsNotNull(archiveRecord.CustomFields, "CustomField Property should be filled correctly");

            Assert.Throws<RuntimeBinderException>(() =>
                {
                    var test = archiveRecord.CustomFields.title;
                },
                "Title is not a custom field, so it should not be in the dynamic property");

            Assert.DoesNotThrow(() =>
                {
                    var test = archiveRecord.CustomFields.zugänglichkeitGemässBga;
                },
                "zugänglichkeitGemässBga is a custom field (with special chars), and so it should not throw on access");

            Assert.Throws<RuntimeBinderException>(() =>
                {
                    var testYear = archiveRecord.CustomFields.dummyElasticdatewithyear;
                },
                "DummyElasticDateWithYear is not available, as there is no data for it.");

            Assert.IsNotNull(archiveRecord.CustomFields.form);
            Assert.AreEqual("Fotografie", archiveRecord.CustomFields.form);

            Assert.IsAssignableFrom<List<ElasticHyperlink>>(archiveRecord.CustomFields.digitaleVersion);
            Assert.AreEqual("https://commons.wikimedia.org/wiki/File:Flugzeug_Grandjean_vor_dem_Aufstieg_-_CH-BAR_-_3236769.tif",
                archiveRecord.CustomFields.digitaleVersion[0].Url);
            Assert.AreEqual("E27#1000/721#14093#5489* (Wikimedia Commons)",
                archiveRecord.CustomFields.digitaleVersion[0].Text);
        }

        [Test]
        public void Test_CustomFields_Serialization_Working_Correct()
        {
            // Arrange
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customFieldsConfig.json");
            var mockSearchIndexAccess = new Mock<ISearchIndexDataAccess>();
            var mockAnonymizationEngine = new Mock<IAnonymizationEngine>();
            var mockManuelleKorrekturAccess = new Mock<IManuelleKorrekturAccess>();
            var mockAnonymizationWithManuelleKorrekturEngine = new Mock<IAnonymizationReferenceEngine>();

            mockSearchIndexAccess.Setup(s => s
                    .UpdateDocument(It.IsAny<ElasticArchiveRecord>()))
                .Callback(() => Console.WriteLine("Update Document was called"));

            mockSearchIndexAccess.Setup(s => s
                    .RemoveDocument(It.IsAny<string>()))
                .Callback(() => Console.WriteLine("Remove Document was called"));

            var config = new CustomFieldsConfiguration(configFile);
            var indexmanager = new IndexManager(mockSearchIndexAccess.Object, config, mockAnonymizationEngine.Object, mockManuelleKorrekturAccess.Object, mockAnonymizationWithManuelleKorrekturEngine.Object);
            var archiveRecord = new ElasticArchiveRecord();

            var dataElementFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dataElements.json");
            var json = File.ReadAllText(dataElementFile);
            var dataElements = JsonConvert.DeserializeObject<List<DataElement>>(json);

            // Act
            indexmanager.TransferDataFromPropertyBag(archiveRecord, dataElements);

            string jsonString = JsonConvert.SerializeObject(archiveRecord);
            var record = JsonConvert.DeserializeObject<ElasticArchiveRecord>(jsonString);

            // Assert
            Assert.IsNotNull(record);
            //  Beim deserialisieren via Elastic hat das dynamic Property ein ExpandoObject
            Assert.IsAssignableFrom<ExpandoObject>(record.CustomFields);

            // - nicht weiter schlimm, wir können es weiterhin als dynamic ansprechen
            Assert.DoesNotThrow(() =>
            {
                var test = record.CustomFields.form;
                Console.WriteLine(test);
            });

            // - und die properties auslesen
            var form = record.CustomFields.form;
            Assert.IsNotNull(form);
            Assert.AreEqual("Fotografie", form);

            Assert.IsAssignableFrom<List<dynamic>>(record.CustomFields.digitaleVersion);
            var hyperlink = record.CustomFields.digitaleVersion[0];

            Assert.AreEqual("https://commons.wikimedia.org/wiki/File:Flugzeug_Grandjean_vor_dem_Aufstieg_-_CH-BAR_-_3236769.tif",
                hyperlink.Url);
            Assert.AreEqual("E27#1000/721#14093#5489* (Wikimedia Commons)",
                hyperlink.Text);
        }

        [Test]
        public void Inexisting_ConfigFile_Raises_Exception()
        {
            // Arrange

            var action = new Action(() =>
            {
                var config = new CustomFieldsConfiguration("wewillnotfindthis.json");
            });

            // Act

            // Assert
            action.Should().Throw<FileNotFoundException>();
        }
    }
}