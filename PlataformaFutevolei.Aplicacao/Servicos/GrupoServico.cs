using System.Globalization;
using System.Text;
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
    IArenaRepositorio arenaRepositorio,
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

    public async Task<GrupoVerificacaoNomeDto> VerificarNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var nomeValidado = ValidarNome(nome);
        var nomeNormalizado = NormalizarParaBusca(nomeValidado);
        var grupos = await grupoRepositorio.ListarAsync(cancellationToken);
        var candidatos = grupos
            .Where(grupo => !string.Equals(grupo.Nome, grupoPadraoServico.NomeGrupoGeral, StringComparison.OrdinalIgnoreCase))
            .Select(grupo => new
            {
                Grupo = grupo,
                NomeNormalizado = NormalizarParaBusca(grupo.Nome)
            })
            .ToList();
        var existeExato = candidatos.Any(x => x.NomeNormalizado == nomeNormalizado);
        var termos = nomeNormalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var similares = candidatos
            .Where(x => x.NomeNormalizado != nomeNormalizado && NomePareceComBusca(x.NomeNormalizado, nomeNormalizado, termos))
            .Take(5)
            .Select(x => new GrupoNomeSimilarDto(
                x.Grupo.Id,
                x.Grupo.Nome,
                x.Grupo.Atletas?.Count ?? 0,
                ObterPrivacidade(x.Grupo)))
            .ToList();

        return new GrupoVerificacaoNomeDto(!existeExato, existeExato, similares);
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
        var arenaId = dto.ArenaId ?? dto.LocalId;
        await ValidarArenaAsync(arenaId, cancellationToken);
        var publico = EhPublico(dto.Privacidade);

        var grupo = new Grupo
        {
            Nome = nome,
            Descricao = dto.Descricao?.Trim(),
            Link = NormalizarLink(dto.Link),
            DataInicio = dataInicioUtc,
            DataFim = dataFimUtc,
            ArenaId = arenaId,
            UsuarioOrganizadorId = usuario.Id,
            Publico = publico,
            ImagemUrl = NormalizarImagemUrl(dto.ImagemUrl)
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

        var nome = ValidarNome(dto.Nome);
        if (string.Equals(nome, grupoPadraoServico.NomeGrupoGeral, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(grupo.Nome?.Trim(), grupoPadraoServico.NomeGrupoGeral, StringComparison.OrdinalIgnoreCase))
        {
            throw new RegraNegocioException("O nome Geral é reservado para o grupo global padrão.");
        }

        await grupoPadraoServico.ValidarNomeDisponivelOuAcessivelAsync(nome, id, cancellationToken);

        grupo.Nome = nome;
        grupo.Publico = ResolverPublico(dto, grupo.Publico);
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

    private async Task ValidarArenaAsync(Guid? arenaId, CancellationToken cancellationToken)
    {
        if (!arenaId.HasValue)
        {
            return;
        }

        if (await arenaRepositorio.ObterPorIdAsync(arenaId.Value, cancellationToken) is null)
        {
            throw new RegraNegocioException("Arena informada não foi encontrada.");
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
        return new GrupoSelecaoDto(
            grupo.Id,
            grupo.Nome,
            grupo.Atletas?.Count ?? 0,
            grupo.ImagemUrl,
            ObterPrivacidade(grupo));
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome do grupo é obrigatório.");
        }

        return nome.Trim();
    }

    private static bool EhPublico(string? privacidade)
        => string.Equals(privacidade?.Trim(), "Público", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(privacidade?.Trim(), "Publico", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(privacidade?.Trim(), "publico", StringComparison.OrdinalIgnoreCase);

    private static bool ResolverPublico(AtualizarGrupoDto dto, bool publicoAtual)
    {
        if (dto.Publico.HasValue)
        {
            return dto.Publico.Value;
        }

        if (string.IsNullOrWhiteSpace(dto.Privacidade))
        {
            return publicoAtual;
        }

        if (EhPublico(dto.Privacidade))
        {
            return true;
        }

        if (string.Equals(dto.Privacidade.Trim(), "Privado", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        throw new RegraNegocioException("Visibilidade do grupo inválida.");
    }

    private static string ObterPrivacidade(Grupo grupo)
        => grupo.Publico ? "Público" : "Privado";

    private static string? NormalizarImagemUrl(string? imagemUrl)
        => string.IsNullOrWhiteSpace(imagemUrl) ? null : imagemUrl.Trim();

    private static bool NomePareceComBusca(string nomeGrupo, string nomeBusca, IReadOnlyCollection<string> termosBusca)
    {
        if (nomeGrupo.Contains(nomeBusca, StringComparison.OrdinalIgnoreCase) ||
            nomeBusca.Contains(nomeGrupo, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return termosBusca.Count > 0 && termosBusca.Count(termo => termo.Length >= 3 && nomeGrupo.Contains(termo)) >= Math.Min(2, termosBusca.Count);
    }

    private static string NormalizarParaBusca(string valor)
    {
        var texto = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(texto.Length);

        foreach (var caractere in texto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(caractere) ? caractere : ' ');
            }
        }

        return string.Join(' ', builder.ToString().Normalize(NormalizationForm.FormC).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
