using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SuracDev.Api.Data;

/// <summary>
/// Design-time factory so the EF Core tooling can build the context without the running host.
/// Uses <c>ConnectionStrings__Default</c> when present, otherwise the local development container
/// from docs/build-and-test.md. Building the model for a migration opens no connection.
/// Only `dotnet ef` instantiates it, hence excluded from coverage.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Invoked by the EF Core tooling only.")]
public sealed class SuracDevDbContextFactory : IDesignTimeDbContextFactory<SuracDevDbContext>
{
  public SuracDevDbContext CreateDbContext(string[] args)
  {
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
      ?? "Host=localhost;Port=5432;Database=suracdev;Username=suracdev;Password=suracdev";

    var options = new DbContextOptionsBuilder<SuracDevDbContext>()
      .UseNpgsql(connectionString)
      .Options;

    return new SuracDevDbContext(options);
  }
}
