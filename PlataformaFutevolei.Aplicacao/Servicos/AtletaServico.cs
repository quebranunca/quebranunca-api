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

public class AtletaServico(
    IAtletaRepositorio atletaRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IPartidaAprovacaoRepositorio partidaAprovacaoRepositorio,
    IDuplaRepositorio duplaRepositorio,
    IInscricaoCampeonatoRepositorio inscricaoCampeonatoRepositorio,
    IGrupoRepositorio grupoRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    ICompeticaoRepositorio competicaoRepositorio,
    IArenaRepositorio arenaRepositorio,
    IUsuarioRepositorio usuarioRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IResolvedorAtletaDuplaServico resolvedorAtletaDuplaServico,
    IPendenciaServico pendenciaServico,
    IConviteCadastroServico conviteCadastroServico
) : IAtletaServico
{
    public async Task<IReadOnlyList<AtletaPublicoDto>> ListarPublicoAsync(CancellationToken cancellationToken = default)
    {
        var atletas = await atletaRepositorio.ListarAsync(cancellationToken);
        return atletas.Select(x => x.ParaPublicoDto()).ToList();
    }

    public async Task<IReadOnlyList<AtletaDto>> ListarGerencialAsync(
        bool somenteInscritosMinhasCompeticoes = false,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil is not PerfilUsuario.Administrador and not PerfilUsuario.Organizador)
        {
            throw new RegraNegocioException("Apenas administradores ou organizadores podem executar esta operação.");
        }

        var atletas = somenteInscritosMinhasCompeticoes && usuario.Perfil == PerfilUsuario.Organizador
            ? await atletaRepositorio.ListarInscritosPorOrganizadorAsync(usuario.Id, cancellationToken)
            : await atletaRepositorio.ListarAsync(cancellationToken);

        return atletas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<IReadOnlyList<AtletaResumoDto>> BuscarAsync(string? termo, CancellationToken cancellationToken = default)
    {
        var atletas = await atletaRepositorio.BuscarAsync(termo, cancellationToken);
        var quantidadeJogos = await atletaRepositorio.ContarPartidasPorAtletasAsync(
            atletas.Select(x => x.Id),
            cancellationToken);

        return atletas
            .Select(x => x.ParaResumoDto(quantidadeJogos.TryGetValue(x.Id, out var totalJogos) ? totalJogos : 0))
            .ToList();
    }

    public async Task<IReadOnlyList<AtletaResumoDto>> BuscarSugestoesPorCompeticaoAsync(
        Guid competicaoId,
        string? termo,
        CancellationToken cancellationToken = default)
    {
        var termoNormalizado = NormalizadorNomeAtleta.NormalizarTexto(termo ?? string.Empty);
        if (termoNormalizado.Length < 3)
        {
            return [];
        }

        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            var possuiAcesso = await competicaoRepositorio.AtletaPossuiAcessoAsync(
                competicaoId,
                usuario.Id,
                usuario.AtletaId,
                cancellationToken);
            if (!possuiAcesso)
            {
                throw new RegraNegocioException("Você só pode consultar atletas de competições em que participa.");
            }
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(competicaoId, cancellationToken);
        }

        var atletas = await atletaRepositorio.BuscarSugestoesPorCompeticaoAsync(
            competicaoId,
            termoNormalizado,
            cancellationToken);
        return atletas.Select(x => x.ParaResumoDto()).ToList();
    }

    public async Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(
        Guid? grupoId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            return new AtletasSugestoesPartidaDto([], []);
        }

        if (grupoId.HasValue && usuario.Perfil == PerfilUsuario.Atleta)
        {
            var possuiAcesso = await grupoRepositorio.AtletaPossuiAcessoAsync(
                grupoId.Value,
                usuario.Id,
                usuario.AtletaId,
                cancellationToken);
            if (!possuiAcesso)
            {
                throw new RegraNegocioException("Você só pode consultar sugestões de grupos em que participa.");
            }
        }

        return await partidaRepositorio.ObterSugestoesPartidaAsync(
            usuario.AtletaId.Value,
            grupoId,
            3,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AtletaPendenciaDto>> ListarPendenciasAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var partidas = await partidaRepositorio.ListarComAtletasPendentesPorUsuarioCriadorAsync(usuario.Id, cancellationToken);

        return partidas
            .SelectMany(EnumerarPendencias)
            .GroupBy(x => x.Atleta.Id)
            .Select(grupo =>
            {
                var atleta = grupo.First().Atleta;
                return new AtletaPendenciaDto(
                    atleta.Id,
                    atleta.Nome,
                    atleta.Apelido,
                    atleta.Email,
                    atleta.CadastroPendente,
                    StatusCadastroAtletaUtil.PossuiUsuarioVinculado(atleta),
                    StatusCadastroAtletaUtil.TemEmail(atleta),
                    StatusCadastroAtletaUtil.ObterStatusPendencia(atleta),
                    grupo.Select(x => x.PartidaId).Distinct().Count(),
                    grupo.Select(x => x.NomeCompeticao).Distinct().OrderBy(x => x).ToList());
            })
            .OrderBy(x => x.TemEmail)
            .ThenBy(x => x.NomeAtleta)
            .ToList();
    }

    public async Task<AtletaPublicoDto> ObterPublicoPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var atleta = await atletaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (atleta is null)
        {
            throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
        }

        return atleta.ParaPublicoDto();
    }

    public async Task<AtletaDto> ObterGerencialPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            await autorizacaoUsuarioServico.GarantirAcessoAtletaAsync(id, cancellationToken);
        }
        else if (usuario.Perfil == PerfilUsuario.Organizador)
        {
            await GarantirAcessoOrganizadorAsync(id, usuario.Id, cancellationToken);
        }

        var atleta = await atletaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (atleta is null)
        {
            throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
        }

        return atleta.ParaDto();
    }

    public async Task<AtletaDto?> ObterMeuAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            return null;
        }

        var atleta = await atletaRepositorio.ObterPorIdAsync(usuario.AtletaId.Value, cancellationToken);
        return atleta?.ParaDto();
    }

    public async Task<AtletaDto> CriarAsync(CriarAtletaDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var usuarioComum = usuario.Perfil == PerfilUsuario.Atleta;
        if (usuarioComum && usuario.AtletaId.HasValue)
        {
            var atletaExistente = await atletaRepositorio.ObterPorIdAsync(usuario.AtletaId.Value, cancellationToken);
            if (atletaExistente is not null)
            {
                throw new RegraNegocioException("Este usuário já possui um atleta vinculado.");
            }

            usuario.AtletaId = null;
        }

        var dados = usuarioComum
            ? Normalizar(usuario.Nome, dto.Apelido, dto.Telefone, usuario.Email, dto.Instagram, dto.Cpf, dto.Bairro, dto.Cidade, dto.Estado)
            : Normalizar(dto.Nome, dto.Apelido, dto.Telefone, dto.Email, dto.Instagram, dto.Cpf, dto.Bairro, dto.Cidade, dto.Estado);
        var dataNascimento = Validar(dados.Nome, dados.Cpf, dto.Lado, dto.Nivel, dto.DataNascimento, dto.CadastroPendente, dados.PossuiIdentificador);
        await ValidarPerfilEsportivoAsync(dto, cancellationToken);

        var criandoMeuProprioAtleta = usuarioComum &&
            !usuario.AtletaId.HasValue &&
            !string.IsNullOrWhiteSpace(dados.Email) &&
            string.Equals(dados.Email, usuario.Email, StringComparison.OrdinalIgnoreCase);

        if (!criandoMeuProprioAtleta)
        {
            await GarantirEmailDisponivelAsync(dados.Email, null, cancellationToken);
        }

        Atleta atleta;
        if (criandoMeuProprioAtleta)
        {
            atleta = await resolvedorAtletaDuplaServico.ObterOuCriarAtletaParaUsuarioAsync(
                dados.Nome,
                dados.Email!,
                cancellationToken);
        }
        else
        {
            atleta = new Atleta();
            await atletaRepositorio.AdicionarAsync(atleta, cancellationToken);
        }

        atleta.Nome = dados.Nome;
        atleta.Apelido = dados.Apelido;
        atleta.Telefone = dados.Telefone;
        atleta.Email = dados.Email;
        atleta.Instagram = dados.Instagram;
        atleta.Cpf = dados.Cpf;
        atleta.Bairro = dados.Bairro;
        atleta.Cidade = dados.Cidade;
        atleta.Estado = dados.Estado;
        atleta.CadastroPendente = criandoMeuProprioAtleta ? false : dto.CadastroPendente;
        atleta.Sexo = dto.Sexo;
        atleta.Nivel = dto.Nivel;
        atleta.Lado = dto.Lado;
        atleta.DataNascimento = dataNascimento;
        atleta.PeDominante = dto.PeDominante;
        atleta.TempoPratica = dto.TempoPratica;
        atleta.ArenaPrincipalId = dto.ArenaPrincipalId;
        atleta.ObjetivoAtual = dto.ObjetivoAtual;
        atleta.UsuarioCriadorId ??= usuario.Id;
        atleta.AtualizarDataModificacao();

        if (usuarioComum || criandoMeuProprioAtleta)
        {
            usuario.AtletaId = atleta.Id;
            usuario.AtualizarDataModificacao();
            usuarioRepositorio.Atualizar(usuario);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        if (usuarioComum || criandoMeuProprioAtleta)
        {
            await pendenciaServico.SincronizarAposVinculoAtletaAsync(atleta.Id, cancellationToken);
        }
        return atleta.ParaDto();
    }

    public async Task<AtletaDto> SalvarMeuAsync(AtualizarAtletaDto dto, CancellationToken cancellationToken = default)
    {
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAtual.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        var dados = Normalizar(dto.Nome, dto.Apelido, dto.Telefone, usuario.Email, dto.Instagram, dto.Cpf, dto.Bairro, dto.Cidade, dto.Estado);
        var dataNascimento = Validar(dados.Nome, dados.Cpf, dto.Lado, dto.Nivel, dto.DataNascimento, false, dados.PossuiIdentificador);
        await ValidarPerfilEsportivoAsync(dto, cancellationToken);

        Atleta atleta;
        var atletaExistente = true;
        if (usuario.AtletaId.HasValue)
        {
            var atletaAtual = await atletaRepositorio.ObterPorIdParaAtualizacaoAsync(usuario.AtletaId.Value, cancellationToken);
            atleta = atletaAtual ?? new Atleta();
            if (atletaAtual is null)
            {
                atletaExistente = false;
                await atletaRepositorio.AdicionarAsync(atleta, cancellationToken);
            }
        }
        else
        {
            var atletaPorEmail = await ObterAtletaDisponivelParaVinculoAsync(usuario.Email, cancellationToken);
            if (atletaPorEmail is not null)
            {
                atleta = atletaPorEmail;
            }
            else
            {
                atleta = new Atleta();
                atletaExistente = false;
                await atletaRepositorio.AdicionarAsync(atleta, cancellationToken);
            }
        }

        atleta.Nome = dados.Nome;
        atleta.Apelido = dados.Apelido;
        atleta.Telefone = dados.Telefone;
        atleta.Email = usuario.Email;
        atleta.Instagram = dados.Instagram;
        atleta.Cpf = dados.Cpf;
        atleta.Bairro = dados.Bairro;
        atleta.Cidade = dados.Cidade;
        atleta.Estado = dados.Estado;
        atleta.CadastroPendente = false;
        atleta.Sexo = dto.Sexo;
        atleta.Nivel = dto.Nivel;
        atleta.Lado = dto.Lado;
        atleta.DataNascimento = dataNascimento;
        atleta.PeDominante = dto.PeDominante;
        atleta.TempoPratica = dto.TempoPratica;
        atleta.ArenaPrincipalId = dto.ArenaPrincipalId;
        atleta.ObjetivoAtual = dto.ObjetivoAtual;
        atleta.UsuarioCriadorId ??= usuario.Id;
        atleta.AtualizarDataModificacao();

        if (!atletaExistente)
        {
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        }

        usuario.AtletaId = atleta.Id;
        usuario.Atleta = atleta;
        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            usuario.Nome = dados.Nome;
        }

        usuario.AtualizarDataModificacao();
        atleta.Usuario = usuario;

        atletaRepositorio.Atualizar(atleta);
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        await SincronizarPendenciasAposMeuPerfilAsync(atleta.Id, cancellationToken);

        return atleta.ParaDto();
    }

    public async Task<AtletaDto> AtualizarAsync(Guid id, AtualizarAtletaDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var usuarioComum = usuario.Perfil == PerfilUsuario.Atleta;
        var atleta = await atletaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (atleta is null)
        {
            throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
        }

        var podeEditar = usuario.Perfil == PerfilUsuario.Administrador ||
            usuario.AtletaId == atleta.Id ||
            atleta.UsuarioCriadorId == usuario.Id;

        if (!podeEditar)
        {
            throw new AcessoNegadoException("Você só pode editar o próprio atleta ou atletas cadastrados por você.");
        }

        var dados = usuarioComum
            ? Normalizar(usuario.Nome, dto.Apelido, dto.Telefone, usuario.Email, dto.Instagram, dto.Cpf, dto.Bairro, dto.Cidade, dto.Estado)
            : Normalizar(dto.Nome, dto.Apelido, dto.Telefone, dto.Email, dto.Instagram, dto.Cpf, dto.Bairro, dto.Cidade, dto.Estado);
        var dataNascimento = Validar(dados.Nome, dados.Cpf, dto.Lado, dto.Nivel, dto.DataNascimento, dto.CadastroPendente, dados.PossuiIdentificador);
        await ValidarPerfilEsportivoAsync(dto, cancellationToken);
        await GarantirEmailDisponivelAsync(dados.Email, atleta.Id, cancellationToken);

        atleta.Nome = dados.Nome;
        atleta.Apelido = dados.Apelido;
        atleta.Telefone = dados.Telefone;
        atleta.Email = dados.Email;
        atleta.Instagram = dados.Instagram;
        atleta.Cpf = dados.Cpf;
        atleta.Bairro = dados.Bairro;
        atleta.Cidade = dados.Cidade;
        atleta.Estado = dados.Estado;
        atleta.CadastroPendente = dto.CadastroPendente;
        atleta.Sexo = dto.Sexo;
        atleta.Nivel = dto.Nivel;
        atleta.Lado = dto.Lado;
        atleta.DataNascimento = dataNascimento;
        atleta.PeDominante = dto.PeDominante;
        atleta.TempoPratica = dto.TempoPratica;
        atleta.ArenaPrincipalId = dto.ArenaPrincipalId;
        atleta.ObjetivoAtual = dto.ObjetivoAtual;
        atleta.AtualizarDataModificacao();

        atletaRepositorio.Atualizar(atleta);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return atleta.ParaDto();
    }

    public async Task<AtletaMedidasDto> AtualizarMinhasMedidasAsync(
        AtualizarAtletaMedidasDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            throw new RegraNegocioException("Crie ou complete o seu atleta antes de informar medidas.");
        }

        var atleta = await atletaRepositorio.ObterPorIdParaAtualizacaoAsync(usuario.AtletaId.Value, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Atleta não encontrado.");

        var medidas = atleta.Medidas;
        if (medidas is null)
        {
            medidas = new AtletaMedidas
            {
                AtletaId = atleta.Id
            };
            await atletaRepositorio.AdicionarMedidasAsync(medidas, cancellationToken);
        }

        AplicarMedidas(medidas, dto, atleta.Sexo);
        medidas.AtualizadoEm = DateTime.UtcNow;
        medidas.AtualizarDataModificacao();
        atleta.AtualizarDataModificacao();

        atletaRepositorio.AtualizarMedidas(medidas);
        atletaRepositorio.Atualizar(atleta);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return medidas.ParaDto();
    }

    public async Task<AtletaEmailDisponibilidadeDto> VerificarEmailAsync(
        string email,
        Guid? atletaIgnoradoId = null,
        CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmailOpcional(email);
        if (emailNormalizado is null)
        {
            return new AtletaEmailDisponibilidadeDto(string.Empty, true, null, null, null, null);
        }

        var atleta = await ObterAtletaComEmailAsync(emailNormalizado, atletaIgnoradoId, cancellationToken);
        return atleta is null
            ? new AtletaEmailDisponibilidadeDto(emailNormalizado, true, null, null, null, null)
            : new AtletaEmailDisponibilidadeDto(
                emailNormalizado,
                false,
                atleta.Id,
                atleta.Nome,
                atleta.Apelido,
                MontarMensagemEmailDuplicado(atleta));
    }

    public async Task<AtletaPendenciaDto> InformarEmailPendenteAsync(
        Guid atletaId,
        AtualizarEmailAtletaPendenteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (atletaId == Guid.Empty)
        {
            throw new RegraNegocioException("Atleta é obrigatório.");
        }

        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var emailNormalizado = NormalizarEmailPendente(dto.Email);
        var atleta = await atletaRepositorio.ObterPorIdAsync(atletaId, cancellationToken);
        if (atleta is null)
        {
            throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
        }

        var usuarioVinculado = await usuarioRepositorio.ObterPorAtletaIdAsync(atletaId, cancellationToken);
        if (usuarioVinculado is not null)
        {
            throw new RegraNegocioException("Este atleta já possui usuário vinculado.");
        }

        var podeEditar = await partidaRepositorio.ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(
            usuario.Id,
            atletaId,
            cancellationToken);
        if (!podeEditar)
        {
            throw new RegraNegocioException("Você só pode informar e-mail para atletas pendentes de partidas registradas por você.");
        }

        await GarantirEmailDisponivelAsync(emailNormalizado, atleta.Id, cancellationToken);

        atleta.Email = emailNormalizado;
        atleta.AtualizarDataModificacao();
        atletaRepositorio.Atualizar(atleta);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        await conviteCadastroServico.CriarParaPendenciaAtletaAsync(
            new CriarConvitePendenciaAtletaDto(
                emailNormalizado,
                atleta.Telefone,
                usuario.Id,
                atleta.Id,
                null),
            cancellationToken);

        return new AtletaPendenciaDto(
            atleta.Id,
            atleta.Nome,
            atleta.Apelido,
            atleta.Email,
            atleta.CadastroPendente,
            false,
            true,
            StatusCadastroAtletaUtil.ObterStatusPendencia(atleta),
            0,
            []);
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirAdministradorAsync(cancellationToken);

        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var atleta = await atletaRepositorio.ObterPorIdAsync(id, ct);
            if (atleta is null)
            {
                throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
            }

            var usuarioVinculado = await usuarioRepositorio.ObterPorAtletaIdParaAtualizacaoAsync(id, ct);
            var partidas = await partidaRepositorio.ListarPorAtletaParaRemocaoAsync(id, ct);
            var partidaIdsRemovidas = partidas.Select(x => x.Id).ToHashSet();
            await RemoverPartidasDoAtletaAsync(partidas, ct);
            await RemoverDuplasDoAtletaAsync(id, ct);
            await TratarGruposDoAtletaAsync(id, usuarioVinculado, partidaIdsRemovidas, ct);
            await RemoverAprovacoesRemanescentesDoAtletaAsync(id, ct);

            if (usuarioVinculado is not null)
            {
                usuarioVinculado.AtletaId = null;
                usuarioVinculado.Atleta = null;
                usuarioVinculado.AtualizarDataModificacao();
                usuarioRepositorio.Atualizar(usuarioVinculado);
            }

            atletaRepositorio.Remover(atleta);
            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
        }, cancellationToken);
    }

    private async Task RemoverPartidasDoAtletaAsync(
        IReadOnlyList<Partida> partidas,
        CancellationToken cancellationToken)
    {
        var partidaIds = partidas.Select(x => x.Id).ToHashSet();
        var partidasReferenciandoRemovidas = await partidaRepositorio.ListarReferenciandoPartidasAsync(partidaIds, cancellationToken);
        foreach (var partida in partidasReferenciandoRemovidas.Where(x => !partidaIds.Contains(x.Id)))
        {
            LimparReferenciasPartidasRemovidas(partida, partidaIds);
            partida.AtualizarDataModificacao();
            partidaRepositorio.Atualizar(partida);
        }

        foreach (var partida in partidas)
        {
            partidaRepositorio.Remover(partida);
        }
    }

    private async Task RemoverDuplasDoAtletaAsync(Guid atletaId, CancellationToken cancellationToken)
    {
        var duplas = await duplaRepositorio.ListarPorAtletaParaAtualizacaoAsync(atletaId, cancellationToken);
        var duplaIds = duplas.Select(x => x.Id).ToHashSet();
        var inscricoes = await inscricaoCampeonatoRepositorio.ListarPorDuplasParaAtualizacaoAsync(duplaIds, cancellationToken);
        foreach (var inscricao in inscricoes)
        {
            inscricaoCampeonatoRepositorio.Remover(inscricao);
        }

        foreach (var dupla in duplas)
        {
            duplaRepositorio.Remover(dupla);
        }
    }

    private async Task TratarGruposDoAtletaAsync(
        Guid atletaId,
        Usuario? usuarioVinculado,
        IReadOnlySet<Guid> partidaIdsRemovidas,
        CancellationToken cancellationToken)
    {
        var vinculosDoAtleta = await grupoAtletaRepositorio.ListarPorAtletaParaAtualizacaoAsync(atletaId, cancellationToken);
        var gruposRemovidos = new HashSet<Guid>();

        if (usuarioVinculado is not null)
        {
            var gruposCriados = await grupoRepositorio.ListarPorUsuarioOrganizadorParaAtualizacaoAsync(
                usuarioVinculado.Id,
                cancellationToken);
            foreach (var grupo in gruposCriados)
            {
                var vinculosGrupo = await grupoAtletaRepositorio.ListarPorGrupoParaAtualizacaoAsync(grupo.Id, cancellationToken);
                var outrosVinculos = vinculosGrupo
                    .Where(x => x.AtletaId != atletaId)
                    .ToList();

                var partidasRestantes = await ListarPartidasRestantesGrupoAsync(
                    grupo.Id,
                    partidaIdsRemovidas,
                    cancellationToken);

                if (outrosVinculos.Count == 0 && partidasRestantes.Count == 0)
                {
                    grupoRepositorio.Remover(grupo);
                    gruposRemovidos.Add(grupo.Id);
                    continue;
                }

                var novoProprietario = EscolherNovoProprietarioGrupo(outrosVinculos)
                    ?? await VincularEEscolherProprietarioPorPartidasRestantesAsync(
                        grupo.Id,
                        atletaId,
                        outrosVinculos,
                        partidasRestantes,
                        cancellationToken);
                if (novoProprietario is null)
                {
                    throw new RegraNegocioException("Não é possível excluir o atleta porque um grupo criado por ele ficaria sem proprietário.");
                }

                grupo.UsuarioOrganizadorId = novoProprietario.Id;
                grupo.UsuarioOrganizador = novoProprietario;
                grupo.AtualizarDataModificacao();
                grupoRepositorio.Atualizar(grupo);
            }
        }

        foreach (var vinculo in vinculosDoAtleta.Where(x => !gruposRemovidos.Contains(x.GrupoId)))
        {
            grupoAtletaRepositorio.Remover(vinculo);
        }
    }

    private async Task<IReadOnlyList<Partida>> ListarPartidasRestantesGrupoAsync(
        Guid grupoId,
        IReadOnlySet<Guid> partidaIdsRemovidas,
        CancellationToken cancellationToken)
    {
        var partidasGrupo = await partidaRepositorio.ListarPorGrupoAsync(grupoId, cancellationToken);
        return partidasGrupo
            .Where(x => !partidaIdsRemovidas.Contains(x.Id))
            .ToList();
    }

    private async Task<Usuario?> VincularEEscolherProprietarioPorPartidasRestantesAsync(
        Guid grupoId,
        Guid atletaIdRemovido,
        IReadOnlyList<GrupoAtleta> vinculosExistentes,
        IReadOnlyList<Partida> partidasRestantes,
        CancellationToken cancellationToken)
    {
        var vinculosExistentesIds = vinculosExistentes
            .Select(x => x.AtletaId)
            .ToHashSet();
        var candidato = EnumerarAtletasPartidas(partidasRestantes)
            .Where(x => x.Id != atletaIdRemovido)
            .Where(x => x.Usuario is not null && x.Usuario.Ativo && !x.Usuario.DadosAnonimizados)
            .DistinctBy(x => x.Id)
            .OrderBy(x => PrioridadeProprietarioGrupo(x.Usuario!.Perfil))
            .ThenBy(x => x.DataCriacao)
            .FirstOrDefault();

        if (candidato is null)
        {
            return null;
        }

        if (!vinculosExistentesIds.Contains(candidato.Id))
        {
            await grupoAtletaRepositorio.AdicionarAsync(new GrupoAtleta
            {
                GrupoId = grupoId,
                AtletaId = candidato.Id
            }, cancellationToken);
        }

        return candidato.Usuario;
    }

    private async Task RemoverAprovacoesRemanescentesDoAtletaAsync(Guid atletaId, CancellationToken cancellationToken)
    {
        var aprovacoes = await partidaAprovacaoRepositorio.ListarPorAtletaAsync(atletaId, cancellationToken);
        if (aprovacoes.Count > 0)
        {
            partidaAprovacaoRepositorio.RemoverIntervalo(aprovacoes);
        }
    }

    private static Usuario? EscolherNovoProprietarioGrupo(IReadOnlyList<GrupoAtleta> vinculos)
    {
        return vinculos
            .Where(x => x.Atleta.Usuario is not null && x.Atleta.Usuario.Ativo && !x.Atleta.Usuario.DadosAnonimizados)
            .OrderBy(x => PrioridadeProprietarioGrupo(x.Atleta.Usuario!.Perfil))
            .ThenBy(x => x.DataCriacao)
            .Select(x => x.Atleta.Usuario)
            .FirstOrDefault();
    }

    private static int PrioridadeProprietarioGrupo(PerfilUsuario perfil)
    {
        return perfil switch
        {
            PerfilUsuario.Administrador => 0,
            PerfilUsuario.Organizador => 1,
            _ => 2
        };
    }

    private static IEnumerable<Atleta> EnumerarAtletasPartidas(IEnumerable<Partida> partidas)
    {
        return partidas.SelectMany(partida => new[]
        {
            partida.DuplaA?.Atleta1,
            partida.DuplaA?.Atleta2,
            partida.DuplaB?.Atleta1,
            partida.DuplaB?.Atleta2
        }).OfType<Atleta>();
    }

    private static void LimparReferenciasPartidasRemovidas(Partida partida, IReadOnlySet<Guid> partidaIds)
    {
        if (partida.PartidaOrigemParticipanteAId.HasValue && partidaIds.Contains(partida.PartidaOrigemParticipanteAId.Value))
        {
            partida.PartidaOrigemParticipanteAId = null;
            partida.OrigemParticipanteATipo = null;
            partida.DuplaAId = null;
            partida.DuplaA = null;
        }

        if (partida.PartidaOrigemParticipanteBId.HasValue && partidaIds.Contains(partida.PartidaOrigemParticipanteBId.Value))
        {
            partida.PartidaOrigemParticipanteBId = null;
            partida.OrigemParticipanteBTipo = null;
            partida.DuplaBId = null;
            partida.DuplaB = null;
        }

        if (partida.ProximaPartidaVencedorId.HasValue && partidaIds.Contains(partida.ProximaPartidaVencedorId.Value))
        {
            partida.ProximaPartidaVencedorId = null;
            partida.SlotDestinoVencedor = null;
        }

        if (partida.ProximaPartidaPerdedorId.HasValue && partidaIds.Contains(partida.ProximaPartidaPerdedorId.Value))
        {
            partida.ProximaPartidaPerdedorId = null;
            partida.SlotDestinoPerdedor = null;
        }
    }

    private async Task GarantirAcessoOrganizadorAsync(
        Guid atletaId,
        Guid usuarioOrganizadorId,
        CancellationToken cancellationToken)
    {
        var pertenceAoOrganizador = await atletaRepositorio.PertenceAoOrganizadorAsync(atletaId, usuarioOrganizadorId, cancellationToken);
        if (!pertenceAoOrganizador)
        {
            throw new RegraNegocioException("O organizador só pode alterar atletas inscritos em competições vinculadas ao próprio usuário.");
        }
    }

    private static (
        string Nome,
        string? Apelido,
        string? Telefone,
        string? Email,
        string? Instagram,
        string? Cpf,
        string? Bairro,
        string? Cidade,
        string? Estado,
        bool PossuiIdentificador
    ) Normalizar(
        string nome,
        string? apelido,
        string? telefone,
        string? email,
        string? instagram,
        string? cpf,
        string? bairro,
        string? cidade,
        string? estado)
    {
        var nomeNormalizado = NormalizadorNomeAtleta.NormalizarTexto(nome);
        var apelidoNormalizado = NormalizadorNomeAtleta.NormalizarTexto(apelido);
        var telefoneNormalizado = NormalizadorNomeAtleta.NormalizarTexto(telefone);
        var emailNormalizado = NormalizadorNomeAtleta.NormalizarTexto(email).ToLowerInvariant();
        var instagramNormalizado = NormalizadorNomeAtleta.NormalizarTexto(instagram);
        var cpfNormalizado = ValidadorCpf.Normalizar(cpf);
        var bairroNormalizado = NormalizadorNomeAtleta.NormalizarTexto(bairro);
        var cidadeNormalizada = NormalizadorNomeAtleta.NormalizarTexto(cidade);
        var estadoNormalizado = NormalizadorNomeAtleta.NormalizarTexto(estado);

        return (
            nomeNormalizado,
            string.IsNullOrWhiteSpace(apelidoNormalizado) ? null : apelidoNormalizado,
            string.IsNullOrWhiteSpace(telefoneNormalizado) ? null : telefoneNormalizado,
            string.IsNullOrWhiteSpace(emailNormalizado) ? null : emailNormalizado,
            string.IsNullOrWhiteSpace(instagramNormalizado) ? null : instagramNormalizado,
            string.IsNullOrWhiteSpace(cpfNormalizado) ? null : cpfNormalizado,
            string.IsNullOrWhiteSpace(bairroNormalizado) ? null : bairroNormalizado,
            string.IsNullOrWhiteSpace(cidadeNormalizada) ? null : cidadeNormalizada,
            string.IsNullOrWhiteSpace(estadoNormalizado) ? null : estadoNormalizado,
            !string.IsNullOrWhiteSpace(telefoneNormalizado)
                || !string.IsNullOrWhiteSpace(emailNormalizado)
                || !string.IsNullOrWhiteSpace(instagramNormalizado)
                || !string.IsNullOrWhiteSpace(cpfNormalizado)
        );
    }

    private async Task GarantirEmailDisponivelAsync(
        string? email,
        Guid? atletaIgnoradoId,
        CancellationToken cancellationToken)
    {
        var emailNormalizado = NormalizarEmailOpcional(email);
        if (emailNormalizado is null)
        {
            return;
        }

        var atleta = await ObterAtletaComEmailAsync(emailNormalizado, atletaIgnoradoId, cancellationToken);
        if (atleta is not null)
        {
            throw new RegraNegocioException(MontarMensagemEmailDuplicado(atleta));
        }
    }

    private async Task<Atleta?> ObterAtletaComEmailAsync(
        string emailNormalizado,
        Guid? atletaIgnoradoId,
        CancellationToken cancellationToken)
    {
        var atletas = await atletaRepositorio.ListarPorEmailAsync(emailNormalizado, cancellationToken);
        return atletas.FirstOrDefault(x => !atletaIgnoradoId.HasValue || x.Id != atletaIgnoradoId.Value);
    }

    private async Task<Atleta?> ObterAtletaDisponivelParaVinculoAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var emailNormalizado = NormalizarEmailOpcional(email);
        if (emailNormalizado is null)
        {
            return null;
        }

        var atletas = await atletaRepositorio.ListarPorEmailAsync(emailNormalizado, cancellationToken);
        if (atletas.Count == 0)
        {
            return null;
        }

        var atletasDisponiveis = atletas.Where(x => x.Usuario is null).ToList();
        if (atletasDisponiveis.Count == 1)
        {
            return atletasDisponiveis[0];
        }

        throw new RegraNegocioException(MontarMensagemEmailDuplicado(atletas[0]));
    }

    private static string? NormalizarEmailOpcional(string? email)
    {
        var emailNormalizado = NormalizadorNomeAtleta.NormalizarTexto(email).ToLowerInvariant();
        return string.IsNullOrWhiteSpace(emailNormalizado) ? null : emailNormalizado;
    }

    private static string MontarMensagemEmailDuplicado(Atleta atleta)
    {
        return "Já existe um atleta cadastrado com este e-mail.\n\n" +
               "Atleta encontrado:\n\n" +
               $"* Nome: {atleta.Nome}\n" +
               $"* Apelido: {atleta.Apelido ?? "Não informado"}\n\n" +
               "Utilize outro e-mail ou edite o cadastro existente.";
    }

    private async Task ValidarPerfilEsportivoAsync(AtualizarAtletaDto dto, CancellationToken cancellationToken)
    {
        ValidarEnumOpcional(dto.Sexo, "Sexo/gênero inválido.");
        ValidarEnumOpcional(dto.PeDominante, "Pé dominante inválido.");
        ValidarEnumOpcional(dto.TempoPratica, "Tempo de prática inválido.");
        ValidarEnumOpcional(dto.ObjetivoAtual, "Objetivo atual inválido.");

        if (dto.ArenaPrincipalId.HasValue)
        {
            var arena = await arenaRepositorio.ObterPorIdAsync(dto.ArenaPrincipalId.Value, cancellationToken);
            if (arena is null)
            {
                throw new RegraNegocioException("Arena principal não encontrada.");
            }
        }
    }

    private async Task ValidarPerfilEsportivoAsync(CriarAtletaDto dto, CancellationToken cancellationToken)
    {
        await ValidarPerfilEsportivoAsync(
            new AtualizarAtletaDto(
                dto.Nome,
                dto.Apelido,
                dto.Telefone,
                dto.Email,
                dto.Instagram,
                dto.Cpf,
                dto.Bairro,
                dto.Cidade,
                dto.Estado,
                dto.CadastroPendente,
                dto.Nivel,
                dto.Lado,
                dto.DataNascimento,
                dto.Sexo,
                dto.PeDominante,
                dto.TempoPratica,
                dto.ArenaPrincipalId,
                dto.ObjetivoAtual),
            cancellationToken);
    }

    private static void ValidarEnumOpcional<TEnum>(TEnum? valor, string mensagem)
        where TEnum : struct, Enum
    {
        if (valor.HasValue && !Enum.IsDefined(valor.Value))
        {
            throw new RegraNegocioException(mensagem);
        }
    }

    private static void AplicarMedidas(AtletaMedidas medidas, AtualizarAtletaMedidasDto dto, SexoAtleta? sexo)
    {
        medidas.Camiseta = NormalizarTamanhoOpcional(dto.Camiseta, TamanhosRoupa, "Tamanho de camiseta inválido.");
        medidas.Regata = NormalizarTamanhoOpcional(dto.Regata, TamanhosRoupa, "Tamanho de regata inválido.");
        medidas.Short = NormalizarTamanhoOpcional(dto.Short, TamanhosShort, "Tamanho de short inválido.");

        medidas.Sunga = sexo == SexoAtleta.Masculino
            ? NormalizarTamanhoOpcional(dto.Sunga, TamanhosRoupa, "Tamanho de sunga inválido.")
            : null;
        medidas.Top = sexo == SexoAtleta.Feminino
            ? NormalizarTamanhoOpcional(dto.Top, TamanhosRoupa, "Tamanho de top inválido.")
            : null;
        medidas.Biquini = sexo == SexoAtleta.Feminino
            ? NormalizarTamanhoOpcional(dto.Biquini, TamanhosRoupa, "Tamanho de biquíni inválido.")
            : null;
    }

    private static string? NormalizarTamanhoOpcional(
        string? valor,
        IReadOnlySet<string> opcoesValidas,
        string mensagemErro)
    {
        var normalizado = NormalizadorNomeAtleta.NormalizarTexto(valor).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            return null;
        }

        if (!opcoesValidas.Contains(normalizado))
        {
            throw new RegraNegocioException(mensagemErro);
        }

        return normalizado;
    }

    private static readonly IReadOnlySet<string> TamanhosRoupa = new HashSet<string>
    {
        "PP",
        "P",
        "M",
        "G",
        "GG",
        "XGG"
    };

    private static readonly IReadOnlySet<string> TamanhosShort = new HashSet<string>
    {
        "36",
        "38",
        "40",
        "42",
        "44",
        "46",
        "48",
        "50"
    };

    private static DateTime? Validar(
        string nome,
        string? cpf,
        LadoAtleta lado,
        NivelAtleta? nivel,
        DateTime? dataNascimento,
        bool cadastroPendente,
        bool possuiIdentificador)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome do atleta é obrigatório.");
        }

        if (!cadastroPendente && !possuiIdentificador)
        {
            throw new RegraNegocioException("Informe ao menos um identificador do atleta: telefone, e-mail, Instagram ou CPF.");
        }

        if (!string.IsNullOrWhiteSpace(cpf) && !ValidadorCpf.EhValido(cpf))
        {
            throw new RegraNegocioException("CPF inválido.");
        }

        if (!Enum.IsDefined(lado))
        {
            throw new RegraNegocioException("Lado do atleta inválido.");
        }

        if (nivel.HasValue && !Enum.IsDefined(nivel.Value))
        {
            throw new RegraNegocioException("Nível do atleta inválido.");
        }

        if (!dataNascimento.HasValue)
        {
            return null;
        }

        var dataNormalizada = dataNascimento.Value.Date;
        if (dataNormalizada > DateTime.UtcNow.Date)
        {
            throw new RegraNegocioException("Data de nascimento não pode ser futura.");
        }

        return dataNormalizada;
    }

    private static IEnumerable<(Atleta Atleta, Guid PartidaId, string NomeCompeticao)> EnumerarPendencias(Partida partida)
    {
        var nomeCompeticao = partida.CategoriaCompeticao?.Competicao?.Nome ?? partida.Grupo?.Nome ?? "Grupo";

        return new[]
        {
            partida.DuplaA?.Atleta1,
            partida.DuplaA?.Atleta2,
            partida.DuplaB?.Atleta1,
            partida.DuplaB?.Atleta2
        }
        .OfType<Atleta>()
        .Where(atleta => !StatusCadastroAtletaUtil.PossuiUsuarioVinculado(atleta))
        .DistinctBy(atleta => atleta.Id)
        .Select(atleta => (atleta, partida.Id, nomeCompeticao));
    }

    private static string NormalizarEmailPendente(string email)
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

    private async Task SincronizarPendenciasAposMeuPerfilAsync(Guid atletaId, CancellationToken cancellationToken)
    {
        try
        {
            await pendenciaServico.SincronizarAposVinculoAtletaAsync(atletaId, cancellationToken);
        }
        catch (Exception)
        {
            return;
        }
    }
}
