using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using NUnit.Framework;

namespace CMI.Access.Common.Tests;

[Ignore("Diese Tests müssen bewusst bzw. Bedarf ausgeführt werden")]
[TestFixture]
public class ElasticSearchIndexDataAccessTests
{
    [SetUp]
    public void SetUp()
    {
        var uri = "(change here, but do not commit)";
        string username = "(change here, but do not commit)";
        string pwd = "(change here, but do not commit)";
        helper = new SearchIndexDataAccess(new Uri(uri), username, pwd);
    }

    private SearchIndexDataAccess helper;

    [Test]
    public async Task TestGetChildrenOfTopNodeUsingScopeId()
    {
        var result = await helper.GetChildren("someArchiveRecordId", "1", false);

        result.ShouldNotBeNull();
        result.Count().ShouldBe(7);
    }

    [Test]
    public async Task TestGetChildrenOfTopNodeUsingDocKey()
    {
        var result = await helper.GetChildren("Tekt    857197dc-4b61-4aca-b670-c24bac3f1933", "_", false);

        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
    }

    [Test]
    public async Task TestGetChildrenOfAllLevels()
    {
        var result = await helper.GetChildren("Tekt    857197dc-4b61-4aca-b670-c24bac3f1933", "_", true);

        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
    }

}