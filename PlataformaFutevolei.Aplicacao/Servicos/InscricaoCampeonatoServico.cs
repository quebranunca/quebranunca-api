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

public class InscricaoCampeonatoServico(
    IInscricaoCampeonatoRepositorio inscricaoRepositorio,
    ICompeticaoRepositorio competicaoRepositorio,
    ICategoriaCompeticaoRepositorio categoriaRepositorio,
    IDuplaRepositorio duplaRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IResolvedorAtletaDuplaServico resolvedorAtletaDuplaServico
) : IInscricaoCampeonatoServico
{
    public async Task<IReadOnlyList<InscricaoCampeonatoDto>> ListarPorCampeonatoAsync(
        Guid campeonatoId,
        Guid? categoriaId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualAsync(cancellationToken);
        await ObterCompeticaoComInscricaoValidaAsync(campeonatoId, cancellationToken);

        if (categoriaId.HasValue)
        {
            await ObterCategoriaValidaAsync(campeonatoId, categoriaId.Value, cancellationToken);
        }

        var inscricoes = await inscricaoRepositorio.ListarPorCampeonatoAsync(campeonatoId, categoriaId, cancellationToken);
        if (usuario?.Perfil == PerfilUsuario.Atleta)
        {
            var atletaId = ObterAtletaUsuarioIdObrigatorio(usuario);
            inscricoes = inscricoes
                .Where(x => DuplaContemAtleta(x.Dupla, atletaId))
                .ToList();
        }
        else if (usuario is not null)
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(campeonatoId, cancellationToken);
        }
        else
        {
            inscricoes = inscricoes
                .Where(x => x.Status == StatusInscricaoCampeonato.Ativa)
                .ToList();
        }

        return inscricoes.Select(x => x.ParaDto()).ToList();
    }

    public async Task<InscricaoCampeonatoDto> ObterPorIdAsync(
        Guid campeonatoId,
        Guid inscricaoId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await ObterCompeticaoComInscricaoValidaAsync(campeonatoId, cancellationToken);

        var inscricao = await inscricaoRepositorio.ObterPorIdAsync(inscricaoId, cancellationToken);
        if (inscricao is null || inscricao.CompeticaoId != campeonatoId)
        {
            throw new EntidadeNaoEncontradaException("Inscrição não encontrada para o campeonato informado.");
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            var atletaId = ObterAtletaUsuarioIdObrigatorio(usuario);
            if (!DuplaContemAtleta(inscricao.Dupla, atletaId))
            {
                throw new RegraNegocioException("Você só pode acessar as suas próprias inscrições.");
            }
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(campeonatoId, cancellationToken);
        }

        return inscricao.ParaDto();
    }

    public async Task<InscricaoCampeonatoDto> CriarAsync(
        Guid campeonatoId,
        CriarInscricaoCampeonatoDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var campeonato = await ObterCompeticaoComInscricaoValidaAsync(campeonatoId, cancellationToken, exigirInscricoesAbertas: true);
        var categoria = await ObterCategoriaValidaAsync(campeonatoId, dto.CategoriaId, cancellationToken);
        await ValidarCategoriaAptaParaReceberInscricaoAsync(categoria, cancellationToken);
        Dupla dupla;
        var parceiroCadastroPendente = false;

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            (dupla, parceiroCadastroPendente) = await ResolverDuplaAutoInscricaoAsync(usuario, dto, cancellationToken);
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(campeonatoId, cancellationToken);
            dupla = await ResolverDuplaAsync(dto, cancellationToken);
        }

        var inscricaoDuplicada = await inscricaoRepositorio.ObterDuplicadaAsync(dto.CategoriaId, dupla.Id, cancellationToken);
        if (inscricaoDuplicada is not null)
        {
            throw new RegraNegocioException("Esta dupla já está inscrita nesta categoria do campeonato.");
        }

        var inscricao = new InscricaoCampeonato
        {
            CompeticaoId = campeonato.Id,
            CategoriaCompeticaoId = categoria.Id,
            DuplaId = dupla.Id,
            Pago = usuario.Perfil == PerfilUsuario.Atleta ? false : dto.Pago,
            DataInscricaoUtc = DateTime.UtcNow,
            Status = usuario.Perfil == PerfilUsuario.Atleta
                ? StatusInscricaoCampeonato.PendenteAprovacao
                : StatusInscricaoCampeonato.Ativa,
            Observacao = MontarObservacaoInscricao(dto.Observacao, parceiroCadastroPendente),
            Competicao = campeonato,
            CategoriaCompeticao = categoria,
            Dupla = dupla
        };

        await inscricaoRepositorio.AdicionarAsync(inscricao, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var inscricaoCriada = await inscricaoRepositorio.ObterPorIdAsync(inscricao.Id, cancellationToken);
        return inscricaoCriada!.ParaDto();
    }

    public async Task<InscricaoCampeonatoDto> AtualizarAsync(
        Guid campeonatoId,
        Guid inscricaoId,
        CriarInscricaoCampeonatoDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await ObterCompeticaoComInscricaoValidaAsync(campeonatoId, cancellationToken, exigirInscricoesAbertas: true);

        var inscricao = await inscricaoRepositorio.ObterPorIdAsync(inscricaoId, cancellationToken);
        if (inscricao is null || inscricao.CompeticaoId != campeonatoId)
        {
            throw new EntidadeNaoEncontradaException("Inscrição não encontrada para o campeonato informado.");
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            var atletaId = ObterAtletaUsuarioIdObrigatorio(usuario);
            if (!DuplaContemAtleta(inscricao.Dupla, atletaId))
            {
                throw new RegraNegocioException("Você só pode editar as suas próprias inscrições.");
            }
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(campeonatoId, cancellationToken);
        }

        var categoria = await ObterCategoriaValidaAsync(campeonatoId, dto.CategoriaId, cancellationToken);
        await ValidarCategoriaAptaParaReceberInscricaoAsync(categoria, cancellationToken, inscricao.Id);
        Dupla dupla;
        var parceiroCadastroPendente = false;

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            (dupla, parceiroCadastroPendente) = await ResolverDuplaAutoInscricaoAsync(usuario, dto, cancellationToken);
        }
        else
        {
            dupla = await ResolverDuplaAsync(dto, cancellationToken);
        }

        var inscricaoDuplicada = await inscricaoRepositorio.ObterDuplicadaAsync(dto.CategoriaId, dupla.Id, cancellationToken);
        if (inscricaoDuplicada is not null && inscricaoDuplicada.Id != inscricao.Id)
        {
            throw new RegraNegocioException("Esta dupla já está inscrita nesta categoria do campeonato.");
        }

        inscricao.CategoriaCompeticaoId = categoria.Id;
        inscricao.DuplaId = dupla.Id;
        inscricao.Observacao = MontarObservacaoInscricao(dto.Observacao, parceiroCadastroPendente);

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            inscricao.Status = StatusInscricaoCampeonato.PendenteAprovacao;
        }
        else
        {
            inscricao.Pago = dto.Pago;
        }

        inscricao.AtualizarDataModificacao();
        inscricaoRepositorio.Atualizar(inscricao);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var inscricaoAtualizada = await inscricaoRepositorio.ObterPorIdAsync(inscricao.Id, cancellationToken);
        return inscricaoAtualizada!.ParaDto();
    }

    public async Task<InscricaoCampeonatoDto> AprovarAsync(
        Guid campeonatoId,
        Guid inscricaoId,
        CancellationToken cancellationToken = default)
    {
        await ObterCompeticaoComInscricaoValidaAsync(campeonatoId, cancellationToken);
        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(campeonatoId, cancellationToken);

        var inscricao = await inscricaoRepositorio.ObterPorIdAsync(inscricaoId, cancellationToken);
        if (inscricao is null || inscricao.CompeticaoId != campeonatoId)
        {
            throw new EntidadeNaoEncontradaException("Inscrição não encontrada para o campeonato informado.");
        }

        if (inscricao.Status == StatusInscricaoCampeonato.Cancelada)
        {
            throw new RegraNegocioException("Inscrições canceladas não podem ser aprovadas.");
        }

        if (inscricao.Status != StatusInscricaoCampeonato.Ativa)
        {
            await ValidarLimiteAprovacaoAsync(inscricao.CategoriaCompeticao, inscricao.Id, cancellationToken);
            inscricao.Status = StatusInscricaoCampeonato.Ativa;
            inscricao.AtualizarDataModificacao();
            inscricaoRepositorio.Atualizar(inscricao);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        }

        var inscricaoAprovada = await inscricaoRepositorio.ObterPorIdAsync(inscricao.Id, cancellationToken);
        return inscricaoAprovada!.ParaDto();
    }

    public async Task RemoverAsync(
        Guid campeonatoId,
        Guid inscricaoId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await ObterCompeticaoComInscricaoValidaAsync(campeonatoId, cancellationToken);

        var inscricao = await inscricaoRepositorio.ObterPorIdAsync(inscricaoId, cancellationToken);
        if (inscricao is null || inscricao.CompeticaoId != campeonatoId)
        {
            throw new EntidadeNaoEncontradaException("Inscrição não encontrada para o campeonato informado.");
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            var atletaId = ObterAtletaUsuarioIdObrigatorio(usuario);
            if (!DuplaContemAtleta(inscricao.Dupla, atletaId))
            {
                throw new RegraNegocioException("Você só pode excluir as suas próprias inscrições.");
            }
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(campeonatoId, cancellationToken);
        }

        inscricaoRepositorio.Remover(inscricao);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<(Dupla dupla, bool parceiroCadastroPendente)> ResolverDuplaAutoInscricaoAsync(
        Usuario usuario,
        CriarInscricaoCampeonatoDto dto,
        CancellationToken cancellationToken)
    {
        var atletaUsuarioId = ObterAtletaUsuarioIdObrigatorio(usuario);
        var atletaUsuario = await resolvedorAtletaDuplaServico.ObterAtletaExistenteAsync(
            atletaUsuarioId,
            "Você precisa ter um atleta válido vinculado para se inscrever.",
            cancellationToken);

        if (dto.DuplaId.HasValue)
        {
            var duplaExistente = await duplaRepositorio.ObterPorIdAsync(dto.DuplaId.Value, cancellationToken);
            if (duplaExistente is null)
            {
                throw new EntidadeNaoEncontradaException("Dupla não encontrada.");
            }

            if (!DuplaContemAtleta(duplaExistente, atletaUsuario.Id))
            {
                throw new RegraNegocioException("Você só pode se inscrever com uma dupla que contenha o seu atleta vinculado.");
            }

            return (duplaExistente, false);
        }

        var (parceiro, parceiroCadastroPendente) = await ResolverParceiroAutoInscricaoAsync(atletaUsuario, dto, cancellationToken);
        var dupla = await resolvedorAtletaDuplaServico.ObterOuCriarDuplaAsync(atletaUsuario, parceiro, cancellationToken);
        return (dupla, parceiroCadastroPendente);
    }

    private async Task<Competicao> ObterCompeticaoComInscricaoValidaAsync(
        Guid campeonatoId,
        CancellationToken cancellationToken,
        bool exigirInscricoesAbertas = false)
    {
        var campeonato = await competicaoRepositorio.ObterPorIdAsync(campeonatoId, cancellationToken);
        if (campeonato is null)
        {
            throw new EntidadeNaoEncontradaException("Competição não encontrada.");
        }

        if (campeonato.Tipo is not TipoCompeticao.Campeonato and not TipoCompeticao.Evento)
        {
            throw new RegraNegocioException("A competição informada não aceita inscrições.");
        }

        if (exigirInscricoesAbertas && !campeonato.InscricoesAbertas)
        {
            throw new RegraNegocioException("A competição não está apta para receber inscrições.");
        }

        return campeonato;
    }

    private async Task<CategoriaCompeticao> ObterCategoriaValidaAsync(
        Guid campeonatoId,
        Guid categoriaId,
        CancellationToken cancellationToken)
    {
        var categoria = await categoriaRepositorio.ObterPorIdAsync(categoriaId, cancellationToken);
        if (categoria is null)
        {
            throw new EntidadeNaoEncontradaException("Categoria não encontrada.");
        }

        if (categoria.CompeticaoId != campeonatoId)
        {
            throw new RegraNegocioException("A categoria informada não pertence à competição.");
        }

        return categoria;
    }

    private async Task ValidarCategoriaAptaParaReceberInscricaoAsync(
        CategoriaCompeticao categoria,
        CancellationToken cancellationToken,
        Guid? ignorarInscricaoId = null)
    {
        if (categoria.InscricoesEncerradas)
        {
            throw new RegraNegocioException("As inscrições desta categoria estão encerradas.");
        }

        if (!categoria.QuantidadeMaximaDuplas.HasValue)
        {
            return;
        }

        var quantidadeInscricoes = await inscricaoRepositorio.ContarPorCategoriaAsync(
            categoria.Id,
            ignorarInscricaoId,
            cancellationToken);

        if (quantidadeInscricoes >= categoria.QuantidadeMaximaDuplas.Value)
        {
            throw new RegraNegocioException("A categoria já atingiu a quantidade máxima de duplas inscritas.");
        }
    }

    private async Task ValidarLimiteAprovacaoAsync(
        CategoriaCompeticao categoria,
        Guid inscricaoId,
        CancellationToken cancellationToken)
    {
        if (!categoria.QuantidadeMaximaDuplas.HasValue)
        {
            return;
        }

        var inscricoesCategoria = await inscricaoRepositorio.ListarPorCampeonatoAsync(
            categoria.CompeticaoId,
            categoria.Id,
            cancellationToken);

        var quantidadeAtivas = inscricoesCategoria.Count(x =>
            x.Id != inscricaoId &&
            x.Status == StatusInscricaoCampeonato.Ativa);

        if (quantidadeAtivas >= categoria.QuantidadeMaximaDuplas.Value)
        {
            throw new RegraNegocioException("A categoria já atingiu a quantidade máxima de duplas aprovadas.");
        }
    }

    private async Task<Dupla> ResolverDuplaAsync(
        CriarInscricaoCampeonatoDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.DuplaId.HasValue)
        {
            var duplaExistente = await duplaRepositorio.ObterPorIdAsync(dto.DuplaId.Value, cancellationToken);
            if (duplaExistente is null)
            {
                throw new EntidadeNaoEncontradaException("Dupla não encontrada.");
            }

            return duplaExistente;
        }

        var atleta1 = await resolvedorAtletaDuplaServico.ResolverAtletaAsync(
            dto.Atleta1Id,
            dto.NomeAtleta1,
            dto.ApelidoAtleta1,
            "Os atletas informados para a inscrição não foram encontrados.",
            dto.Atleta1CadastroPendente,
            cancellationToken);

        var atleta2 = await resolvedorAtletaDuplaServico.ResolverAtletaAsync(
            dto.Atleta2Id,
            dto.NomeAtleta2,
            dto.ApelidoAtleta2,
            "Os atletas informados para a inscrição não foram encontrados.",
            dto.Atleta2CadastroPendente,
            cancellationToken);

        if (atleta1.Id == atleta2.Id)
        {
            throw new RegraNegocioException("Um atleta não pode formar dupla com ele mesmo.");
        }

        return await resolvedorAtletaDuplaServico.ObterOuCriarDuplaAsync(atleta1, atleta2, cancellationToken);
    }

    private async Task<(Atleta atleta, bool cadastroPendente)> ResolverParceiroAutoInscricaoAsync(
        Atleta atletaUsuario,
        CriarInscricaoCampeonatoDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.Atleta2Id.HasValue || !string.IsNullOrWhiteSpace(dto.NomeAtleta2))
        {
            var atleta = await resolvedorAtletaDuplaServico.ResolverAtletaAsync(
                dto.Atleta2Id,
                dto.NomeAtleta2,
                dto.ApelidoAtleta2,
                "Os atletas informados para a inscrição não foram encontrados.",
                dto.Atleta2CadastroPendente,
                cancellationToken);
            if (atleta.Id == atletaUsuario.Id)
            {
                throw new RegraNegocioException("Um atleta não pode formar dupla com ele mesmo.");
            }

            return (atleta, dto.Atleta2CadastroPendente);
        }

        var nomeParceiroPendente = CriarNomeAtletaPendente(atletaUsuario.Nome);
        var atletaPendente = await resolvedorAtletaDuplaServico.ObterOuCriarAtletaAsync(
            nomeParceiroPendente,
            null,
            true,
            cancellationToken);
        if (atletaPendente.Id == atletaUsuario.Id)
        {
            throw new RegraNegocioException("Um atleta não pode formar dupla com ele mesmo.");
        }

        return (atletaPendente, true);
    }
    private static Guid ObterAtletaUsuarioIdObrigatorio(Usuario usuario)
    {
        if (!usuario.AtletaId.HasValue)
        {
            throw new RegraNegocioException("Você precisa ter um atleta vinculado para se inscrever.");
        }

        return usuario.AtletaId.Value;
    }

    private static bool DuplaContemAtleta(Dupla? dupla, Guid atletaId)
    {
        return dupla is not null && (dupla.Atleta1Id == atletaId || dupla.Atleta2Id == atletaId);
    }

    private static string CriarNomeAtletaPendente(string nomeAtleta)
    {
        return $"Dupla da {NormalizadorNomeAtleta.NormalizarTexto(nomeAtleta)}";
    }

    private static string? MontarObservacaoInscricao(string? observacaoAtual, bool parceiroCadastroPendente)
    {
        var observacao = NormalizadorNomeAtleta.NormalizarTexto(observacaoAtual);
        if (!parceiroCadastroPendente)
        {
            return string.IsNullOrWhiteSpace(observacao) ? null : observacao;
        }

        return string.IsNullOrWhiteSpace(observacao)
            ? "Parceiro com cadastro pendente."
            : $"{observacao} | Parceiro com cadastro pendente.";
    }
}
