using EthanTcm.Application.Abstractions;
using EthanTcm.Application.Authentication;
using EthanTcm.Domain.Enums;
using EthanTcm.Infrastructure.Persistence;
using EthanTcm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EthanTcm.Tests;

public sealed class DashboardDecisionSupportTests
{
    [Fact]
    public async Task Dashboard_metrics_are_exact_and_kpi_details_are_server_paginated()
    {
        var options = new DbContextOptionsBuilder<EthanTcmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var currentUser = new TestCurrentUserService();

        await using var context = new EthanTcmDbContext(options);
        var seedResult = await new InitialTaxObligationSeeder(context, currentUser).SeedAsync();
        Assert.Empty(seedResult.Errors);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 64 });
        var service = new DashboardService(context, currentUser, cache);

        var dashboard = await service.GetAsync(DashboardView.ManagementOverview);
        var lateDetails = await service.GetMetricDetailsAsync(DashboardMetric.Late, 1, 10);

        var expectedOpenCount = await context.TaxDeclarations.CountAsync(declaration =>
            declaration.Status != TaxDeclarationStatus.Closed &&
            declaration.Status != TaxDeclarationStatus.Cancelled &&
            declaration.Status != TaxDeclarationStatus.NotApplicable);
        Assert.Equal(expectedOpenCount, dashboard.Metrics.OpenDeclarations);
        Assert.Equal(dashboard.Metrics.Late, lateDetails.TotalCount);
        Assert.InRange(lateDetails.Items.Count, 0, 10);
        Assert.All(lateDetails.Items, item =>
            Assert.Equal(DashboardDetailTargetType.TaxDeclaration, item.TargetType));
        Assert.InRange(dashboard.Metrics.EvidenceComplianceRate, 0m, 100m);
    }

    [Fact]
    public async Task Dashboard_cache_is_partitioned_and_reuses_the_same_snapshot()
    {
        var options = new DbContextOptionsBuilder<EthanTcmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var currentUser = new TestCurrentUserService();

        await using var context = new EthanTcmDbContext(options);
        var seedResult = await new InitialTaxObligationSeeder(context, currentUser).SeedAsync();
        Assert.Empty(seedResult.Errors);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 64 });
        var service = new DashboardService(context, currentUser, cache);

        var first = await service.GetAsync(DashboardView.MyTasks);
        var second = await service.GetAsync(DashboardView.MyTasks);

        Assert.Same(first, second);
        Assert.Equal(first.GeneratedAt, second.GeneratedAt);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public string? Login => "dashboard.manager";
        public string? DisplayName => "Dashboard Manager";
        public string? Email => "dashboard.manager@local";
        public Guid? DepartmentId => null;
        public bool IsAuthenticated => true;
        public bool IsActive => true;
        public IReadOnlyCollection<string> Roles => [ApplicationRoles.Administrator];
        public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
