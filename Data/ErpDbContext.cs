using GranitWebApi.Models.NETSISMODELLER;
using Microsoft.EntityFrameworkCore;

public class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options): base(options)
    {
    }
    public DbSet<TBLSTSABIT> TBLSTSABIT { get; set; }
}