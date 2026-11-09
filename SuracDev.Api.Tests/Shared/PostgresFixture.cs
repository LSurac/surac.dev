using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SuracDev.Api.Data;
using Testcontainers.PostgreSql;

namespace SuracDev.Api.Tests.Shared;

/// <summary>
/// A real PostgreSQL for the integration tests, the image production runs. One container per test
/// run; the migrations go once into a template database and every test copies it, so tests share no
/// rows and a fresh database costs milliseconds.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
  private const string TemplateDatabase = "suracdev_template";

  // One database per test, each with its own Npgsql pool that stays open until the run ends; the
  // default of 100 connections ran out once the usage-event tests joined (2026-11-09).
  private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
    .WithCommand("-c", "max_connections=400")
    .Build();

  public async Task InitializeAsync()
  {
    if (!PostgresFactAttribute.CanRun)
    {
      return;
    }

    await container.StartAsync();
    await CreateDatabaseAsync(TemplateDatabase, template: null);

    await using (var dbContext = new SuracDevDbContext(OptionsFor(TemplateDatabase)))
    {
      await dbContext.Database.MigrateAsync();
    }

    // Postgres refuses to copy a template that still has connections.
    NpgsqlConnection.ClearAllPools();
  }

  public Task DisposeAsync() => container.DisposeAsync().AsTask();

  /// <summary>Connection string of a freshly migrated, empty database of its own.</summary>
  public async Task<string> CreateDatabaseAsync()
  {
    var name = $"test_{Guid.NewGuid():N}";
    await CreateDatabaseAsync(name, TemplateDatabase);
    return new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = name }.ConnectionString;
  }

  private async Task CreateDatabaseAsync(string name, string? template)
  {
    await using var connection = new NpgsqlConnection(container.GetConnectionString());
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = template is null ? $"CREATE DATABASE {name}" : $"CREATE DATABASE {name} TEMPLATE {template}";
    await command.ExecuteNonQueryAsync();
  }

  private DbContextOptions<SuracDevDbContext> OptionsFor(string database)
  {
    var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
    {
      Database = database,
    }.ConnectionString;
    return new DbContextOptionsBuilder<SuracDevDbContext>().UseNpgsql(connectionString).Options;
  }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
  public const string Name = "Postgres";
}

/// <summary>The data-driven variant of <see cref="PostgresFactAttribute"/>, skipped under the same rule.</summary>
public sealed class PostgresTheoryAttribute : TheoryAttribute
{
  public PostgresTheoryAttribute()
  {
    if (!PostgresFactAttribute.CanRun)
    {
      Skip = "Needs a running Docker for its PostgreSQL container.";
    }
  }
}

/// <summary>
/// A test that needs <see cref="PostgresFixture"/>. On a workstation without a running Docker it is
/// skipped so the gate stays usable there; in GitHub Actions it never is, so it cannot quietly stop
/// running in the deploy gate.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
  private static readonly Lazy<bool> DockerIsRunning = new(IsDockerRunning);

  public static bool CanRun => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" || DockerIsRunning.Value;

  public PostgresFactAttribute()
  {
    if (!CanRun)
    {
      Skip = "Needs a running Docker for its PostgreSQL container.";
    }
  }

  private static bool IsDockerRunning()
  {
    try
    {
      using var docker = Process.Start(new ProcessStartInfo("docker", "info")
      {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
      });
      if (docker is null)
      {
        return false;
      }

      docker.OutputDataReceived += (_, _) => { };
      docker.ErrorDataReceived += (_, _) => { };
      docker.BeginOutputReadLine();
      docker.BeginErrorReadLine();
      return docker.WaitForExit(TimeSpan.FromSeconds(10)) && docker.ExitCode == 0;
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return false;
    }
  }
}
