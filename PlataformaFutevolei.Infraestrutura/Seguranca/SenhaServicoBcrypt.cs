using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;

namespace PlataformaFutevolei.Infraestrutura.Seguranca;

public class SenhaServicoBcrypt : ISenhaServico
{
    public string GerarHash(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha);
    }

    public bool Verificar(string senha, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
