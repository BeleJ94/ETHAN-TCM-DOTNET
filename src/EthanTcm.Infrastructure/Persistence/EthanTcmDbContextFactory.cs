using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EthanTcm.Infrastructure.Persistence;

public sealed class EthanTcmDbContextFactory : IDesignTimeDbContextFactory<EthanTcmDbContext>
{
    public EthanTcmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EthanTcmDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=EthanTcmDatabase;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True",
            sql =>
            {
                sql.MigrationsAssembly(typeof(EthanTcmDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure();
            });

        return new EthanTcmDbContext(optionsBuilder.Options);
    }
}
