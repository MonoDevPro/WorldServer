using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Simulation.Domain.Templates;

namespace Simulation.Persistence.Configurations;

public class MapConfiguration : IEntityTypeConfiguration<MapData>
{
    public void Configure(EntityTypeBuilder<MapData> builder)
    {
        builder.HasKey(m => m.MapId);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(150);

        // Converte o array de TileType[] para uma string separada por vírgulas
        var tileConverter = new ValueConverter<TileType[]?, string>(
            v => v != null ? string.Join(",", v.Select(e => (byte)e)) : string.Empty,
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => (TileType)byte.Parse(s)).ToArray()
        );

        // Converte o array de byte[] para uma string separada por vírgulas
        var byteConverter = new ValueConverter<byte[]?, string>(
            v => v != null ? string.Join(",", v) : string.Empty,
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(byte.Parse).ToArray()
        );

        builder.Property(m => m.TilesRowMajor).HasConversion(tileConverter);
        builder.Property(m => m.CollisionRowMajor).HasConversion(byteConverter);
    }

}