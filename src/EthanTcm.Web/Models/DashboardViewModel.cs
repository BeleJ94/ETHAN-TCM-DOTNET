using EthanTcm.Application.Abstractions;

namespace EthanTcm.Web.Models;

public sealed class DashboardViewModel
{
    public DashboardDto Dashboard { get; init; } = default!;
}
