using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Simulation.Domain;
using Simulation.Domain.Templates;

namespace Simulation.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<PlayerData>
{
    public void Configure(EntityTypeBuilder<PlayerData> builder)
    {
        // --- Configuração da Entidade PlayerTemplate ---
        builder.HasKey(p => p.CharId); // Define a chave primária
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);

        // Mapeia enums para serem armazenados como strings no banco de dados (mais legível)
        builder.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Vocation).HasConversion<string>().HasMaxLength(20);
    }
}