using Microsoft.EntityFrameworkCore;
using LakeHub.Models;

namespace LakeHub
{
    public abstract class LakeHubContext : DbContext
    {
        public LakeHubContext(DbContextOptions ctx) : base(ctx)
        { }
        //https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types
        public DbSet<DbUser> User { get; set; } = null!;
    }

    public class SqlServerDbContext : LakeHubContext
    {
        public SqlServerDbContext(DbContextOptions<SqlServerDbContext> ctx) : base(ctx)
        {
        }
    }

    public class PostgreSqlDbContext : LakeHubContext
    {
        public PostgreSqlDbContext(DbContextOptions<PostgreSqlDbContext> ctx) : base(ctx)
        {
        }
    }
}
