using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Simulation.Domain.Templates;
using Simulation.Persistence.Configurations;

namespace Simulation.Persistence;

using Microsoft.EntityFrameworkCore;

public class SimulationDbContext(DbContextOptions<SimulationDbContext> options) : DbContext(options)
{
    public DbSet<PlayerData> PlayerTemplates { get; set; }
    public DbSet<MapData> MapTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        base.OnModelCreating(modelBuilder);
    }
}