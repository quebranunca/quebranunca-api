using PlataformaFutevolei.Dominio.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class AtletaMapeamento : IEntityTypeConfiguration<Atleta>
{
    public void Configure(EntityTypeBuilder<Atleta> builder)
    {
        builder.ToTable("atletas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Apelido).HasColumnName("apelido").HasMaxLength(100);
        builder.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(30);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(150);
        builder.Property(x => x.Instagram).HasColumnName("instagram").HasMaxLength(100);
        builder.Property(x => x.Cpf).HasColumnName("cpf").HasMaxLength(20);
        builder.Property(x => x.Bairro).HasColumnName("bairro").HasMaxLength(120);
        builder.Property(x => x.Cidade).HasColumnName("cidade").HasMaxLength(120);
        builder.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(60);
        builder.Property(x => x.CadastroPendente).HasColumnName("cadastro_pendente").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Sexo).HasColumnName("sexo").HasConversion<int>();
        builder.Property(x => x.Nivel).HasColumnName("nivel").HasConversion<int>();
        builder.Property(x => x.Lado).HasColumnName("lado").HasConversion<int>().HasDefaultValue(LadoAtleta.Ambos).IsRequired();
        builder.Property(x => x.DataNascimento).HasColumnName("data_nascimento").HasColumnType("date");
        builder.Property(x => x.UsuarioCriadorId).HasColumnName("usuario_criador_id");
        builder.Property(x => x.PeDominante).HasColumnName("pe_dominante").HasConversion<int>();
        builder.Property(x => x.TempoPratica).HasColumnName("tempo_pratica").HasConversion<int>();
        builder.Property(x => x.ArenaPrincipalId).HasColumnName("arena_principal_id");
        builder.Property(x => x.ObjetivoAtual).HasColumnName("objetivo_atual").HasConversion<int>();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.UsuarioCriador)
            .WithMany()
            .HasForeignKey(x => x.UsuarioCriadorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ArenaPrincipal)
            .WithMany()
            .HasForeignKey(x => x.ArenaPrincipalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Cpf);
        builder.HasIndex(x => x.Nome);
        builder.HasIndex(x => x.UsuarioCriadorId);
        builder.HasIndex(x => x.ArenaPrincipalId);
    }
}
