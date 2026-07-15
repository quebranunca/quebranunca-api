using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class AdministradorInicialServico(
    IUsuarioRepositorio usuarioRepositorio,
    IUnidadeTrabalho unidadeTrabalho
) : IAdministradorInicialServico
{
    public async Task<PromocaoAdministradorInicialResultadoDto> PromoverAsync(
        string emailAdministradorInicial,
        CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmail(emailAdministradorInicial);
        if (string.IsNullOrWhiteSpace(emailNormalizado))
        {
            throw new RegraNegocioException("E-mail do administrador inicial não configurado.");
        }

        var administradoresAtivos = await usuarioRepositorio.ListarAdministradoresAtivosAsync(cancellationToken);
        var administradoresDivergentes = administradoresAtivos
            .Where(x => !string.Equals(NormalizarEmail(x.Email), emailNormalizado, StringComparison.Ordinal))
            .ToList();

        if (administradoresDivergentes.Count > 0)
        {
            throw new RegraNegocioException(
                "Existem outros administradores ativos. Revise os cadastros manualmente antes de promover o administrador inicial desta fase.");
        }

        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário configurado como administrador inicial não encontrado.");

        if (!usuario.Ativo)
        {
            throw new RegraNegocioException("Usuário configurado como administrador inicial está inativo.");
        }

        if (usuario.Perfil == PerfilUsuario.Administrador)
        {
            return new PromocaoAdministradorInicialResultadoDto(
                usuario.Id,
                usuario.Email,
                Promovido: false,
                JaEraAdministrador: true);
        }

        usuario.Perfil = PerfilUsuario.Administrador;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new PromocaoAdministradorInicialResultadoDto(
            usuario.Id,
            usuario.Email,
            Promovido: true,
            JaEraAdministrador: false);
    }

    private static string NormalizarEmail(string email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}
