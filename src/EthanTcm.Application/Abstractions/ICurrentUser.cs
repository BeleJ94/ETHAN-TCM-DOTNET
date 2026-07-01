namespace EthanTcm.Application.Abstractions;

public interface ICurrentUser
{
    string? Login { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
}
