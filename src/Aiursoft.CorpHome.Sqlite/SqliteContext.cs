using System.Diagnostics.CodeAnalysis;
using Aiursoft.CorpHome.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.CorpHome.Sqlite;

[ExcludeFromCodeCoverage]

public class SqliteContext(DbContextOptions<SqliteContext> options) : TemplateDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
