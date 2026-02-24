using System.Diagnostics.CodeAnalysis;
using Aiursoft.CorpHome.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.CorpHome.MySql;

[ExcludeFromCodeCoverage]

public class MySqlContext(DbContextOptions<MySqlContext> options) : TemplateDbContext(options);
