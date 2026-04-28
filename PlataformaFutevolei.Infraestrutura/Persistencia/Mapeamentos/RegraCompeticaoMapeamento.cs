using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class RegraCompeticaoMapeamento : IEntityTypeConfiguration<RegraCompeticao>
{
    public void Configure(EntityTypeBuilder<RegraCompeticao> builder)
    {
        builder.ToTable("regras_competicao");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(1000);
        builder.Property(x => x.PontosMinimosPartida).HasColumnName("pontos_minimos_partida").IsRequired();
        builder.Property(x => x.DiferencaMinimaPartida).HasColumnName("diferenca_minima_partida").IsRequired();
        builder.Property(x => x.PermiteEmpate).HasColumnName("permite_empate").IsRequired();
        builder.Property(x => x.PontosVitoria).HasColumnName("pontos_vitoria").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.PontosDerrota).HasColumnName("pontos_derrota").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.PontosParticipacao).HasColumnName("pontos_participacao").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.PontosPrimeiroLugar).HasColumnName("pontos_primeiro_lugar").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.PontosSegundoLugar).HasColumnName("pontos_segundo_lugar").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.PontosTerceiroLugar).HasColumnName("pontos_terceiro_lugar").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.UsuarioCriadorId).HasColumnName("usuario_criador_id");
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.UsuarioCriador)
            .WithMany()
            .HasForeignKey(x => x.UsuarioCriadorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Nome).IsUnique();
        builder.HasIndex(x => x.UsuarioCriadorId);
    }
}
