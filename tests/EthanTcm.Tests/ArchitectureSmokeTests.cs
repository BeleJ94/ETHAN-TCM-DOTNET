using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Entities;

namespace EthanTcm.Tests;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void Domain_entities_can_be_created_without_infrastructure_dependencies()
    {
        var entity = new LegalEntity("ETHAN", "ETHAN TCM Demo Entity", "MA", "TAX-001");

        Assert.Equal("ETHAN", entity.Code);
    }

    [Fact]
    public void Application_layer_exposes_use_case_contracts()
    {
        Assert.True(typeof(ITaxMatrixImporter).IsInterface);
    }
}
