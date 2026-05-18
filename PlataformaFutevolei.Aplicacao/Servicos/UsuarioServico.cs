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

public class UsuarioServico(
    IUsuarioRepositorio usuarioRepositorio,
    IAtletaRepositorio atletaRepositorio,
    IConviteCadastroRepositorio conviteCadastroRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    IPendenciaUsuarioRepositorio pendenciaUsuarioRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IPendenciaServico pendenciaServico
) : IUsuarioServico
{
    public async Task<UsuarioLogadoDto> ObterMeuUsuarioAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        return usuario.ParaDto();
    }

    public async Task<UsuarioResumoDto> ObterMeuResumoAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            return new UsuarioResumoDto(string.Empty,0, 0, 0, 0, 0, 0, 0);
        }

        return await partidaRepositorio.ObterResumoUsuarioPorAtletaAsync(usuario.AtletaId.Value, cancellationToken);
    }

    public async Task<UsuarioLogadoDto> AtualizarMeuUsuarioAsync(
        AtualizarMeuUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new RegraNegocioException("Nome é obrigatório.");
        }

        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAtual.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");

        usuario.Nome = dto.Nome.Trim();
        if (usuario.Perfil == PerfilUsuario.Atleta && usuario.Atleta is not null)
        {
            var (nomeAtleta, apelidoAtleta) = NormalizadorNomeAtleta.NormalizarNomeEApelido(usuario.Nome, null);
            usuario.Atleta.Nome = nomeAtleta;
            usuario.Atleta.Apelido = apelidoAtleta;
            usuario.Atleta.Email = usuario.Email;
            usuario.Atleta.AtualizarDataModificacao();
        }

        usuario.AtualizarDataModificacao();

        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var atualizado = await usuarioRepositorio.ObterPorIdAsync(usuario.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        return atualizado.ParaDto();
    }

    public async Task<UsuarioLogadoDto> VincularMeuAtletaAsync(
        VincularAtletaUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.AtletaId == Guid.Empty)
        {
            throw new RegraNegocioException("Atleta é obrigatório.");
        }

        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuarioAtual.Perfil == PerfilUsuario.Atleta)
        {
            throw new RegraNegocioException("Usuário comum só pode criar o próprio atleta pelo Meu Perfil.");
        }

        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAtual.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        var atleta = await atletaRepositorio.ObterPorIdAsync(dto.AtletaId, cancellationToken);
        if (atleta is null)
        {
            throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
        }

        var usuarioExistente = await usuarioRepositorio.ObterPorAtletaIdAsync(dto.AtletaId, cancellationToken);
        if (usuarioExistente is not null && usuarioExistente.Id != usuario.Id)
        {
            throw new RegraNegocioException("Este atleta já está vinculado a outro usuário.");
        }

        if (usuario.Atleta is not null && usuario.Atleta.Id != atleta.Id)
        {
            usuario.Atleta.Usuario = null;
        }

        usuario.AtletaId = atleta.Id;
        usuario.Atleta = atleta;
        atleta.Usuario = usuario;
        atleta.Email = usuario.Email;
        atleta.CadastroPendente = false;
        atleta.AtualizarDataModificacao();

        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await SalvarAlteracoesUsuarioAtletaAsync(cancellationToken);
        await SincronizarPendenciasAposVinculoAsync(atleta.Id, cancellationToken);

        var atualizado = await usuarioRepositorio.ObterPorIdAsync(usuario.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        return atualizado.ParaDto();
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(
        string? nome,
        string? email,
        CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirAdministradorAsync(cancellationToken);
        var usuarios = await usuarioRepositorio.ListarAsync(nome, email, cancellationToken);
        return usuarios.Select(x => x.ParaAdminDto()).ToList();
    }

    public async Task<UsuarioDto> AtualizarAsync(
        Guid id,
        AtualizarUsuarioDto dto,
        CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirAdministradorAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new RegraNegocioException("Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        if (!Enum.IsDefined(dto.Perfil))
        {
            throw new RegraNegocioException("Perfil inválido.");
        }

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(id, cancellationToken);
        if (usuario is null || usuario.DadosAnonimizados)
        {
            throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        }

        await ValidarManutencaoAdministradorAtivoAsync(usuario, dto.Perfil, dto.Ativo, cancellationToken);

        var usuarioMesmoEmail = await usuarioRepositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (usuarioMesmoEmail is not null && usuarioMesmoEmail.Id != usuario.Id)
        {
            throw new RegraNegocioException("Já existe um usuário cadastrado com este e-mail.");
        }

        if (dto.AtletaId.HasValue)
        {
            var atleta = await atletaRepositorio.ObterPorIdAsync(dto.AtletaId.Value, cancellationToken);
            if (atleta is null)
            {
                throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
            }

            var usuarioComAtleta = await usuarioRepositorio.ObterPorAtletaIdAsync(dto.AtletaId.Value, cancellationToken);
            if (usuarioComAtleta is not null && usuarioComAtleta.Id != usuario.Id)
            {
                throw new RegraNegocioException("Este atleta já está vinculado a outro usuário.");
            }
        }

        usuario.Nome = dto.Nome.Trim();
        usuario.Email = emailNormalizado;
        usuario.Perfil = dto.Perfil;
        usuario.Ativo = dto.Ativo;
        usuario.AtletaId = dto.AtletaId;
        usuario.AtualizarDataModificacao();

        if (dto.AtletaId.HasValue)
        {
            var atleta = await atletaRepositorio.ObterPorIdAsync(dto.AtletaId.Value, cancellationToken)
                ?? throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
            if (usuario.Atleta is not null && usuario.Atleta.Id != atleta.Id)
            {
                usuario.Atleta.Usuario = null;
            }

            usuario.Atleta = atleta;
            atleta.Usuario = usuario;
            atleta.Email = emailNormalizado;
            atleta.CadastroPendente = false;
            atleta.AtualizarDataModificacao();
        }
        else if (usuario.Atleta is not null)
        {
            usuario.Atleta.Usuario = null;
            usuario.Atleta = null;
        }

        usuarioRepositorio.Atualizar(usuario);
        await SalvarAlteracoesUsuarioAtletaAsync(cancellationToken);
        if (dto.AtletaId.HasValue)
        {
            await SincronizarPendenciasAposVinculoAsync(dto.AtletaId.Value, cancellationToken);
        }

        var atualizado = await usuarioRepositorio.ObterPorIdAsync(usuario.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        return atualizado.ParaAdminDto();
    }

    public async Task ExcluirPorAdministradorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var executor = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (executor.Perfil != PerfilUsuario.Administrador)
        {
            throw new RegraNegocioException("Apenas administradores podem executar esta operação.");
        }

        if (executor.Id == id)
        {
            throw new RegraNegocioException("O administrador não pode excluir a própria conta por esta tela.");
        }

        await ExcluirUsuarioAsync(id, executor.Id, OrigemExclusaoUsuario.Admin, cancellationToken);
    }

    public async Task ExcluirMeuPerfilAsync(CancellationToken cancellationToken = default)
    {
        var executor = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await ExcluirUsuarioAsync(executor.Id, executor.Id, OrigemExclusaoUsuario.ProprioUsuario, cancellationToken);
    }

    private async Task ExcluirUsuarioAsync(
        Guid usuarioAlvoId,
        Guid usuarioExecutorId,
        OrigemExclusaoUsuario origem,
        CancellationToken cancellationToken)
    {
        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAlvoId, ct);
            if (usuario is null || usuario.DadosAnonimizados)
            {
                throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
            }

            if (!usuario.Ativo)
            {
                throw new RegraNegocioException("Usuário já está inativo.");
            }

            if (origem == OrigemExclusaoUsuario.Admin && usuarioExecutorId == usuario.Id)
            {
                throw new RegraNegocioException("O administrador não pode excluir a própria conta por esta tela.");
            }

            await ValidarPodeExcluirAdministradorAsync(usuario, ct);

            var agora = DateTime.UtcNow;
            var emailOriginal = usuario.Email;
            var atleta = usuario.Atleta;

            await CancelarPendenciasAsync(usuario.Id, agora, ct);
            await DesativarConvitesAsync(usuario.Id, emailOriginal, ct);
            await RemoverVinculosGrupoAsync(atleta?.Id, ct);
            AnonimizarAtleta(atleta);
            AnonimizarUsuario(usuario, usuarioExecutorId, agora);

            usuarioRepositorio.Atualizar(usuario);
            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
        }, cancellationToken);
    }

    private async Task ValidarPodeExcluirAdministradorAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        if (usuario.Perfil != PerfilUsuario.Administrador)
        {
            return;
        }

        var totalAdministradoresAtivos = await usuarioRepositorio.ContarAdministradoresAtivosAsync(cancellationToken);
        if (totalAdministradoresAtivos <= 1)
        {
            throw new RegraNegocioException(
                "Não é possível excluir esta conta porque ela é o último administrador ativo da plataforma.");
        }
    }

    private async Task ValidarManutencaoAdministradorAtivoAsync(
        Usuario usuario,
        PerfilUsuario proximoPerfil,
        bool proximoAtivo,
        CancellationToken cancellationToken)
    {
        var desativaAdministradorAtivo = usuario.Perfil == PerfilUsuario.Administrador &&
            usuario.Ativo &&
            (proximoPerfil != PerfilUsuario.Administrador || !proximoAtivo);

        if (!desativaAdministradorAtivo)
        {
            return;
        }

        var totalAdministradoresAtivos = await usuarioRepositorio.ContarAdministradoresAtivosAsync(cancellationToken);
        if (totalAdministradoresAtivos <= 1)
        {
            throw new RegraNegocioException(
                "Não é possível excluir esta conta porque ela é o último administrador ativo da plataforma.");
        }
    }

    private async Task CancelarPendenciasAsync(Guid usuarioId, DateTime agora, CancellationToken cancellationToken)
    {
        var pendencias = await pendenciaUsuarioRepositorio.ListarPendentesPorUsuarioParaAtualizacaoAsync(usuarioId, cancellationToken);
        foreach (var pendencia in pendencias)
        {
            pendencia.Status = StatusPendenciaUsuario.Cancelada;
            pendencia.DataConclusao = agora;
            pendencia.Observacao = "Cancelada pela exclusão do usuário.";
            pendencia.AtualizarDataModificacao();
            pendenciaUsuarioRepositorio.Atualizar(pendencia);
        }
    }

    private async Task DesativarConvitesAsync(Guid usuarioId, string email, CancellationToken cancellationToken)
    {
        var convites = await conviteCadastroRepositorio.ListarAtivosPorUsuarioOuEmailAsync(usuarioId, email, cancellationToken);
        foreach (var convite in convites)
        {
            convite.Desativar();
            convite.CodigoConvite = null;
            convite.CodigoConviteHash = null;
            conviteCadastroRepositorio.Atualizar(convite);
        }
    }

    private async Task RemoverVinculosGrupoAsync(Guid? atletaId, CancellationToken cancellationToken)
    {
        if (!atletaId.HasValue)
        {
            return;
        }

        var vinculos = await grupoAtletaRepositorio.ListarPorAtletaParaAtualizacaoAsync(atletaId.Value, cancellationToken);
        foreach (var vinculo in vinculos)
        {
            grupoAtletaRepositorio.Remover(vinculo);
        }
    }

    private static void AnonimizarUsuario(Usuario usuario, Guid usuarioExecutorId, DateTime agora)
    {
        usuario.Nome = "Usuário excluído";
        usuario.Email = $"usuario-excluido-{usuario.Id:N}@excluido.local";
        usuario.SenhaHash = string.Empty;
        usuario.CodigoLoginHash = null;
        usuario.CodigoLoginExpiraEmUtc = null;
        usuario.CodigoRedefinicaoSenhaHash = null;
        usuario.CodigoRedefinicaoSenhaExpiraEmUtc = null;
        usuario.RefreshTokenHash = null;
        usuario.RefreshTokenExpiraEmUtc = null;
        usuario.Ativo = false;
        usuario.DadosAnonimizados = true;
        usuario.PerfilPublico = false;
        usuario.ExibirEmail = false;
        usuario.PermitirUsoLocalizacao = false;
        usuario.PermitirUsoImagem = false;
        usuario.ExclusaoSolicitadaEmUtc ??= agora;
        usuario.ExcluidoEm = agora;
        usuario.ExcluidoPorUsuarioId = usuarioExecutorId;
        usuario.AtletaId = null;
        usuario.Atleta = null;
        usuario.AtualizarDataModificacao();
    }

    private static void AnonimizarAtleta(Atleta? atleta)
    {
        if (atleta is null)
        {
            return;
        }

        atleta.Nome = "Usuário excluído";
        atleta.Apelido = "Usuário excluído";
        atleta.Telefone = null;
        atleta.Email = null;
        atleta.Instagram = null;
        atleta.Cpf = null;
        atleta.Bairro = null;
        atleta.Cidade = null;
        atleta.Estado = null;
        atleta.CadastroPendente = false;
        atleta.Nivel = null;
        atleta.DataNascimento = null;
        atleta.Usuario = null;
        atleta.AtualizarDataModificacao();
    }

    private async Task SalvarAlteracoesUsuarioAtletaAsync(CancellationToken cancellationToken)
    {
        try
        {
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        }
        catch (Exception ex) when (EhViolacaoUnicidade(ex))
        {
            throw new RegraNegocioException("Este atleta já está vinculado a outro usuário.");
        }
    }

    private async Task SincronizarPendenciasAposVinculoAsync(Guid atletaId, CancellationToken cancellationToken)
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

    private static bool EhViolacaoUnicidade(Exception ex)
    {
        Exception? atual = ex;
        while (atual is not null)
        {
            var tipo = atual.GetType();
            var sqlState = tipo.GetProperty("SqlState")?.GetValue(atual)?.ToString();
            if (string.Equals(sqlState, "23505", StringComparison.Ordinal))
            {
                return true;
            }

            atual = atual.InnerException;
        }

        return false;
    }

    private enum OrigemExclusaoUsuario
    {
        Admin,
        ProprioUsuario
    }
}
