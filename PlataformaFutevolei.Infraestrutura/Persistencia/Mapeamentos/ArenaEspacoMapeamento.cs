using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class ArenaEspacoMapeamento : IEntityTypeConfiguration<ArenaEspaco>
{
    public void Configure(EntityTypeBuilder<ArenaEspaco> builder)
    {
        builder.ToTable("arena_espacos");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ArenaId).HasColumnName("arena_id").IsRequired();
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(x => x.TipoEspaco).HasColumnName("tipo_espaco").IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(1000);
        builder.Property(x => x.PossuiIluminacao).HasColumnName("possui_iluminacao").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PossuiCobertura).HasColumnName("possui_cobertura").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Ativo).HasColumnName("ativo").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.OrdemExibicao).HasColumnName("ordem_exibicao");
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Arena)
            .WithMany(x => x.Espacos)
            .HasForeignKey(x => x.ArenaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ArenaId, x.Ativo });
        builder.HasIndex(x => new { x.ArenaId, x.OrdemExibicao });
    }
}
