using System;
using System.Collections.Generic;
using CMI.Access.Common;
using CMI.Contract.Common;
using CMI.Engine.Anonymization;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Index.Tests;

[TestFixture]
public class AnonymizationReferenceEngineTests
{
    private readonly ElasticArchiveDbRecord id1 = new()
    {
        ArchiveRecordId = "1",
        ReferenceCode = "Signatur1",
        Title = "Ein ███ Titel1",
        Level = "Dossier",
        CreationPeriod = new ElasticTimePeriod {Text = "1990-1995"},
        UnanonymizedFields = new UnanonymizedFields
        {
            Title = "Ein anonymisierter Titel1",
            ParentContentInfos = new List<ElasticParentContentInfo>
            {
                new() {Title = "Archivname"},
                new() {Title = "Bestandname"},
                new() {Title = "Ein anonymisierter Titel1"},
            }
        },
        References = new List<ElasticReference>
        {
            new() {ArchiveRecordId = "2", ReferenceName = "A first test"},
            new() {ArchiveRecordId = "3", ReferenceName = "A second test"}
        },
        ArchiveplanContext = new List<ElasticArchiveplanContextItem>
        {
            new() {ArchiveRecordId = "1000", Title = "Archivname", Level = "Archiv"},
            new() {ArchiveRecordId = "1001", Title = "Bestandname", Level = "Bestand"},
            new() {ArchiveRecordId = "1", Title = "Ein ███ Titel1", Level = "Dossier"},
        },
        ParentContentInfos = new List<ElasticParentContentInfo>
        {
            new() {Title = "Archivname"},
            new() {Title = "Bestandname"},
            new() {Title = "Ein ███ Titel1"},
        }
    };

    private readonly ElasticArchiveDbRecord id2 = new()
    {
        ArchiveRecordId = "2",
        ReferenceCode = "Signatur2",
        Title = "Ein ███ Titel2",
        Level = "Subdossier",
        CreationPeriod = new ElasticTimePeriod { Text = "1995-1998" },
        UnanonymizedFields = new UnanonymizedFields
        {
            Title = "Ein anonymisierter Titel2",
            ParentContentInfos = new List<ElasticParentContentInfo>
            {
                new() {Title = "Archivname"},
                new() {Title = "Bestandname"},
                new() {Title = "Ein anonymisierter Titel1"},
                new() {Title = "Ein anonymisierter Titel2"},
            }
        },
        References = new List<ElasticReference>
        {
            new() {ArchiveRecordId = "1", ReferenceName = "Old reference name"}
        },
        ArchiveplanContext = new List<ElasticArchiveplanContextItem>
        {
            new(){ArchiveRecordId = "1000", Title = "Archivname", Level = "Archiv"},
            new(){ArchiveRecordId = "1001", Title = "Bestandname", Level = "Bestand"},
            new(){ArchiveRecordId = "1", Title = "Ein alter ███ Titel1", Level = "Dossier"},
            new(){ArchiveRecordId = "2", Title = "Ein ███ Titel2", Level = "Subdossier"},
        },
        ParentContentInfos = new List<ElasticParentContentInfo>
        {
            new() {Title = "Archivname"},
            new() {Title = "Bestandname"},
            new() {Title = "Ein alter ███ Titel1"},
            new() {Title = "Ein ███ Titel2"},
        }
    };

    private readonly ElasticArchiveDbRecord id3 = new()
    {
        ArchiveRecordId = "3",
        ReferenceCode = "Signatur3",
        Title = "Ein ███ Titel3",
        Level = "Subdossier",
        CreationPeriod = new ElasticTimePeriod { Text = "1999-2000" },
        UnanonymizedFields = new UnanonymizedFields
        {
            Title = "Ein anonymisierter Titel3",
            ParentContentInfos = new List<ElasticParentContentInfo>
            {
                new() {Title = "Archivname"},
                new() {Title = "Bestandname"},
                new() {Title = "Ein anonymisierter Titel1"},
                new() {Title = "Ein anonymisierter Titel3"},
            }
        },
        References = new List<ElasticReference>
        {
            new() {ArchiveRecordId = "1", ReferenceName = "Old reference name"}
        },
        ArchiveplanContext = new List<ElasticArchiveplanContextItem>
        {
            new(){ArchiveRecordId = "1000", Title = "Archivname", Level = "Archiv"},
            new(){ArchiveRecordId = "1001", Title = "Bestandname", Level = "Bestand"},
            new(){ArchiveRecordId = "1", Title = "Ein alter ███ Titel1", Level = "Dossier"},
            new(){ArchiveRecordId = "3", Title = "Ein ███ Titel3", Level = "Subdossier"},
        },
        ParentContentInfos = new List<ElasticParentContentInfo>
        {
            new() {Title = "Archivname"},
            new() {Title = "Bestandname"},
            new() {Title = "Ein alter ███ Titel1"},
            new() {Title = "Ein ███ Titel3"},
        }
    };

    [Test]
    public void Test_if_dependent_records_are_updated()
    {
        // Arrange
        var dbAccess = new Mock<ISearchIndexDataAccess>();
        var engine = new AnonymizationReferenceEngine(dbAccess.Object);
        var updateRecordList = new List<ElasticArchiveRecord>();
        var updateAction = new Action<ElasticArchiveRecord>(record => updateRecordList.Add(record));

        var elasticRecord = id1;

        dbAccess.Setup(f => f.FindDbDocument("2", It.IsAny<bool>())).Returns(id2);
        dbAccess.Setup(f => f.FindDbDocument("3", It.IsAny<bool>())).Returns(id3);
        dbAccess.Setup(f => f.GetChildren("1", true)).Returns(() => new List<ElasticArchiveRecord> { id2, id3 });
        dbAccess.Setup(f => f.UpdateDocument(It.IsAny<ElasticArchiveDbRecord>())).Callback(updateAction);

        // Act
        engine.UpdateDependentRecords(elasticRecord);

        // Assert

        // There should be 4 updates. Two for the two references, and two updated child records
        updateRecordList.Count.Should().Be(4);

        // The first updates are the children
        // The title of the updated children must match the current title attribute
        updateRecordList[0].ArchiveRecordId.Should().Be("2");
        updateRecordList[0].ArchiveplanContext.Find(i => i.ArchiveRecordId == "1").Title.Should().Be(id1.Title);
        updateRecordList[0].ParentContentInfos[2].Title.Should().Be(id1.Title);
        updateRecordList[1].ArchiveRecordId.Should().Be("3");
        updateRecordList[1].ArchiveplanContext.Find(i => i.ArchiveRecordId == "1").Title.Should().Be(id1.Title);
        updateRecordList[1].ParentContentInfos[2].Title.Should().Be(id1.Title);

        // The last two updates are the references
        updateRecordList[2].ArchiveRecordId.Should().Be("2");
        updateRecordList[2].References.Find(i => i.ArchiveRecordId == "1").ReferenceName.Should().Be("Signatur1 Ein ███ Titel1, 1990-1995 (Dossier)");
        updateRecordList[3].ArchiveRecordId.Should().Be("3");
        updateRecordList[3].References.Find(i => i.ArchiveRecordId == "1").ReferenceName.Should().Be("Signatur1 Ein ███ Titel1, 1990-1995 (Dossier)");
    }
}