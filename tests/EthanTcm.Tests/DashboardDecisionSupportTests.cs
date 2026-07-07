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
        var lateDetails = await service.GetMetricDetailsAsync(DashboardView.ManagementOverview, DashboardMetric.Late, 1, 10);

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
        Assert.NotEmpty(dashboard.Charts.DueDateBuckets);
        Assert.NotEmpty(dashboard.Charts.StatusDistribution);
        Assert.NotEmpty(dashboard.Charts.RiskDistribution);
        Assert.All(dashboard.Charts.DueDateBuckets, point =>
            Assert.InRange(point.Percent, 0m, 100m));
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

    [Fact]
    public async Task Dashboard_views_apply_operational_filters()
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

        var late = await service.GetAsync(DashboardView.LateItems);
        var supervision = await service.GetAsync(DashboardView.ManagementOverview);
        var team = await service.GetAsync(DashboardView.TeamTasks);

        Assert.Equal(Math.Min(late.Metrics.Late, 75), late.Items.Count);
        Assert.All(late.Items, item =>
        {
            Assert.True(item.DaysLate > 0);
            Assert.Equal("En retard", item.Issue);
        });
        Assert.Equal(Math.Min(team.Metrics.OpenDeclarations, 75), team.Items.Count);
        Assert.Equal(Math.Min(supervision.Metrics.OpenDeclarations, 75), supervision.Items.Count);
    }

    [Fact]
    public async Task Dashboard_kpi_details_use_the_active_view_scope()
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

        var myTasks = await service.GetAsync(DashboardView.MyTasks);
        var myTaskLateDetails = await service.GetMetricDetailsAsync(DashboardView.MyTasks, DashboardMetric.Late, 1, 50);
        var supervisionOwnerlessDetails = await service.GetMetricDetailsAsync(DashboardView.ManagementOverview, DashboardMetric.ObligationsWithoutResponsible, 1, 50);
        var myTaskOwnerlessDetails = await service.GetMetricDetailsAsync(DashboardView.MyTasks, DashboardMetric.ObligationsWithoutResponsible, 1, 50);

        Assert.Equal(myTasks.Metrics.Late, myTaskLateDetails.TotalCount);
        Assert.Equal(DashboardView.MyTasks, myTaskLateDetails.View);
        Assert.Equal(DashboardView.ManagementOverview, supervisionOwnerlessDetails.View);
        Assert.True(supervisionOwnerlessDetails.TotalCount >= myTaskOwnerlessDetails.TotalCount);
        Assert.Equal(0, myTaskOwnerlessDetails.TotalCount);
    }

    [Fact]
    public async Task Dashboard_kpi_details_can_be_exported_to_excel_and_pdf()
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

        var excel = await service.ExportMetricDetailsAsync(DashboardView.ManagementOverview, DashboardMetric.Late, DashboardExportFormat.Excel);
        var pdf = await service.ExportMetricDetailsAsync(DashboardView.ManagementOverview, DashboardMetric.Late, DashboardExportFormat.Pdf);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excel.ContentType);
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.EndsWith(".xlsx", excel.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".pdf", pdf.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.True(excel.Content.Length > 256);
        Assert.StartsWith("PK", System.Text.Encoding.ASCII.GetString(excel.Content, 0, 2));
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Content, 0, 4));
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
