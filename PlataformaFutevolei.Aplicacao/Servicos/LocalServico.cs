using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class LocalServico(
    ILocalRepositorio localRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : ILocalServico
{
    public async Task<IReadOnlyList<LocalDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var locais = await localRepositorio.ListarAsync(cancellationToken);
        return locais.Select(x => x.ParaDto()).ToList();
    }

    public async Task<LocalDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var local = await localRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (local is null)
        {
            throw new EntidadeNaoEncontradaException("Local não encontrado.");
        }

        return local.ParaDto();
    }

    public async Task<LocalDto> CriarAsync(CriarLocalDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioGestorAsync(cancellationToken);
        var nome = ValidarNome(dto.Nome);
        ValidarTipo(dto.Tipo);
        ValidarQuantidadeQuadras(dto.QuantidadeQuadras);
        await ValidarDuplicidadeAsync(nome, cancellationToken);

        var local = new Local
        {
            Nome = nome,
            Tipo = dto.Tipo,
            QuantidadeQuadras = dto.QuantidadeQuadras,
            UsuarioCriadorId = usuario.Id
        };

        await localRepositorio.AdicionarAsync(local, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return local.ParaDto();
    }

    public async Task<LocalDto> AtualizarAsync(Guid id, AtualizarLocalDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioGestorAsync(cancellationToken);
        var local = await localRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (local is null)
        {
            throw new EntidadeNaoEncontradaException("Local não encontrado.");
        }

        GarantirGestaoPermitida(usuario, local.UsuarioCriadorId, "O organizador só pode alterar locais criados pelo próprio usuário.");

        var nome = ValidarNome(dto.Nome);
        ValidarTipo(dto.Tipo);
        ValidarQuantidadeQuadras(dto.QuantidadeQuadras);

        var existente = await localRepositorio.ObterPorNomeAsync(nome, cancellationToken);
        if (existente is not null && existente.Id != local.Id)
        {
            throw new RegraNegocioException("Já existe um local cadastrado com este nome.");
        }

        local.Nome = nome;
        local.Tipo = dto.Tipo;
        local.QuantidadeQuadras = dto.QuantidadeQuadras;
        local.AtualizarDataModificacao();

        localRepositorio.Atualizar(local);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return local.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioGestorAsync(cancellationToken);
        var local = await localRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (local is null)
        {
            throw new EntidadeNaoEncontradaException("Local não encontrado.");
        }

        GarantirGestaoPermitida(usuario, local.UsuarioCriadorId, "O organizador só pode excluir locais criados pelo próprio usuário.");

        localRepositorio.Remover(local);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<Usuario> ObterUsuarioGestorAsync(CancellationToken cancellationToken)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil is not PerfilUsuario.Administrador and not PerfilUsuario.Organizador)
        {
            throw new RegraNegocioException("Apenas administradores ou organizadores podem executar esta operação.");
        }

        return usuario;
    }

    private static void GarantirGestaoPermitida(Usuario usuario, Guid? usuarioCriadorId, string mensagem)
    {
        if (usuario.Perfil == PerfilUsuario.Administrador)
        {
            return;
        }

        if (usuarioCriadorId != usuario.Id)
        {
            throw new RegraNegocioException(mensagem);
        }
    }

    private async Task ValidarDuplicidadeAsync(string nome, CancellationToken cancellationToken)
    {
        var existente = await localRepositorio.ObterPorNomeAsync(nome, cancellationToken);
        if (existente is not null)
        {
            throw new RegraNegocioException("Já existe um local cadastrado com este nome.");
        }
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome do local é obrigatório.");
        }

        return nome.Trim();
    }

    private static void ValidarTipo(TipoLocal tipo)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new RegraNegocioException("Tipo de local inválido.");
        }
    }

    private static void ValidarQuantidadeQuadras(int quantidadeQuadras)
    {
        if (quantidadeQuadras <= 0)
        {
            throw new RegraNegocioException("Quantidade de quadras deve ser maior que zero.");
        }
    }
}
