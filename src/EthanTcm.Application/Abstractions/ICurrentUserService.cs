namespace EthanTcm.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Login { get; }
    string? DisplayName { get; }
    string? Email { get; }
    Guid? DepartmentId { get; }
    bool IsAuthenticated { get; }
    bool IsActive { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
}
