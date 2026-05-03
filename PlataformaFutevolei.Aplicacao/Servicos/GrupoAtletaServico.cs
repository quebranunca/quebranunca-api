using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class GrupoAtletaServico(
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    IGrupoRepositorio grupoRepositorio,
    IAtletaRepositorio atletaRepositorio,
    IUsuarioRepositorio usuarioRepositorio,
    IPendenciaUsuarioRepositorio pendenciaUsuarioRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IResolvedorAtletaDuplaServico resolvedorAtletaDuplaServico,
    IPendenciaServico pendenciaServico
) : IGrupoAtletaServico
{
    private const string CodigoPossivelDuplicidadeNome = "PossivelDuplicidadeAtletaGrupo";
    private const string MensagemEmailDuplicado = "Já existe um atleta nesse grupo com este email.";
    private const string MensagemPossivelDuplicidadeNome = "Já existe um atleta com esse nome ou apelido e sem email. É o mesmo atleta?";
    private const string ObservacaoPendenciaEmailGrupo = "Atleta incompleto por falta de email.";

    public async Task<IReadOnlyList<GrupoAtletaDto>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var grupo = await ObterGrupoValidoAsync(grupoId, cancellationToken);

        if (usuario.Perfil is PerfilUsuario.Administrador or PerfilUsuario.Organizador)
        {
            await autorizacaoUsuarioServico.GarantirGestaoGrupoAsync(grupoId, cancellationToken);
        }

        var atletas = await grupoAtletaRepositorio.ListarPorGrupoAsync(grupo.Id, cancellationToken);
        return atletas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<GrupoAtletaDto> CriarAsync(
        Guid grupoId,
        CriarGrupoAtletaDto dto,
        CancellationToken cancellationToken = default)
    {
        var nomeOuApelido = NormalizadorNomeAtleta.NormalizarTexto(dto.NomeAtleta);
        if (string.IsNullOrWhiteSpace(nomeOuApelido))
        {
            throw new RegraNegocioException("Nome ou apelido é obrigatório.");
        }

        await autorizacaoUsuarioServico.GarantirGestaoGrupoAsync(grupoId, cancellationToken);
        var grupo = await ObterGrupoValidoAsync(grupoId, cancellationToken);
        var emailNormalizado = NormalizarEmailOpcional(dto.Email);
        var atletasDoGrupo = await grupoAtletaRepositorio.ListarPorGrupoAsync(grupoId, cancellationToken);

        GarantirEmailUnicoNoGrupo(atletasDoGrupo, emailNormalizado);
        GarantirNomeDisponivelParaNovoAtleta(atletasDoGrupo, nomeOuApelido);

        var atleta = await ObterOuCriarAtletaGrupoAsync(nomeOuApelido, emailNormalizado, cancellationToken);

        if (!string.IsNullOrWhiteSpace(emailNormalizado))
        {
            atleta.Email = emailNormalizado;
            atleta.AtualizarDataModificacao();
            atletaRepositorio.Atualizar(atleta);
        }

        var grupoAtletaExistente = await grupoAtletaRepositorio.ObterPorGrupoEAtletaAsync(grupoId, atleta.Id, cancellationToken);
        if (grupoAtletaExistente is not null)
        {
            throw new RegraNegocioException("Este atleta já faz parte do grupo.");
        }

        var grupoAtleta = new GrupoAtleta
        {
            GrupoId = grupoId,
            AtletaId = atleta.Id,
            Grupo = grupo,
            Atleta = atleta
        };

        await grupoAtletaRepositorio.AdicionarAsync(grupoAtleta, cancellationToken);
        await GarantirPendenciaEmailAtletaGrupoAsync(grupo, atleta, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var grupoAtletaCriado = await grupoAtletaRepositorio.ObterPorIdAsync(grupoAtleta.Id, cancellationToken);
        return grupoAtletaCriado!.ParaDto();
    }

    public async Task<GrupoAtletaDto> CompletarEmailAsync(
        Guid grupoId,
        Guid id,
        AtualizarEmailGrupoAtletaDto dto,
        CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoGrupoAsync(grupoId, cancellationToken);
        await ObterGrupoValidoAsync(grupoId, cancellationToken);

        var emailNormalizado = NormalizarEmailObrigatorio(dto.Email);
        var grupoAtleta = await grupoAtletaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (grupoAtleta is null || grupoAtleta.GrupoId != grupoId)
        {
            throw new EntidadeNaoEncontradaException("Atleta do grupo não encontrado.");
        }

        var atletasDoGrupo = await grupoAtletaRepositorio.ListarPorGrupoAsync(grupoId, cancellationToken);
        GarantirEmailUnicoNoGrupo(atletasDoGrupo, emailNormalizado, grupoAtleta.AtletaId);

        grupoAtleta.Atleta.Email = emailNormalizado;
        grupoAtleta.Atleta.CadastroPendente = false;
        grupoAtleta.Atleta.AtualizarDataModificacao();
        atletaRepositorio.Atualizar(grupoAtleta.Atleta);

        await ConcluirPendenciasEmailAtletaGrupoAsync(grupoAtleta.AtletaId, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var atualizado = await grupoAtletaRepositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Atleta do grupo não encontrado.");
        return atualizado.ParaDto();
    }

    public async Task RemoverAsync(Guid grupoId, Guid id, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoGrupoAsync(grupoId, cancellationToken);
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await ObterGrupoValidoAsync(grupoId, cancellationToken);

        var grupoAtleta = await grupoAtletaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (grupoAtleta is null || grupoAtleta.GrupoId != grupoId)
        {
            throw new EntidadeNaoEncontradaException("Atleta do grupo não encontrado.");
        }

        if (usuarioAtual.AtletaId.HasValue && grupoAtleta.AtletaId == usuarioAtual.AtletaId.Value)
        {
            throw new RegraNegocioException("Você não pode remover o próprio atleta do grupo.");
        }

        grupoAtletaRepositorio.Remover(grupoAtleta);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<UsuarioLogadoDto> AssumirMeuNomeNoGrupoAsync(
        Guid grupoId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuarioAtual.Perfil != PerfilUsuario.Atleta)
        {
            throw new RegraNegocioException("Apenas usuários com perfil atleta podem assumir um nome no grupo.");
        }

        await ObterGrupoValidoAsync(grupoId, cancellationToken);

        var grupoAtleta = await grupoAtletaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (grupoAtleta is null || grupoAtleta.GrupoId != grupoId)
        {
            throw new EntidadeNaoEncontradaException("Atleta do grupo não encontrado.");
        }

        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAtual.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");

        var usuarioComMesmoAtleta = await usuarioRepositorio.ObterPorAtletaIdAsync(grupoAtleta.AtletaId, cancellationToken);
        if (usuarioComMesmoAtleta is not null && usuarioComMesmoAtleta.Id != usuario.Id)
        {
            throw new RegraNegocioException("Este nome do grupo já está vinculado a outro usuário.");
        }

        usuario.AtletaId = grupoAtleta.AtletaId;
        if (grupoAtleta.Atleta.Email is null)
        {
            grupoAtleta.Atleta.Email = usuario.Email;
            grupoAtleta.Atleta.AtualizarDataModificacao();
            atletaRepositorio.Atualizar(grupoAtleta.Atleta);
        }

        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        await pendenciaServico.SincronizarAposVinculoAtletaAsync(grupoAtleta.AtletaId, cancellationToken);

        var atualizado = await usuarioRepositorio.ObterPorIdAsync(usuario.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        return atualizado.ParaDto();
    }

    private async Task<Grupo> ObterGrupoValidoAsync(Guid grupoId, CancellationToken cancellationToken)
    {
        var grupo = await grupoRepositorio.ObterPorIdAsync(grupoId, cancellationToken);
        if (grupo is null)
        {
            throw new EntidadeNaoEncontradaException("Grupo não encontrado.");
        }

        return grupo;
    }

    private async Task<Atleta> ObterOuCriarAtletaGrupoAsync(
        string nomeOuApelido,
        string? emailNormalizado,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(emailNormalizado))
        {
            return await ObterOuCriarAtletaGrupoSemEmailAsync(nomeOuApelido, cancellationToken);
        }

        var atletasPorEmail = await atletaRepositorio.ListarPorEmailAsync(emailNormalizado, cancellationToken);
        var atletaPorEmail = atletasPorEmail.FirstOrDefault();
        if (atletaPorEmail is not null)
        {
            if (string.IsNullOrWhiteSpace(atletaPorEmail.Email))
            {
                atletaPorEmail.Email = emailNormalizado;
            }

            return atletaPorEmail;
        }

        var atleta = await resolvedorAtletaDuplaServico.ObterOuCriarAtletaAsync(
            nomeOuApelido,
            null,
            cadastroPendente: string.IsNullOrWhiteSpace(emailNormalizado),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(emailNormalizado))
        {
            atleta.CadastroPendente = false;
        }

        return atleta;
    }

    private async Task<Atleta> ObterOuCriarAtletaGrupoSemEmailAsync(
        string nomeOuApelido,
        CancellationToken cancellationToken)
    {
        var atletasPorNome = await atletaRepositorio.ListarPorNomeAsync(nomeOuApelido, cancellationToken);
        var atletaSemEmail = atletasPorNome.FirstOrDefault(x =>
            string.IsNullOrWhiteSpace(x.Email) &&
            x.Usuario is null);

        if (atletaSemEmail is not null)
        {
            return atletaSemEmail;
        }

        var (nomeFinal, apelidoFinal) = NormalizadorNomeAtleta.NormalizarNomeEApelido(nomeOuApelido, null);
        var atleta = new Atleta
        {
            Nome = nomeFinal,
            Apelido = apelidoFinal,
            CadastroPendente = true,
            Lado = LadoAtleta.Ambos
        };

        await atletaRepositorio.AdicionarAsync(atleta, cancellationToken);
        return atleta;
    }

    private static void GarantirEmailUnicoNoGrupo(
        IReadOnlyList<GrupoAtleta> atletasDoGrupo,
        string? emailNormalizado,
        Guid? ignorarAtletaId = null)
    {
        if (string.IsNullOrWhiteSpace(emailNormalizado))
        {
            return;
        }

        var emailDuplicado = atletasDoGrupo.Any(x =>
            x.AtletaId != ignorarAtletaId &&
            !string.IsNullOrWhiteSpace(x.Atleta.Email) &&
            string.Equals(x.Atleta.Email, emailNormalizado, StringComparison.OrdinalIgnoreCase));

        if (emailDuplicado)
        {
            throw new RegraNegocioException(MensagemEmailDuplicado);
        }
    }

    private static void GarantirNomeDisponivelParaNovoAtleta(
        IReadOnlyList<GrupoAtleta> atletasDoGrupo,
        string nomeOuApelido)
    {
        var chave = NormalizadorNomeAtleta.NormalizarChave(nomeOuApelido);
        var conflito = atletasDoGrupo.FirstOrDefault(x =>
            string.IsNullOrWhiteSpace(x.Atleta.Email) &&
            (NormalizadorNomeAtleta.NormalizarChave(x.Atleta.Nome) == chave ||
             NormalizadorNomeAtleta.NormalizarChave(x.Atleta.Apelido) == chave));

        if (conflito is null)
        {
            return;
        }

        throw new ConflitoGrupoAtletaException(
            MensagemPossivelDuplicidadeNome,
            CodigoPossivelDuplicidadeNome,
            conflito.Id,
            conflito.AtletaId);
    }

    private async Task GarantirPendenciaEmailAtletaGrupoAsync(
        Grupo grupo,
        Atleta atleta,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(atleta.Email) || !grupo.UsuarioOrganizadorId.HasValue)
        {
            return;
        }

        var pendenciaExistente = await pendenciaUsuarioRepositorio.ObterPendenteAsync(
            TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
            grupo.UsuarioOrganizadorId.Value,
            partidaId: null,
            atleta.Id,
            cancellationToken);
        if (pendenciaExistente is not null)
        {
            return;
        }

        await pendenciaUsuarioRepositorio.AdicionarAsync(new PendenciaUsuario
        {
            Tipo = TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
            UsuarioId = grupo.UsuarioOrganizadorId.Value,
            AtletaId = atleta.Id,
            PartidaId = null,
            Status = StatusPendenciaUsuario.Pendente,
            Observacao = ObservacaoPendenciaEmailGrupo,
            Atleta = atleta
        }, cancellationToken);
    }

    private async Task ConcluirPendenciasEmailAtletaGrupoAsync(Guid atletaId, CancellationToken cancellationToken)
    {
        var pendencias = await pendenciaUsuarioRepositorio.ListarPendentesPorAtletaAsync(atletaId, cancellationToken);
        foreach (var pendencia in pendencias.Where(x => x.Tipo == TipoPendenciaUsuario.CompletarContatoAtletaDaPartida))
        {
            pendencia.Status = StatusPendenciaUsuario.Concluida;
            pendencia.DataConclusao = DateTime.UtcNow;
            pendencia.Observacao = "Contato informado.";
            pendencia.AtualizarDataModificacao();
            pendenciaUsuarioRepositorio.Atualizar(pendencia);
        }
    }

    private static string? NormalizarEmailOpcional(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return NormalizarEmailObrigatorio(email);
    }

    private static string NormalizarEmailObrigatorio(string email)
    {
        var emailNormalizado = NormalizadorNomeAtleta.NormalizarTexto(email).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(emailNormalizado))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        try
        {
            _ = new System.Net.Mail.MailAddress(emailNormalizado);
        }
        catch
        {
            throw new RegraNegocioException("E-mail inválido.");
        }

        return emailNormalizado;
    }
}
