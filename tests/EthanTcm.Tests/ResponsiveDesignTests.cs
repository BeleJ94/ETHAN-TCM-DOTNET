namespace EthanTcm.Tests;

public sealed class ResponsiveDesignTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Layout_declares_mobile_viewport_and_collapsible_navigation()
    {
        var layout = Read("src", "EthanTcm.Web", "Views", "Shared", "_Layout.cshtml");

        Assert.Contains("width=device-width, initial-scale=1.0", layout, StringComparison.Ordinal);
        Assert.Contains("navbar-expand-lg", layout, StringComparison.Ordinal);
        Assert.Contains("navbar-toggler", layout, StringComparison.Ordinal);
        Assert.Contains("mobile-bottom-nav", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_operational_table_has_a_responsive_container()
    {
        var viewsRoot = Path.Combine(RepositoryRoot, "src", "EthanTcm.Web", "Views");
        var offenders = Directory.EnumerateFiles(viewsRoot, "*.cshtml", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("<table", StringComparison.OrdinalIgnoreCase))
            .Where(path => !File.ReadAllText(path).Contains("table-responsive", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(RepositoryRoot, path))
            .ToArray();

        Assert.True(offenders.Length == 0, $"Tables without responsive container: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Mobile_styles_cover_cards_touch_targets_modals_and_safe_areas()
    {
        var css = Read("src", "EthanTcm.Web", "wwwroot", "css", "site.css");

        Assert.Contains(".mobile-card-table", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem", css, StringComparison.Ordinal);
        Assert.Contains("env(safe-area-inset-bottom)", css, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 359.98px)", css, StringComparison.Ordinal);
        Assert.Contains("@media (hover: none) and (pointer: coarse)", css, StringComparison.Ordinal);
        Assert.Contains(".modal-dialog.modal-xl", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Responsive_table_enhancement_supports_initial_and_ajax_content()
    {
        var script = Read("src", "EthanTcm.Web", "wwwroot", "js", "site.js");

        Assert.Contains("enhanceResponsiveTable", script, StringComparison.Ordinal);
        Assert.Contains("cell.dataset.label = label", script, StringComparison.Ordinal);
        Assert.Contains("window.ethanTcmInitialize = initialize", script, StringComparison.Ordinal);
        Assert.Contains("window.ethanTcmInitialize?.(modalBody)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Declaration_actions_refresh_details_without_full_page_navigation()
    {
        var script = Read("src", "EthanTcm.Web", "wwwroot", "js", "site.js");

        Assert.Contains("await refreshDeclarationDetails(payload.refreshUrl)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("window.location.assign(payload.refreshUrl)", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Correspondence_action_kpis_and_filters_have_mobile_layouts()
    {
        var view = Read("src", "EthanTcm.Web", "Views", "CorrespondenceActions", "Index.cshtml");

        Assert.Contains("col-6 col-md-4 col-xl-2", view, StringComparison.Ordinal);
        Assert.Contains("mobile-filter-form", view, StringComparison.Ordinal);
    }

    private static string Read(params string[] segments)
        => File.ReadAllText(Path.Combine([RepositoryRoot, .. segments]));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EthanTcm.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
