using System;
using CMI.Utilities.ActaPro;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class ActaProMappingProviderTests
{
    [Test]
    public void ActaProMappingProvider_Test_Read_ActaProId_Gets_ScopeId()
    {
        // ARRANGE
        var actaProMappingProvider = new ActaProMappingProvider();

        // ACT
        var result = actaProMappingProvider.GetScopeId("Arch    b83a8ece-92dc-506d-9baa-222222222222");

        // ASSERT
        result.Should().Be(1L);
    }

    [Test]
    public void ActaProMappingProvider_Test_Read_EmptySting_Get_Minus_1()
    {
        // ARRANGE
        var actaProMappingProvider = new ActaProMappingProvider();

        // ACT
        var result = actaProMappingProvider.GetScopeId(string.Empty);

        // ASSERT
        result.Should().Be(-1L);
    }

    [Test]
    public void ActaProMappingProvider_Test_Read_unknownID_Get_Minus_1()
    {
        // ARRANGE
        var actaProMappingProvider = new ActaProMappingProvider();

        // ACT
        var result = actaProMappingProvider.GetScopeId("XXX");

        // ASSERT
        result.Should().Be(-1L);
    }

    [Test]
    public void ActaProMappingProvider_Test_Read_TBest_Gets_ScopeId()
    {
        // ARRANGE
        var actaProMappingProvider = new ActaProMappingProvider();

        // ACT
        var result = actaProMappingProvider.GetScopeId("Vz      91cb05de-eaa6-5a7f-8793-3fa8f3d632d5");

        // ASSERT
        result.Should().Be(3244391L);
    }

    [Test]
    public void ActaProMappingProvider_Test_Read_ScopeId_Gets_ActaProId()
    {
        // ARRANGE
        var actaProMappingProvider = new ActaProMappingProvider();
        var scopeId = 3244391L;

        // ACT
        var result = actaProMappingProvider.GetActaProId(scopeId.ToString());

        // ASSERT
        result.Should().Be("Vz      91cb05de-eaa6-5a7f-8793-3fa8f3d632d5");
    }


    [Test]
    public void ActaProMappingProvider_Test_Read_not_valid_ScopeId_Gets_stringEmpty()
    {
        // ARRANGE
        var actaProMappingProvider = new ActaProMappingProvider();

        // ACT
        var result = actaProMappingProvider.GetActaProId("4b");

        // ASSERT
        result.Should().Be("");
    }
}