using EthanTcm.Application.Abstractions;
using EthanTcm.Domain.Entities;

namespace EthanTcm.Tests;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void Audit_log_action_filter_does_not_collide_with_mvc_action_route_value()
    {
        var modelType = typeof(EthanTcm.Web.Models.AuditLogIndexViewModel);

        Assert.Null(modelType.GetProperty("Action"));
        Assert.NotNull(modelType.GetProperty("ActionFilter"));
    }

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
