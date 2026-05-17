using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class SolicitacaoAcessoMapeamento : IEntityTypeConfiguration<SolicitacaoAcesso>
{
    public void Configure(EntityTypeBuilder<SolicitacaoAcesso> builder)
    {
        builder.ToTable("solicitacoes_acesso");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasDefaultValue(StatusSolicitacaoAcesso.Pendente)
            .IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasIndex(x => x.Email)
            .HasFilter("status = 1")
            .IsUnique();
        builder.HasIndex(x => new { x.Email, x.Status });
    }
}
