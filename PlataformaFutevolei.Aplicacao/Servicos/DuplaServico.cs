using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class DuplaServico(
    IDuplaRepositorio duplaRepositorio,
    IAtletaRepositorio atletaRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IDuplaServico
{
    public async Task<IReadOnlyList<DuplaDto>> ListarAsync(
        bool somenteInscritasMinhasCompeticoes = false,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil is not PerfilUsuario.Administrador and not PerfilUsuario.Organizador)
        {
            throw new RegraNegocioException("Apenas administradores ou organizadores podem executar esta operação.");
        }

        var duplas = somenteInscritasMinhasCompeticoes && usuario.Perfil == PerfilUsuario.Organizador
            ? await duplaRepositorio.ListarInscritasPorOrganizadorAsync(usuario.Id, cancellationToken)
            : await duplaRepositorio.ListarAsync(cancellationToken);

        return duplas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<IReadOnlyList<DuplaDto>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            await autorizacaoUsuarioServico.GarantirAcessoAtletaAsync(atletaId, cancellationToken);
        }

        var duplas = await duplaRepositorio.ListarPorAtletaAsync(atletaId, cancellationToken);
        return duplas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<DuplaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        GarantirPerfilGestor(usuario);
        if (usuario.Perfil == PerfilUsuario.Organizador)
        {
            await GarantirAcessoOrganizadorAsync(id, usuario.Id, cancellationToken);
        }

        var dupla = await duplaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (dupla is null)
        {
            throw new EntidadeNaoEncontradaException("Dupla não encontrada.");
        }

        return dupla.ParaDto();
    }

    public async Task<DuplaDto> CriarAsync(CriarDuplaDto dto, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirAdminOuOrganizadorAsync(cancellationToken);
        var (atletaNormalizado1Id, atletaNormalizado2Id) = NormalizarAtletas(dto.Atleta1Id, dto.Atleta2Id);
        var (atleta1, atleta2) = await ValidarAtletasAsync(atletaNormalizado1Id, atletaNormalizado2Id, cancellationToken);

        var existente = await duplaRepositorio.ObterPorAtletasAsync(atletaNormalizado1Id, atletaNormalizado2Id, cancellationToken);
        if (existente is not null)
        {
            return existente.ParaDto();
        }

        var dupla = new Dupla
        {
            Nome = ObterNomeDupla(dto.Nome, atleta1.Nome, atleta2.Nome),
            Atleta1Id = atletaNormalizado1Id,
            Atleta2Id = atletaNormalizado2Id
        };

        await duplaRepositorio.AdicionarAsync(dupla, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var duplaCriada = await duplaRepositorio.ObterPorIdAsync(dupla.Id, cancellationToken);
        return duplaCriada!.ParaDto();
    }

    public async Task<DuplaDto> AtualizarAsync(Guid id, AtualizarDuplaDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        GarantirPerfilGestor(usuario);
        if (usuario.Perfil == PerfilUsuario.Organizador)
        {
            await GarantirAcessoOrganizadorAsync(id, usuario.Id, cancellationToken);
        }

        var dupla = await duplaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (dupla is null)
        {
            throw new EntidadeNaoEncontradaException("Dupla não encontrada.");
        }

        var (atletaNormalizado1Id, atletaNormalizado2Id) = NormalizarAtletas(dto.Atleta1Id, dto.Atleta2Id);
        var (atleta1, atleta2) = await ValidarAtletasAsync(atletaNormalizado1Id, atletaNormalizado2Id, cancellationToken);

        var existente = await duplaRepositorio.ObterPorAtletasAsync(atletaNormalizado1Id, atletaNormalizado2Id, cancellationToken);
        if (existente is not null && existente.Id != dupla.Id)
        {
            throw new RegraNegocioException("Já existe uma dupla cadastrada com estes atletas.");
        }

        dupla.Atleta1Id = atletaNormalizado1Id;
        dupla.Atleta2Id = atletaNormalizado2Id;
        dupla.Nome = ObterNomeDupla(dto.Nome, atleta1.Nome, atleta2.Nome);
        dupla.AtualizarDataModificacao();

        duplaRepositorio.Atualizar(dupla);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var duplaAtualizada = await duplaRepositorio.ObterPorIdAsync(id, cancellationToken);
        return duplaAtualizada!.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        GarantirPerfilGestor(usuario);
        if (usuario.Perfil == PerfilUsuario.Organizador)
        {
            await GarantirAcessoOrganizadorAsync(id, usuario.Id, cancellationToken);
        }

        var dupla = await duplaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (dupla is null)
        {
            throw new EntidadeNaoEncontradaException("Dupla não encontrada.");
        }

        duplaRepositorio.Remover(dupla);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private static void GarantirPerfilGestor(Usuario usuario)
    {
        if (usuario.Perfil is not PerfilUsuario.Administrador and not PerfilUsuario.Organizador)
        {
            throw new RegraNegocioException("Apenas administradores ou organizadores podem executar esta operação.");
        }
    }

    private async Task GarantirAcessoOrganizadorAsync(Guid duplaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken)
    {
        var pertenceAoOrganizador = await duplaRepositorio.PertenceAoOrganizadorAsync(duplaId, usuarioOrganizadorId, cancellationToken);
        if (!pertenceAoOrganizador)
        {
            throw new RegraNegocioException("O organizador só pode alterar duplas inscritas em competições vinculadas ao próprio usuário.");
        }
    }

    private async Task<(Atleta atleta1, Atleta atleta2)> ValidarAtletasAsync(Guid atleta1Id, Guid atleta2Id, CancellationToken cancellationToken)
    {
        if (atleta1Id == Guid.Empty || atleta2Id == Guid.Empty)
        {
            throw new RegraNegocioException("Uma dupla deve possuir exatamente dois atletas válidos.");
        }

        if (atleta1Id == atleta2Id)
        {
            throw new RegraNegocioException("Uma dupla não pode ter o mesmo atleta duas vezes.");
        }

        var atleta1 = await atletaRepositorio.ObterPorIdAsync(atleta1Id, cancellationToken);
        var atleta2 = await atletaRepositorio.ObterPorIdAsync(atleta2Id, cancellationToken);

        if (atleta1 is null || atleta2 is null)
        {
            throw new RegraNegocioException("Os dois atletas da dupla devem existir.");
        }

        return (atleta1, atleta2);
    }

    private static string ObterNomeDupla(string? nome, string nomeAtleta1, string nomeAtleta2)
    {
        if (!string.IsNullOrWhiteSpace(nome))
        {
            return nome.Trim();
        }

        return $"{nomeAtleta1} / {nomeAtleta2}";
    }

    private static (Guid atleta1Id, Guid atleta2Id) NormalizarAtletas(Guid atleta1Id, Guid atleta2Id)
    {
        return atleta1Id.CompareTo(atleta2Id) <= 0
            ? (atleta1Id, atleta2Id)
            : (atleta2Id, atleta1Id);
    }
}
