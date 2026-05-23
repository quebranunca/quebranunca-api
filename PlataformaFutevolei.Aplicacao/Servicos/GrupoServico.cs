using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class GrupoServico(
    IGrupoRepositorio grupoRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    ILocalRepositorio localRepositorio,
    IGrupoPadraoServico grupoPadraoServico,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IGrupoServico
{
    public async Task<IReadOnlyList<GrupoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var grupos = await grupoRepositorio.ListarAsync(cancellationToken);
        return grupos.Select(x => x.ParaDto()).ToList();
    }

    public async Task<IReadOnlyList<GrupoSelecaoDto>> ListarParaSelecaoAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var incluirPrivadosDeTerceiros = usuario.Perfil == PerfilUsuario.Administrador;
        var grupos = await grupoRepositorio.ListarParaSelecaoAsync(
            usuario.Id,
            usuario.AtletaId,
            incluirPrivadosDeTerceiros,
            cancellationToken);

        return grupos.Select(ParaSelecaoDto).ToList();
    }

    public async Task<GrupoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var grupo = await grupoRepositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Grupo não encontrado.");
        return grupo.ParaDto();
    }

    public async Task<GrupoDto> CriarAsync(CriarGrupoDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil is not PerfilUsuario.Administrador and not PerfilUsuario.Organizador and not PerfilUsuario.Atleta)
        {
            throw new RegraNegocioException("Apenas administradores, organizadores ou atletas podem criar grupos.");
        }

        var nome = Validar(dto.Nome, dto.DataInicio, dto.DataFim, dto.Link);
        if (string.Equals(nome, grupoPadraoServico.NomeGrupoGeral, StringComparison.OrdinalIgnoreCase))
        {
            var grupoGeral = await grupoPadraoServico.ObterOuCriarGrupoGeralAsync(cancellationToken);
            return grupoGeral.ParaDto();
        }

        await grupoPadraoServico.ValidarNomeDisponivelOuAcessivelAsync(nome, cancellationToken: cancellationToken);
        var dataInicioUtc = NormalizarParaUtc(dto.DataInicio);
        var dataFimUtc = dto.DataFim.HasValue ? NormalizarParaUtc(dto.DataFim.Value) : (DateTime?)null;
        await ValidarLocalAsync(dto.LocalId, cancellationToken);

        var grupo = new Grupo
        {
            Nome = nome,
            Descricao = dto.Descricao?.Trim(),
            Link = NormalizarLink(dto.Link),
            DataInicio = dataInicioUtc,
            DataFim = dataFimUtc,
            LocalId = dto.LocalId,
            UsuarioOrganizadorId = usuario.Perfil is PerfilUsuario.Organizador or PerfilUsuario.Atleta ? usuario.Id : null
        };

        await grupoRepositorio.AdicionarAsync(grupo, cancellationToken);
        if (usuario.AtletaId.HasValue)
        {
            await grupoAtletaRepositorio.AdicionarAsync(new GrupoAtleta
            {
                GrupoId = grupo.Id,
                AtletaId = usuario.AtletaId.Value
            }, cancellationToken);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var criado = await grupoRepositorio.ObterPorIdAsync(grupo.Id, cancellationToken);
        return criado!.ParaDto();
    }

    public async Task<GrupoDto> AtualizarAsync(Guid id, AtualizarGrupoDto dto, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoGrupoAsync(id, cancellationToken);
        var grupo = await grupoRepositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Grupo não encontrado.");

        var nome = Validar(dto.Nome, dto.DataInicio, dto.DataFim, dto.Link);
        if (string.Equals(nome, grupoPadraoServico.NomeGrupoGeral, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(grupo.Nome?.Trim(), grupoPadraoServico.NomeGrupoGeral, StringComparison.OrdinalIgnoreCase))
        {
            throw new RegraNegocioException("O nome Geral é reservado para o grupo global padrão.");
        }

        await grupoPadraoServico.ValidarNomeDisponivelOuAcessivelAsync(nome, id, cancellationToken);
        await ValidarLocalAsync(dto.LocalId, cancellationToken);

        grupo.Nome = nome;
        grupo.Descricao = dto.Descricao?.Trim();
        grupo.Link = NormalizarLink(dto.Link);
        grupo.DataInicio = NormalizarParaUtc(dto.DataInicio);
        grupo.DataFim = dto.DataFim.HasValue ? NormalizarParaUtc(dto.DataFim.Value) : (DateTime?)null;
        grupo.LocalId = dto.LocalId;
        grupo.AtualizarDataModificacao();

        grupoRepositorio.Atualizar(grupo);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var atualizado = await grupoRepositorio.ObterPorIdAsync(id, cancellationToken);
        return atualizado!.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoGrupoAsync(id, cancellationToken);
        var grupo = await grupoRepositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Grupo não encontrado.");

        grupoRepositorio.Remover(grupo);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task ValidarLocalAsync(Guid? localId, CancellationToken cancellationToken)
    {
        if (!localId.HasValue)
        {
            return;
        }

        if (await localRepositorio.ObterPorIdAsync(localId.Value, cancellationToken) is null)
        {
            throw new RegraNegocioException("Local informado não foi encontrado.");
        }
    }

    private static string Validar(string nome, DateTime dataInicio, DateTime? dataFim, string? link)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome do grupo é obrigatório.");
        }

        if (dataFim.HasValue && dataFim.Value < dataInicio)
        {
            throw new RegraNegocioException("Data final não pode ser anterior à data inicial.");
        }

        if (!string.IsNullOrWhiteSpace(link) &&
            !Uri.TryCreate(link.Trim(), UriKind.Absolute, out _))
        {
            throw new RegraNegocioException("Link do grupo deve ser uma URL válida.");
        }

        return nome.Trim();
    }

    private static string? NormalizarLink(string? link)
        => string.IsNullOrWhiteSpace(link) ? null : link.Trim();

    private static DateTime NormalizarParaUtc(DateTime data)
    {
        return data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
        };
    }

    private static GrupoSelecaoDto ParaSelecaoDto(Grupo grupo)
    {
        var privacidade = grupo.UsuarioOrganizadorId.HasValue ? "Privado" : "Público";
        return new GrupoSelecaoDto(
            grupo.Id,
            grupo.Nome,
            grupo.Atletas?.Count ?? 0,
            null,
            privacidade);
    }
}
