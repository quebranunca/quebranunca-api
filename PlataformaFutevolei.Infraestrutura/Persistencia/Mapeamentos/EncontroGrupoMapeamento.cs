using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class EncontroGrupoMapeamento : IEntityTypeConfiguration<EncontroGrupo>
{
    public void Configure(EntityTypeBuilder<EncontroGrupo> builder)
    {
        builder.ToTable("encontros_grupos");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.GrupoId).HasColumnName("grupo_id").IsRequired();
        builder.Property(x => x.DataJogo).HasColumnName("data_jogo").HasColumnType("date").IsRequired();
        builder.Property(x => x.HorarioInicio).HasColumnName("horario_inicio").HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.HorarioFim).HasColumnName("horario_fim").HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.ArenaId).HasColumnName("arena_id");
        builder.Property(x => x.LocalSnapshot).HasColumnName("local_snapshot").HasMaxLength(200);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Grupo)
            .WithMany(x => x.Encontros)
            .HasForeignKey(x => x.GrupoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Arena)
            .WithMany()
            .HasForeignKey(x => x.ArenaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.GrupoId, x.DataJogo }).IsUnique();
        builder.HasIndex(x => x.ArenaId);
        builder.HasIndex(x => x.DataJogo);
    }
}
