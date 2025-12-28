using Microsoft.EntityFrameworkCore;

namespace SAT242516026.Models.DbContexts;

public class MyDbModel_Context : DbContext
{
    public MyDbModel_Context(DbContextOptions<MyDbModel_Context> options)
        : base(options) { }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      
    }
}
