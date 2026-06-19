using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class ExtratoPontuacaoBeneficioMapeamento : IEntityTypeConfiguration<ExtratoPontuacaoBeneficio>
{
    public void Configure(EntityTypeBuilder<ExtratoPontuacaoBeneficio> builder)
    {
        builder.ToTable("extratos_pontuacao_beneficio", tabela =>
        {
            tabela.HasCheckConstraint("ck_extratos_pontuacao_beneficio_pontos_nao_zero", "\"pontos\" <> 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id").IsRequired();
        builder.Property(x => x.GrupoId).HasColumnName("grupo_id");
        builder.Property(x => x.PartidaId).HasColumnName("partida_id");
        builder.Property(x => x.ResgateId).HasColumnName("resgate_id");
        builder.Property(x => x.TipoEvento).HasColumnName("tipo_evento").HasConversion<int>().IsRequired();
        builder.Property(x => x.Pontos).HasColumnName("pontos").IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Origem).HasColumnName("origem").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ChaveIdempotencia).HasColumnName("chave_idempotencia").HasMaxLength(220).IsRequired();
        builder.Property(x => x.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id");
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Atleta)
            .WithMany()
            .HasForeignKey(x => x.AtletaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Grupo)
            .WithMany()
            .HasForeignKey(x => x.GrupoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Partida)
            .WithMany()
            .HasForeignKey(x => x.PartidaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Resgate)
            .WithMany()
            .HasForeignKey(x => x.ResgateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CriadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.AtletaId);
        builder.HasIndex(x => x.GrupoId);
        builder.HasIndex(x => x.PartidaId);
        builder.HasIndex(x => x.ResgateId);
        builder.HasIndex(x => x.TipoEvento);
        builder.HasIndex(x => x.DataCriacao);
        builder.HasIndex(x => x.ChaveIdempotencia).IsUnique();
        builder.HasIndex(x => new { x.AtletaId, x.TipoEvento, x.DataCriacao });
    }
}
