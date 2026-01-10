using Microsoft.EntityFrameworkCore;

public class TestServerDbContext : DbContext
{
    public TestServerDbContext(DbContextOptions<TestServerDbContext> options) : base(options) { }

    // This represents your table in SQLite
    public DbSet<DataItem> DataItems => Set<DataItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set the Key property as the primary key for the database
        modelBuilder.Entity<DataItem>().HasKey(d => d.Key);
    }
}

