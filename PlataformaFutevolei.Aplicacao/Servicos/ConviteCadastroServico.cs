using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
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

public class ConviteCadastroServico(
    IConviteCadastroRepositorio conviteCadastroRepositorio,
    IAtletaRepositorio atletaRepositorio,
    IUsuarioRepositorio usuarioRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IGeracaoLinkConviteCadastroServico geracaoLinkConviteCadastroServico,
    IEnvioEmailConviteCadastroServico envioEmailConviteCadastroServico,
    IEnvioWhatsappConviteCadastroServico envioWhatsappConviteCadastroServico,
    ILogger<ConviteCadastroServico> logger
) : IConviteCadastroServico
{
    private static readonly TimeSpan ValidadePadrao = TimeSpan.FromDays(7);
    private const PerfilUsuario PerfilDestinoPadraoConvite = PerfilUsuario.Atleta;
    private const string CanalEmail = "e-mail";

    public async Task<IReadOnlyList<ConviteCadastroDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await GarantirAdministradorAsync(cancellationToken);
        var convites = await conviteCadastroRepositorio.ListarAsync(cancellationToken);
        return convites.Select(x => x.ParaDto()).ToList();
    }

    public async Task<IReadOnlyList<AtletaElegivelConviteCadastroDto>> ListarAtletasElegiveisAsync(CancellationToken cancellationToken = default)
    {
        await GarantirAdministradorAsync(cancellationToken);

        var atletas = await atletaRepositorio.ListarComEmailEmPartidasSemUsuarioAsync(cancellationToken);
        var agoraUtc = DateTime.UtcNow;
        var emailsComConviteAtivo = (await conviteCadastroRepositorio.ListarAsync(cancellationToken))
            .Where(x => x.PodeSerUsado(agoraUtc))
            .Select(x => x.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return atletas
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .Where(x => !emailsComConviteAtivo.Contains(x.Email!))
            .Select(x => new AtletaElegivelConviteCadastroDto(
                x.Id,
                x.Nome,
                x.Apelido,
                x.Email!,
                x.Telefone))
            .ToList();
    }

    public async Task<ConviteCadastroDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GarantirAdministradorAsync(cancellationToken);
        var convite = await conviteCadastroRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (convite is null)
        {
            throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");
        }

        return convite.ParaDto();
    }

    public async Task<ConviteCadastroLinkAceiteDto> ObterLinkAceiteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GarantirAdministradorAsync(cancellationToken);
        var convite = await conviteCadastroRepositorio.ObterPorIdParaAtualizacaoAsync(id, cancellationToken);
        if (convite is null)
        {
            throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");
        }

        ValidarConviteParaGeracaoAcesso(convite);
        var codigoConvite = await ObterOuGerarCodigoConviteAsync(convite, cancellationToken);

        return new ConviteCadastroLinkAceiteDto(
            geracaoLinkConviteCadastroServico.Gerar(convite),
            codigoConvite);
    }

    public async Task<ConviteCadastroPublicoDto> ObterPublicoAsync(string identificadorPublico, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
        {
            throw new RegraNegocioException("Identificador do convite é obrigatório.");
        }

        var convite = await conviteCadastroRepositorio.ObterPorIdentificadorPublicoAsync(
            identificadorPublico.Trim(),
            cancellationToken);
        if (convite is null)
        {
            throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");
        }

        return convite.ParaPublicoDto();
    }

    public async Task<ConviteCadastroDto> CriarAsync(CriarConviteCadastroDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await GarantirAdministradorAsync(cancellationToken);
        var emailNormalizado = NormalizarEmail(dto.Email);

        var usuarioExistente = await usuarioRepositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (usuarioExistente is not null)
        {
            throw new RegraNegocioException("Já existe um usuário cadastrado com este e-mail.");
        }

        var telefoneNormalizado = NormalizarTelefone(dto.Telefone);
        ValidarCanalEnvio(dto.CanalEnvio, telefoneNormalizado);
        var expiraEmUtc = NormalizarExpiracao(dto.ExpiraEmUtc);
        var convite = new ConviteCadastro
        {
            Email = emailNormalizado,
            Telefone = telefoneNormalizado,
            IdentificadorPublico = await GerarIdentificadorPublicoUnicoAsync(cancellationToken),
            PerfilDestino = PerfilDestinoPadraoConvite,
            ExpiraEmUtc = expiraEmUtc,
            Ativo = true,
            CriadoPorUsuarioId = usuario.Id,
            CanalEnvio = NormalizarTexto(dto.CanalEnvio)
        };
        DefinirNovoCodigoConvite(convite);

        await conviteCadastroRepositorio.AdicionarAsync(convite, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var conviteCriado = await conviteCadastroRepositorio.ObterPorIdParaAtualizacaoAsync(convite.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");

        await TentarEnviarEmailAutomaticoAsync(conviteCriado, cancellationToken);
        await TentarEnviarWhatsappAutomaticoAsync(conviteCriado, cancellationToken);

        return conviteCriado.ParaDto();
    }

    public async Task<ConvitePendenciaAtletaResultadoDto> CriarParaPendenciaAtletaAsync(
        CriarConvitePendenciaAtletaDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var emailNormalizado = NormalizarEmail(dto.Email);
            var usuarioExistente = await usuarioRepositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken);
            if (usuarioExistente is not null)
            {
                logger.LogInformation(
                    "Convite de pendência ignorado porque já existe usuário para o e-mail. UsuarioResolvedorId: {UsuarioResolvedorId}. AtletaId: {AtletaId}. PartidaId: {PartidaId}. UsuarioExistenteId: {UsuarioExistenteId}.",
                    dto.UsuarioResolvedorId,
                    dto.AtletaId,
                    dto.PartidaId,
                    usuarioExistente.Id);
                return new ConvitePendenciaAtletaResultadoDto(false, false, true, null);
            }

            var agoraUtc = DateTime.UtcNow;
            var conviteExistente = await conviteCadastroRepositorio.ObterAtivoPendentePorEmailAsync(
                emailNormalizado,
                agoraUtc,
                cancellationToken);
            if (conviteExistente is not null)
            {
                logger.LogInformation(
                    "Convite de pendência ignorado porque já existe convite ativo para o e-mail. ConviteId: {ConviteId}. UsuarioResolvedorId: {UsuarioResolvedorId}. AtletaId: {AtletaId}. PartidaId: {PartidaId}.",
                    conviteExistente.Id,
                    dto.UsuarioResolvedorId,
                    dto.AtletaId,
                    dto.PartidaId);
                return new ConvitePendenciaAtletaResultadoDto(false, true, false, conviteExistente.Id);
            }

            var convite = new ConviteCadastro
            {
                Email = emailNormalizado,
                Telefone = NormalizarTelefone(dto.Telefone),
                IdentificadorPublico = await GerarIdentificadorPublicoUnicoAsync(cancellationToken),
                PerfilDestino = PerfilDestinoPadraoConvite,
                ExpiraEmUtc = NormalizarExpiracao(null),
                Ativo = true,
                CriadoPorUsuarioId = dto.UsuarioResolvedorId,
                AtletaId = dto.AtletaId,
                PartidaId = dto.PartidaId,
                CanalEnvio = CanalEmail
            };
            DefinirNovoCodigoConvite(convite);

            await conviteCadastroRepositorio.AdicionarAsync(convite, cancellationToken);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

            logger.LogInformation(
                "Convite criado automaticamente ao concluir pendência de atleta. ConviteId: {ConviteId}. UsuarioResolvedorId: {UsuarioResolvedorId}. AtletaId: {AtletaId}. PartidaId: {PartidaId}.",
                convite.Id,
                dto.UsuarioResolvedorId,
                dto.AtletaId,
                dto.PartidaId);

            var conviteCriado = await conviteCadastroRepositorio.ObterPorIdParaAtualizacaoAsync(convite.Id, cancellationToken)
                ?? throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");
            await TentarEnviarEmailAutomaticoAsync(conviteCriado, cancellationToken);

            return new ConvitePendenciaAtletaResultadoDto(true, false, false, convite.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao criar/enviar convite automático de pendência de atleta. UsuarioResolvedorId: {UsuarioResolvedorId}. AtletaId: {AtletaId}. PartidaId: {PartidaId}.",
                dto.UsuarioResolvedorId,
                dto.AtletaId,
                dto.PartidaId);
            return new ConvitePendenciaAtletaResultadoDto(false, false, false, null);
        }
    }

    public async Task<ConviteCadastroDto> EnviarEmailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GarantirAdministradorAsync(cancellationToken);
        var convite = await conviteCadastroRepositorio.ObterPorIdParaAtualizacaoAsync(id, cancellationToken);
        if (convite is null)
        {
            throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");
        }

        ValidarConviteParaEnvioEmail(convite);
        await EnviarEmailConviteAsync(convite, falharSemConfiguracao: true, falharQuandoNaoEnviado: true, cancellationToken);
        return convite.ParaDto();
    }

    public async Task<ConviteCadastroDto> EnviarWhatsappAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GarantirAdministradorAsync(cancellationToken);
        var convite = await conviteCadastroRepositorio.ObterPorIdParaAtualizacaoAsync(id, cancellationToken);
        if (convite is null)
        {
            throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");
        }

        ValidarConviteParaEnvioWhatsapp(convite);
        await EnviarWhatsappConviteAsync(convite, falharSemConfiguracao: true, falharQuandoNaoEnviado: true, cancellationToken);
        return convite.ParaDto();
    }

    public async Task DesativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GarantirAdministradorAsync(cancellationToken);
        var convite = await conviteCadastroRepositorio.ObterPorIdParaAtualizacaoAsync(id, cancellationToken);
        if (convite is null)
        {
            throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");
        }

        if (!convite.Ativo)
        {
            return;
        }

        convite.Desativar();
        conviteCadastroRepositorio.Atualizar(convite);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<Usuario> GarantirAdministradorAsync(CancellationToken cancellationToken)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil != PerfilUsuario.Administrador)
        {
            throw new RegraNegocioException("Apenas administradores podem executar esta operação.");
        }

        return usuario;
    }

    private static string NormalizarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizarTexto(string? texto)
    {
        var textoNormalizado = NormalizadorNomeAtleta.NormalizarTexto(texto);
        return string.IsNullOrWhiteSpace(textoNormalizado) ? null : textoNormalizado;
    }

    private static string? NormalizarTelefone(string? telefone)
    {
        var telefoneNormalizado = new string((telefone ?? string.Empty)
            .Where(c => char.IsDigit(c) || c == '+' || c == ' ')
            .ToArray())
            .Trim();

        return string.IsNullOrWhiteSpace(telefoneNormalizado) ? null : telefoneNormalizado;
    }

    private async Task TentarEnviarEmailAutomaticoAsync(
        ConviteCadastro conviteCadastro,
        CancellationToken cancellationToken)
    {
        if (!DeveEnviarEmailAutomaticamente(conviteCadastro.CanalEnvio))
        {
            return;
        }

        await EnviarEmailConviteAsync(
            conviteCadastro,
            falharSemConfiguracao: false,
            falharQuandoNaoEnviado: false,
            cancellationToken);
    }

    private async Task TentarEnviarWhatsappAutomaticoAsync(
        ConviteCadastro conviteCadastro,
        CancellationToken cancellationToken)
    {
        if (!DeveEnviarWhatsappAutomaticamente(conviteCadastro.CanalEnvio))
        {
            return;
        }

        await EnviarWhatsappConviteAsync(
            conviteCadastro,
            falharSemConfiguracao: false,
            falharQuandoNaoEnviado: false,
            cancellationToken);
    }

    private static DateTime NormalizarExpiracao(DateTime? expiraEmUtc)
    {
        var expiracao = expiraEmUtc ?? DateTime.UtcNow.Add(ValidadePadrao);
        var expiracaoNormalizada = expiracao.Kind switch
        {
            DateTimeKind.Utc => expiracao,
            DateTimeKind.Local => expiracao.ToUniversalTime(),
            _ => DateTime.SpecifyKind(expiracao, DateTimeKind.Utc)
        };

        if (expiracaoNormalizada <= DateTime.UtcNow)
        {
            throw new RegraNegocioException("A data de expiração do convite deve ser futura.");
        }

        return expiracaoNormalizada;
    }

    private async Task<string> ObterOuGerarCodigoConviteAsync(
        ConviteCadastro conviteCadastro,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(conviteCadastro.CodigoConvite)
            && !string.IsNullOrWhiteSpace(conviteCadastro.CodigoConviteHash))
        {
            return conviteCadastro.CodigoConvite;
        }

        var codigoConvite = DefinirNovoCodigoConvite(conviteCadastro);
        conviteCadastroRepositorio.Atualizar(conviteCadastro);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return codigoConvite;
    }

    private static string DefinirNovoCodigoConvite(ConviteCadastro conviteCadastro)
    {
        var codigoConvite = CodigoConviteUtilitario.GerarNovo();
        conviteCadastro.DefinirCodigoConvite(
            codigoConvite,
            CodigoConviteUtilitario.GerarHash(codigoConvite));

        return codigoConvite;
    }

    private async Task<string> GerarIdentificadorPublicoUnicoAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var identificadorPublico = Guid.NewGuid().ToString("N");
            var existente = await conviteCadastroRepositorio.ObterPorIdentificadorPublicoAsync(identificadorPublico, cancellationToken);
            if (existente is null)
            {
                return identificadorPublico;
            }
        }
    }

    private static bool DeveEnviarEmailAutomaticamente(string? canalEnvio)
    {
        return string.IsNullOrWhiteSpace(canalEnvio)
            || canalEnvio.Contains("e-mail", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DeveEnviarWhatsappAutomaticamente(string? canalEnvio)
    {
        return !string.IsNullOrWhiteSpace(canalEnvio)
            && canalEnvio.Contains("whatsapp", StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnviarEmailConviteAsync(
        ConviteCadastro conviteCadastro,
        bool falharSemConfiguracao,
        bool falharQuandoNaoEnviado,
        CancellationToken cancellationToken)
    {
        var codigoConvite = await ObterOuGerarCodigoConviteAsync(conviteCadastro, cancellationToken);
        var resultado = await envioEmailConviteCadastroServico.EnviarAsync(conviteCadastro, codigoConvite, cancellationToken);
        if (!resultado.TentativaRealizada)
        {
            if (falharSemConfiguracao)
            {
                throw new RegraNegocioException(
                    resultado.Erro ?? "O envio automático de e-mail não está configurado.");
            }

            return;
        }

        if (resultado.Enviado)
        {
            conviteCadastro.RegistrarEnvioEmailComSucesso(DateTime.UtcNow);
        }
        else
        {
            conviteCadastro.RegistrarFalhaEnvioEmail(resultado.Erro, DateTime.UtcNow);
            logger.LogWarning(
                "Falha ao enviar e-mail do convite. ConviteId: {ConviteId}. AtletaId: {AtletaId}. PartidaId: {PartidaId}. Erro: {ErroEnvioEmail}.",
                conviteCadastro.Id,
                conviteCadastro.AtletaId,
                conviteCadastro.PartidaId,
                conviteCadastro.ErroEnvioEmail);
        }

        conviteCadastroRepositorio.Atualizar(conviteCadastro);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        if (falharQuandoNaoEnviado && !resultado.Enviado)
        {
            throw new RegraNegocioException(conviteCadastro.ErroEnvioEmail ?? "Falha ao enviar o e-mail do convite.");
        }
    }

    private async Task EnviarWhatsappConviteAsync(
        ConviteCadastro conviteCadastro,
        bool falharSemConfiguracao,
        bool falharQuandoNaoEnviado,
        CancellationToken cancellationToken)
    {
        var codigoConvite = await ObterOuGerarCodigoConviteAsync(conviteCadastro, cancellationToken);
        var resultado = await envioWhatsappConviteCadastroServico.EnviarAsync(conviteCadastro, codigoConvite, cancellationToken);
        if (!resultado.TentativaRealizada)
        {
            if (falharSemConfiguracao)
            {
                throw new RegraNegocioException(
                    resultado.Erro ?? "O envio automático de WhatsApp não está configurado.");
            }

            return;
        }

        if (resultado.Enviado)
        {
            conviteCadastro.RegistrarEnvioWhatsappComSucesso(DateTime.UtcNow);
        }
        else
        {
            conviteCadastro.RegistrarFalhaEnvioWhatsapp(resultado.Erro, DateTime.UtcNow);
        }

        conviteCadastroRepositorio.Atualizar(conviteCadastro);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        if (falharQuandoNaoEnviado && !resultado.Enviado)
        {
            throw new RegraNegocioException(conviteCadastro.ErroEnvioWhatsapp ?? "Falha ao enviar o WhatsApp do convite.");
        }
    }

    private static void ValidarCanalEnvio(string? canalEnvio, string? telefone)
    {
        if (!DeveEnviarWhatsappAutomaticamente(canalEnvio))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            throw new RegraNegocioException("Telefone é obrigatório quando o canal de envio inclui WhatsApp.");
        }
    }

    private static void ValidarConviteParaEnvioEmail(ConviteCadastro conviteCadastro)
    {
        var agoraUtc = DateTime.UtcNow;
        if (!conviteCadastro.Ativo)
        {
            throw new RegraNegocioException("Este convite está cancelado e não pode ser enviado.");
        }

        if (conviteCadastro.FoiUtilizado())
        {
            throw new RegraNegocioException("Este convite já foi utilizado e não pode ser reenviado.");
        }

        if (conviteCadastro.EstaExpirado(agoraUtc))
        {
            throw new RegraNegocioException("Este convite está expirado e não pode ser reenviado.");
        }
    }

    private static void ValidarConviteParaGeracaoAcesso(ConviteCadastro conviteCadastro)
    {
        var agoraUtc = DateTime.UtcNow;
        if (!conviteCadastro.Ativo)
        {
            throw new RegraNegocioException("Este convite está cancelado e não pode gerar um novo código.");
        }

        if (conviteCadastro.FoiUtilizado())
        {
            throw new RegraNegocioException("Este convite já foi utilizado e não pode gerar um novo código.");
        }

        if (conviteCadastro.EstaExpirado(agoraUtc))
        {
            throw new RegraNegocioException("Este convite está expirado e não pode gerar um novo código.");
        }
    }

    private static void ValidarConviteParaEnvioWhatsapp(ConviteCadastro conviteCadastro)
    {
        var agoraUtc = DateTime.UtcNow;
        if (!conviteCadastro.Ativo)
        {
            throw new RegraNegocioException("Este convite está cancelado e não pode ser enviado.");
        }

        if (conviteCadastro.FoiUtilizado())
        {
            throw new RegraNegocioException("Este convite já foi utilizado e não pode ser reenviado.");
        }

        if (conviteCadastro.EstaExpirado(agoraUtc))
        {
            throw new RegraNegocioException("Este convite está expirado e não pode ser reenviado.");
        }

        if (string.IsNullOrWhiteSpace(conviteCadastro.Telefone))
        {
            throw new RegraNegocioException("Informe um telefone válido no convite antes de enviar por WhatsApp.");
        }
    }
}
