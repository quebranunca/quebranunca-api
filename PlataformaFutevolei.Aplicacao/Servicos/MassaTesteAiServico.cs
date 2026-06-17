using System.Net.Mail;
using System.Text;
using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class MassaTesteAiServico(
    IUsuarioRepositorio usuarioRepositorio,
    IAtletaRepositorio atletaRepositorio,
    IArenaRepositorio arenaRepositorio,
    IGrupoRepositorio grupoRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    ISenhaServico senhaServico
) : IMassaTesteAiServico
{
    public const string Prefixo = "[AI TESTE]";
    public const string NomeUsuarioPrincipal = "[AI TESTE] Testador";
    public const string NomeArenaBase = "[AI TESTE] Arena Base";
    public const string NomeGrupoBase = "[AI TESTE] Grupo Base";

    private static readonly string[] NomesAtletasAuxiliares =
    [
        "[AI TESTE] Atleta 01",
        "[AI TESTE] Atleta 02",
        "[AI TESTE] Atleta 03",
        "[AI TESTE] Atleta 04"
    ];

    public async Task<MassaTesteAiResultado> GarantirAsync(
        MassaTesteAiConfiguracao configuracao,
        CancellationToken cancellationToken = default)
    {
        if (!configuracao.Habilitada)
        {
            return new MassaTesteAiResultado(false, false, null, null, null, null, 0, 0);
        }

        var emailUsuarioPrincipal = NormalizarEmail(configuracao.EmailUsuarioPrincipal);
        ValidarSenha(configuracao.SenhaUsuarioPrincipal);

        Usuario? usuarioPrincipal = null;
        Atleta? atletaUsuarioPrincipal = null;
        Arena? arenaBase = null;
        Grupo? grupoBase = null;
        var atletasAuxiliares = new List<Atleta>();

        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            usuarioPrincipal = await GarantirUsuarioPrincipalAsync(
                emailUsuarioPrincipal,
                configuracao.SenhaUsuarioPrincipal,
                ct);

            atletaUsuarioPrincipal = await GarantirAtletaUsuarioPrincipalAsync(usuarioPrincipal, ct);
            arenaBase = await GarantirArenaBaseAsync(ct);
            grupoBase = await GarantirGrupoBaseAsync(usuarioPrincipal, arenaBase, ct);

            await GarantirVinculoGrupoAsync(grupoBase, atletaUsuarioPrincipal, ct);

            foreach (var nomeAtleta in NomesAtletasAuxiliares)
            {
                var atleta = await GarantirAtletaAuxiliarAsync(nomeAtleta, usuarioPrincipal.Id, ct);
                atletasAuxiliares.Add(atleta);
                await GarantirVinculoGrupoAsync(grupoBase, atleta, ct);
            }

            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
        }, cancellationToken);

        var vinculos = await grupoAtletaRepositorio.ListarPorGrupoAsync(grupoBase!.Id, cancellationToken);
        return new MassaTesteAiResultado(
            true,
            true,
            usuarioPrincipal!.Id,
            atletaUsuarioPrincipal!.Id,
            grupoBase!.Id,
            arenaBase!.Id,
            atletasAuxiliares.Select(x => x.Id).Distinct().Count(),
            vinculos.Select(x => x.AtletaId).Distinct().Count());
    }

    private async Task<Usuario> GarantirUsuarioPrincipalAsync(
        string email,
        string senha,
        CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(email, cancellationToken);
        if (usuario is null)
        {
            usuario = new Usuario
            {
                Nome = NomeUsuarioPrincipal,
                Email = email,
                Perfil = PerfilUsuario.Atleta,
                Ativo = true,
                SenhaHash = senhaServico.GerarHash(senha),
                SenhaDefinidaEmUtc = agora,
                SenhaAtualizadaEmUtc = agora,
                PerfilPublico = true,
                ExibirEmail = false,
                PermitirUsoImagem = false,
                PermitirUsoLocalizacao = false
            };
            await usuarioRepositorio.AdicionarAsync(usuario, cancellationToken);
            return usuario;
        }

        usuario.Nome = NomeUsuarioPrincipal;
        usuario.Email = email;
        usuario.Perfil = PerfilUsuario.Atleta;
        usuario.Ativo = true;
        usuario.DadosAnonimizados = false;
        usuario.SenhaHash = senhaServico.GerarHash(senha);
        usuario.SenhaDefinidaEmUtc ??= agora;
        usuario.SenhaAtualizadaEmUtc = agora;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        return usuario;
    }

    private async Task<Atleta> GarantirAtletaUsuarioPrincipalAsync(
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        Atleta? atleta = null;
        if (usuario.AtletaId.HasValue)
        {
            atleta = await atletaRepositorio.ObterPorIdParaAtualizacaoAsync(usuario.AtletaId.Value, cancellationToken);
        }

        atleta ??= (await atletaRepositorio.ListarPorEmailAsync(usuario.Email, cancellationToken))
            .FirstOrDefault(x => string.Equals(x.Nome, NomeUsuarioPrincipal, StringComparison.OrdinalIgnoreCase));

        if (atleta is null)
        {
            atleta = new Atleta();
            await atletaRepositorio.AdicionarAsync(atleta, cancellationToken);
        }

        atleta.Nome = NomeUsuarioPrincipal;
        atleta.Apelido = "AI Testador";
        atleta.Email = usuario.Email;
        atleta.CadastroPendente = false;
        atleta.Lado = LadoAtleta.Ambos;
        atleta.UsuarioCriadorId = usuario.Id;
        atleta.AtualizarDataModificacao();

        usuario.AtletaId = atleta.Id;
        usuario.Atleta = atleta;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        atletaRepositorio.Atualizar(atleta);
        return atleta;
    }

    private async Task<Atleta> GarantirAtletaAuxiliarAsync(
        string nome,
        Guid usuarioCriadorId,
        CancellationToken cancellationToken)
    {
        var atleta = await atletaRepositorio.ObterPorNomeAsync(nome, cancellationToken);
        if (atleta is null)
        {
            atleta = new Atleta();
            await atletaRepositorio.AdicionarAsync(atleta, cancellationToken);
        }

        atleta.Nome = nome;
        atleta.Apelido = nome.Replace("[AI TESTE] ", string.Empty, StringComparison.Ordinal);
        atleta.Email = null;
        atleta.CadastroPendente = false;
        atleta.Lado = LadoAtleta.Ambos;
        atleta.UsuarioCriadorId = usuarioCriadorId;
        atleta.AtualizarDataModificacao();
        atletaRepositorio.Atualizar(atleta);
        return atleta;
    }

    private async Task<Arena> GarantirArenaBaseAsync(CancellationToken cancellationToken)
    {
        var arena = await arenaRepositorio.ObterPorNomeAsync(NomeArenaBase, cancellationToken);
        if (arena is null)
        {
            arena = new Arena();
            await arenaRepositorio.AdicionarAsync(arena, cancellationToken);
        }

        arena.Nome = NomeArenaBase;
        arena.Slug = "ai-teste-arena-base";
        arena.Descricao = "Arena técnica para validações manuais e E2E.";
        arena.TipoArena = TipoArena.ArenaPrivada;
        arena.QuantidadeEspacos = 1;
        arena.Cidade = "Praia Grande";
        arena.Estado = "SP";
        arena.Publica = false;
        arena.Ativa = true;
        arena.PossuiIluminacao = true;
        arena.PossuiEstacionamento = false;
        arena.PossuiVestiario = false;
        arena.PossuiDucha = false;
        arena.PossuiBarRestaurante = false;
        arena.PossuiLoja = false;
        arena.PossuiCobertura = false;
        arena.AtualizarDataModificacao();
        arenaRepositorio.Atualizar(arena);
        return arena;
    }

    private async Task<Grupo> GarantirGrupoBaseAsync(
        Usuario usuarioPrincipal,
        Arena arenaBase,
        CancellationToken cancellationToken)
    {
        var grupo = await grupoRepositorio.ObterPorNomeNormalizadoAsync(NomeGrupoBase, cancellationToken);
        if (grupo is null)
        {
            grupo = new Grupo();
            await grupoRepositorio.AdicionarAsync(grupo, cancellationToken);
        }

        grupo.Nome = NomeGrupoBase;
        grupo.Descricao = "Grupo técnico para validações manuais e E2E.";
        grupo.DataInicio = DateTime.UtcNow.Date;
        grupo.DataFim = null;
        grupo.ArenaId = arenaBase.Id;
        grupo.Arena = arenaBase;
        grupo.LocalPrincipal = NomeArenaBase;
        grupo.UsuarioOrganizadorId = usuarioPrincipal.Id;
        grupo.UsuarioOrganizador = usuarioPrincipal;
        grupo.Publico = false;
        grupo.AtualizarDataModificacao();
        grupoRepositorio.Atualizar(grupo);
        return grupo;
    }

    private async Task GarantirVinculoGrupoAsync(
        Grupo grupo,
        Atleta atleta,
        CancellationToken cancellationToken)
    {
        var vinculo = await grupoAtletaRepositorio.ObterPorGrupoEAtletaAsync(
            grupo.Id,
            atleta.Id,
            cancellationToken);

        if (vinculo is not null)
        {
            return;
        }

        await grupoAtletaRepositorio.AdicionarAsync(new GrupoAtleta
        {
            GrupoId = grupo.Id,
            AtletaId = atleta.Id,
            Grupo = grupo,
            Atleta = atleta
        }, cancellationToken);
    }

    private static string NormalizarEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new RegraNegocioException("E-mail do usuário principal da massa AI é obrigatório.");
        }

        var emailNormalizado = email.Trim().ToLowerInvariant();
        try
        {
            var endereco = new MailAddress(emailNormalizado);
            if (!string.Equals(endereco.Address, emailNormalizado, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch
        {
            throw new RegraNegocioException("E-mail do usuário principal da massa AI é inválido.");
        }

        return emailNormalizado;
    }

    private static void ValidarSenha(string? senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new RegraNegocioException("Senha do usuário principal da massa AI não configurada.");
        }

        if (senha.Length < 6)
        {
            throw new RegraNegocioException("Senha do usuário principal da massa AI deve ter no mínimo 6 caracteres.");
        }
    }
}
