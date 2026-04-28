using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class FormatoCampeonatoMapeamento : IEntityTypeConfiguration<FormatoCampeonato>
{
    public void Configure(EntityTypeBuilder<FormatoCampeonato> builder)
    {
        builder.ToTable("formatos_campeonato");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(1000);
        builder.Property(x => x.TipoFormato).HasColumnName("tipo_formato").IsRequired();
        builder.Property(x => x.Ativo).HasColumnName("ativo").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.QuantidadeGrupos).HasColumnName("quantidade_grupos");
        builder.Property(x => x.ClassificadosPorGrupo).HasColumnName("classificados_por_grupo");
        builder.Property(x => x.GeraMataMataAposGrupos).HasColumnName("gera_mata_mata_apos_grupos").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.TurnoEVolta).HasColumnName("turno_e_volta").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.TipoChave).HasColumnName("tipo_chave").HasMaxLength(100);
        builder.Property(x => x.QuantidadeDerrotasParaEliminacao).HasColumnName("quantidade_derrotas_para_eliminacao");
        builder.Property(x => x.PermiteCabecaDeChave).HasColumnName("permite_cabeca_de_chave").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DisputaTerceiroLugar).HasColumnName("disputa_terceiro_lugar").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasIndex(x => x.Nome).IsUnique();
    }
}
