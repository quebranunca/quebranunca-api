#!/usr/bin/env python3
import argparse
import csv
import json
import re
import sys
import urllib.error
import urllib.request
from collections import Counter, defaultdict
from datetime import datetime, timedelta, timezone


MARCADOR_PADRAO = "ImportacaoCSV:LigaPraiaGrandeHistorico"


class ApiError(Exception):
    def __init__(self, metodo, caminho, status, corpo):
        super().__init__(f"{metodo} {caminho} -> {status}: {corpo[:500]}")
        self.status = status
        self.corpo = corpo


def normalizar(texto):
    return re.sub(r"\s+", " ", (texto or "").strip()).casefold()


def requisitar(base_url, metodo, caminho, dados=None, token=None, ok=(200, 201, 204)):
    corpo = None
    headers = {}
    if dados is not None:
        corpo = json.dumps(dados).encode("utf-8")
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token

    req = urllib.request.Request(base_url + caminho, data=corpo, headers=headers, method=metodo)
    try:
        with urllib.request.urlopen(req, timeout=60) as resposta:
            conteudo = resposta.read().decode("utf-8")
            if resposta.status not in ok:
                raise ApiError(metodo, caminho, resposta.status, conteudo)
            return json.loads(conteudo) if conteudo else None
    except urllib.error.HTTPError as erro:
        conteudo = erro.read().decode("utf-8", "replace")
        raise ApiError(metodo, caminho, erro.code, conteudo)


def ler_csv(caminho):
    with open(caminho, encoding="utf-8-sig", newline="") as arquivo:
        linhas = []
        for numero_linha, linha in enumerate(csv.DictReader(arquivo), start=2):
            registro = {chave: (valor or "").strip() for chave, valor in linha.items()}
            registro["_linha"] = numero_linha
            linhas.append(registro)
        return linhas


def placar(valor, linha):
    try:
        return int(str(valor).strip())
    except Exception as erro:
        raise ValueError(f"Linha {linha}: placar invalido: {valor!r}") from erro


def genero_categoria(campeonato, categoria):
    texto = normalizar(campeonato + " " + categoria)
    if "misto" in texto:
        return 3
    if "feminino" in texto or campeonato == "3 Etapa Feminino":
        return 2
    return 1


def nivel_categoria(categoria):
    texto = normalizar(categoria)
    if "estreante" in texto:
        return 1
    if "iniciante" in texto:
        return 2
    if "intermediario" in texto or "intermediário" in texto:
        return 3
    if "amador" in texto:
        return 4
    if "profissional" in texto:
        return 5
    return 6


def separar_dupla(nome):
    nome = re.sub(r"\s+", " ", nome.strip())
    especiais = {
        normalizar("LU PESANHA GI ANDRAD"): ("LU PESANHA", "GI ANDRAD"),
    }
    if normalizar(nome) in especiais:
        return especiais[normalizar(nome)]

    partes = re.split(r"\s+e\s+", nome, flags=re.I, maxsplit=1)
    if len(partes) == 2 and partes[0].strip() and partes[1].strip():
        atleta_1 = partes[0].strip()
        atleta_2 = partes[1].strip()
        if normalizar(atleta_1) == normalizar(atleta_2):
            atleta_2 = f"{atleta_2} Parceiro"
        return atleta_1, atleta_2

    return f"{nome} Atleta 1", f"{nome} Atleta 2"


def fase_importada(fase, tipo):
    fase_limpa = re.sub(r"\s+", " ", fase.strip())
    tipo_normalizado = normalizar(tipo)
    numero = re.search(r"(\d+)", fase_limpa)
    if tipo_normalizado == "perdedores":
        return f"Chave dos perdedores - Rodada {int(numero.group(1)):02d}" if numero else "Chave dos perdedores"

    fase_normalizada = normalizar(fase_limpa)
    if fase_normalizada.startswith("fase") and numero:
        return f"Chave dos vencedores - Rodada {int(numero.group(1)):02d}"
    if "oitavas" in fase_normalizada:
        return "Chave dos vencedores - Oitavas"
    if "quartas" in fase_normalizada:
        return "Chave dos vencedores - Quartas"
    if "semi" in fase_normalizada:
        return "Chave dos vencedores - Semi-final"
    if fase_normalizada == "final":
        return "Final"
    if "disputa" in fase_normalizada:
        return "Disputa 3o"
    return fase_limpa


def vencedor_csv(linha):
    vencedor = normalizar(linha["Vencedor"])
    dupla_1 = normalizar(linha["Dupla1"])
    dupla_2 = normalizar(linha["Dupla2"])
    pontos_1 = placar(linha["Pts1"], linha["_linha"])
    pontos_2 = placar(linha["Pts2"], linha["_linha"])
    if vencedor == dupla_1:
        return 1
    if vencedor == dupla_2:
        return 2
    return 1 if pontos_1 > pontos_2 else 2


def atualizar_competicao(api, competicao, alteracoes):
    payload = {
        "nome": competicao["nome"],
        "tipo": competicao["tipo"],
        "descricao": competicao.get("descricao"),
        "dataInicio": competicao["dataInicio"],
        "dataFim": competicao.get("dataFim"),
        "ligaId": competicao.get("ligaId"),
        "localId": competicao.get("localId"),
        "formatoCampeonatoId": competicao.get("formatoCampeonatoId"),
        "regraCompeticaoId": competicao.get("regraCompeticaoId"),
        "inscricoesAbertas": competicao.get("inscricoesAbertas", True),
        "possuiFinalReset": competicao.get("possuiFinalReset", False),
    }
    payload.update(alteracoes)
    return api("PUT", f"/api/competicoes/{competicao['id']}", payload)


def atualizar_categoria(api, categoria, inscricoes_encerradas):
    payload = {
        "formatoCampeonatoId": categoria.get("formatoCampeonatoId"),
        "nome": categoria["nome"],
        "genero": categoria["genero"],
        "nivel": categoria["nivel"],
        "pesoRanking": categoria.get("pesoRanking", 1),
        "quantidadeMaximaDuplas": categoria.get("quantidadeMaximaDuplas"),
        "inscricoesEncerradas": inscricoes_encerradas,
    }
    return api("PUT", f"/api/categorias/{categoria['id']}", payload)


def main():
    parser = argparse.ArgumentParser(description="Importa CSV historico de campeonatos usando a API da plataforma.")
    parser.add_argument("csv")
    parser.add_argument("--base-url", default="http://localhost:5080")
    parser.add_argument("--email", default="admin@teste.com")
    parser.add_argument("--senha", default="123456")
    parser.add_argument("--liga", default="Liga Praia Grande")
    parser.add_argument("--marcador", default=MARCADOR_PADRAO)
    args = parser.parse_args()

    criados = Counter()
    atualizados = Counter()
    ignorados = Counter()
    erros_validacao = []

    def api(metodo, caminho, dados=None):
        return requisitar(args.base_url, metodo, caminho, dados, token)

    token = requisitar(
        args.base_url,
        "POST",
        "/api/autenticacao/login",
        {"email": args.email, "senha": args.senha},
    )["token"]

    linhas = ler_csv(args.csv)
    if not linhas:
        raise ValueError("CSV sem linhas.")

    linhas_invalidas = {}
    for linha in linhas:
        pontos_1 = placar(linha["Pts1"], linha["_linha"])
        pontos_2 = placar(linha["Pts2"], linha["_linha"])
        if pontos_1 == pontos_2:
            linhas_invalidas[linha["_linha"]] = "placar empatado nao pode ser importado como resultado"

    linhas_partidas = [linha for linha in linhas if linha["_linha"] not in linhas_invalidas]

    ligas = api("GET", "/api/ligas")
    liga = next((item for item in ligas if normalizar(item["nome"]) == normalizar(args.liga)), None)
    if liga is None:
        liga = api(
            "POST",
            "/api/ligas",
            {"nome": args.liga, "descricao": "Carga historica de campeonatos da Praia Grande."},
        )
        criados["ligas"] += 1

    liga_id = liga["id"]
    regras = api("GET", "/api/regras-competicao")
    nome_regra_historica = "Regra Historica Liga Praia Grande"
    regra = next((item for item in regras if normalizar(item["nome"]) == normalizar(nome_regra_historica)), None)
    if regra is None:
        regra = api(
            "POST",
            "/api/regras-competicao",
            {
                "nome": nome_regra_historica,
                "descricao": "Regra para importacao de campeonatos historicos com jogos curtos no CSV.",
                "pontosMinimosPartida": 1,
                "diferencaMinimaPartida": 1,
                "permiteEmpate": False,
                "pontosVitoria": 3,
                "pontosDerrota": 0,
                "pontosParticipacao": 0,
                "pontosPrimeiroLugar": 0,
                "pontosSegundoLugar": 0,
                "pontosTerceiroLugar": 0,
            },
        )
        criados["regras_competicao"] += 1

    regra_id = regra["id"]
    competicoes = api("GET", "/api/competicoes")
    competicoes_por_nome = {normalizar(item["nome"]): item for item in competicoes}
    competicoes_ids = {}
    categorias_ids = {}
    datas_base = {}

    campeonatos = list(dict.fromkeys(linha["Campeonato"] for linha in linhas))
    for indice, campeonato in enumerate(campeonatos):
        competicao = competicoes_por_nome.get(normalizar(campeonato))
        if competicao is None:
            data_base = datetime(2024, 1, 1, tzinfo=timezone.utc) + timedelta(days=30 * indice)
            competicao = api(
                "POST",
                "/api/competicoes",
                {
                    "nome": campeonato,
                    "tipo": 1,
                    "descricao": f"{args.marcador}; campeonato historico importado do CSV.",
                    "dataInicio": data_base.isoformat().replace("+00:00", "Z"),
                    "dataFim": (data_base + timedelta(days=1)).isoformat().replace("+00:00", "Z"),
                    "ligaId": liga_id,
                    "localId": None,
                    "formatoCampeonatoId": None,
                    "regraCompeticaoId": regra_id,
                    "inscricoesAbertas": True,
                    "possuiFinalReset": False,
                },
            )
            criados["competicoes"] += 1

        if (
            not competicao.get("inscricoesAbertas", False)
            or competicao.get("ligaId") != liga_id
            or competicao.get("regraCompeticaoId") != regra_id
        ):
            competicao = atualizar_competicao(
                api,
                competicao,
                {"inscricoesAbertas": True, "ligaId": liga_id, "regraCompeticaoId": regra_id},
            )
            atualizados["competicoes_abertas_ou_liga"] += 1

        competicoes_ids[campeonato] = competicao["id"]
        datas_base[campeonato] = datetime.fromisoformat(competicao["dataInicio"].replace("Z", "+00:00"))

        categorias = api("GET", f"/api/competicoes/{competicao['id']}/categorias")
        categorias_por_nome = {normalizar(item["nome"]): item for item in categorias}
        nomes_categorias = list(dict.fromkeys(linha["Categoria"] for linha in linhas if linha["Campeonato"] == campeonato))
        for categoria_nome in nomes_categorias:
            categoria = categorias_por_nome.get(normalizar(categoria_nome))
            if categoria is None:
                categoria = api(
                    "POST",
                    "/api/categorias",
                    {
                        "competicaoId": competicao["id"],
                        "formatoCampeonatoId": None,
                        "nome": categoria_nome,
                        "genero": genero_categoria(campeonato, categoria_nome),
                        "nivel": nivel_categoria(categoria_nome),
                        "pesoRanking": 1,
                        "quantidadeMaximaDuplas": None,
                        "inscricoesEncerradas": False,
                    },
                )
                criados["categorias"] += 1

            if categoria.get("inscricoesEncerradas", False):
                categoria = atualizar_categoria(api, categoria, False)
                atualizados["categorias_reabertas"] += 1

            categorias_ids[(campeonato, categoria_nome)] = categoria["id"]

    atletas = api("GET", "/api/atletas")
    atletas_por_nome = {normalizar(item["nome"]): item for item in atletas}
    duplas = api("GET", "/api/duplas")
    duplas_por_composicao = {tuple(sorted([item["atleta1Id"], item["atleta2Id"]])): item for item in duplas}

    def obter_atleta(nome):
        chave = normalizar(nome)
        existente = atletas_por_nome.get(chave)
        if existente:
            return existente

        atleta = api(
            "POST",
            "/api/atletas",
            {
                "nome": nome,
                "apelido": None,
                "telefone": None,
                "email": None,
                "instagram": None,
                "cpf": None,
                "cidade": "Praia Grande",
                "estado": "SP",
                "cadastroPendente": True,
                "nivel": None,
                "lado": 3,
                "dataNascimento": None,
            },
        )
        atletas_por_nome[chave] = atleta
        criados["atletas"] += 1
        return atleta

    def obter_dupla(nome):
        atleta_1_nome, atleta_2_nome = separar_dupla(nome)
        atleta_1 = obter_atleta(atleta_1_nome)
        atleta_2 = obter_atleta(atleta_2_nome)
        chave = tuple(sorted([atleta_1["id"], atleta_2["id"]]))
        existente = duplas_por_composicao.get(chave)
        if existente:
            return existente

        dupla = api(
            "POST",
            "/api/duplas",
            {"nome": nome, "atleta1Id": atleta_1["id"], "atleta2Id": atleta_2["id"]},
        )
        duplas_por_composicao[chave] = dupla
        criados["duplas"] += 1
        return dupla

    duplas_por_nome = {}
    for nome in sorted({linha["Dupla1"] for linha in linhas} | {linha["Dupla2"] for linha in linhas}):
        duplas_por_nome[nome] = obter_dupla(nome)

    inscritos_por_categoria = defaultdict(set)
    for campeonato, competicao_id in competicoes_ids.items():
        for (campeonato_categoria, _categoria_nome), categoria_id in categorias_ids.items():
            if campeonato_categoria != campeonato:
                continue
            inscricoes = api("GET", f"/api/campeonatos/{competicao_id}/inscricoes?categoriaId={categoria_id}")
            for inscricao in inscricoes:
                inscritos_por_categoria[categoria_id].add(inscricao["duplaId"])

    for linha in linhas:
        competicao_id = competicoes_ids[linha["Campeonato"]]
        categoria_id = categorias_ids[(linha["Campeonato"], linha["Categoria"])]
        for nome_dupla in (linha["Dupla1"], linha["Dupla2"]):
            dupla = duplas_por_nome[nome_dupla]
            if dupla["id"] in inscritos_por_categoria[categoria_id]:
                ignorados["inscricoes"] += 1
                continue

            try:
                api(
                    "POST",
                    f"/api/campeonatos/{competicao_id}/inscricoes",
                    {
                        "categoriaId": categoria_id,
                        "duplaId": dupla["id"],
                        "atleta1Id": None,
                        "atleta2Id": None,
                        "nomeAtleta1": None,
                        "apelidoAtleta1": None,
                        "nomeAtleta2": None,
                        "apelidoAtleta2": None,
                        "observacao": f"{args.marcador}; inscricao historica por jogo importado.",
                        "pago": True,
                        "atleta1CadastroPendente": False,
                        "atleta2CadastroPendente": False,
                    },
                )
                inscritos_por_categoria[categoria_id].add(dupla["id"])
                criados["inscricoes"] += 1
            except ApiError as erro:
                if "ja esta inscrita" in normalizar(erro.corpo) or "já está inscrita" in erro.corpo:
                    inscritos_por_categoria[categoria_id].add(dupla["id"])
                    ignorados["inscricoes"] += 1
                else:
                    raise

    for categoria_id in categorias_ids.values():
        categoria = api("GET", f"/api/categorias/{categoria_id}")
        if not categoria.get("inscricoesEncerradas", False):
            atualizar_categoria(api, categoria, True)
            atualizados["categorias_fechadas"] += 1

    linhas_importadas = defaultdict(dict)
    for categoria_id in categorias_ids.values():
        partidas = api("GET", f"/api/partidas?categoriaId={categoria_id}")
        for partida in partidas:
            observacoes = partida.get("observacoes") or ""
            if args.marcador in observacoes:
                encontrada = re.search(r"LinhaCSV=(\d+)", observacoes)
                if encontrada:
                    linhas_importadas[categoria_id][int(encontrada.group(1))] = partida

    linhas_por_categoria = defaultdict(list)
    for linha in linhas_partidas:
        linhas_por_categoria[categorias_ids[(linha["Campeonato"], linha["Categoria"])]].append(linha)

    for categoria_id, linhas_categoria in linhas_por_categoria.items():
        categoria = api("GET", f"/api/categorias/{categoria_id}")
        if categoria.get("tabelaJogosAprovada", False):
            continue

        primeira = linhas_categoria[0]
        existente = linhas_importadas[categoria_id].get(primeira["_linha"])
        if existente is None:
            observacoes = (
                f"{args.marcador}; LinhaCSV={primeira['_linha']}; "
                f"FaseOriginal={primeira['Fase']}; TipoChave={primeira['TipoChave']}; "
                f"VencedorCSV={primeira['Vencedor']}; Partida inicial criada agendada para aprovacao da tabela."
            )
            payload = montar_payload_partida(
                primeira,
                categoria_id,
                duplas_por_nome,
                datas_base,
                args.marcador,
                observacoes,
                status=1,
                placar_a=None,
                placar_b=None,
            )
            existente = api("POST", "/api/partidas", payload)
            linhas_importadas[categoria_id][primeira["_linha"]] = existente
            criados["partidas_agendadas_para_aprovacao"] += 1

        try:
            api("POST", f"/api/categorias/{categoria_id}/partidas/aprovar")
            atualizados["tabelas_aprovadas"] += 1
        except ApiError as erro:
            if "ja foi aprovado" not in normalizar(erro.corpo) and "já foi aprovado" not in erro.corpo:
                raise

    for linha in linhas_partidas:
        categoria_id = categorias_ids[(linha["Campeonato"], linha["Categoria"])]
        pontos_1 = placar(linha["Pts1"], linha["_linha"])
        pontos_2 = placar(linha["Pts2"], linha["_linha"])
        if vencedor_csv(linha) == 1 and pontos_1 < pontos_2:
            erros_validacao.append(f"Linha {linha['_linha']}: vencedor CSV diverge do placar.")
        if vencedor_csv(linha) == 2 and pontos_2 < pontos_1:
            erros_validacao.append(f"Linha {linha['_linha']}: vencedor CSV diverge do placar.")

        observacoes = (
            f"{args.marcador}; LinhaCSV={linha['_linha']}; "
            f"FaseOriginal={linha['Fase']}; TipoChave={linha['TipoChave']}; VencedorCSV={linha['Vencedor']}"
        )
        payload = montar_payload_partida(
            linha,
            categoria_id,
            duplas_por_nome,
            datas_base,
            args.marcador,
            observacoes,
            status=2,
            placar_a=pontos_1,
            placar_b=pontos_2,
        )
        existente = linhas_importadas[categoria_id].get(linha["_linha"])
        if existente:
            if existente.get("status") == 2:
                ignorados["partidas"] += 1
                continue
            api("PUT", f"/api/partidas/{existente['id']}", payload)
            atualizados["partidas"] += 1
        else:
            api("POST", "/api/partidas", payload)
            criados["partidas"] += 1

    for competicao_id in competicoes_ids.values():
        competicao = api("GET", f"/api/competicoes/{competicao_id}")
        if competicao.get("inscricoesAbertas", False):
            atualizar_competicao(api, competicao, {"inscricoesAbertas": False})
            atualizados["competicoes_fechadas"] += 1

    status_aprovacao = Counter()
    for categoria_id in linhas_por_categoria:
        partidas = api("GET", f"/api/partidas?categoriaId={categoria_id}")
        for partida in partidas:
            if args.marcador in (partida.get("observacoes") or ""):
                status_aprovacao[str(partida.get("statusAprovacao"))] += 1

    for numero_linha, motivo in linhas_invalidas.items():
        erros_validacao.append(f"Linha {numero_linha}: {motivo}.")

    print(
        json.dumps(
            {
                "linhas_csv": len(linhas),
                "partidas_validas_csv": len(linhas_partidas),
                "criados": dict(criados),
                "atualizados": dict(atualizados),
                "ignorados": dict(ignorados),
                "status_aprovacao_importadas": dict(status_aprovacao),
                "erros_validacao_csv": erros_validacao[:20],
                "quantidade_erros_validacao_csv": len(erros_validacao),
            },
            ensure_ascii=False,
            indent=2,
        )
    )


def montar_payload_partida(linha, categoria_id, duplas_por_nome, datas_base, marcador, observacoes, status, placar_a, placar_b):
    data_partida = datas_base[linha["Campeonato"]] + timedelta(minutes=linha["_linha"])
    return {
        "competicaoId": None,
        "nomeGrupo": None,
        "categoriaCompeticaoId": categoria_id,
        "duplaAId": duplas_por_nome[linha["Dupla1"]]["id"],
        "duplaBId": duplas_por_nome[linha["Dupla2"]]["id"],
        "duplaAAtleta1Id": None,
        "duplaAAtleta1Nome": None,
        "duplaAAtleta2Id": None,
        "duplaAAtleta2Nome": None,
        "duplaBAtleta1Id": None,
        "duplaBAtleta1Nome": None,
        "duplaBAtleta2Id": None,
        "duplaBAtleta2Nome": None,
        "faseCampeonato": fase_importada(linha["Fase"], linha["TipoChave"]),
        "status": status,
        "placarDuplaA": placar_a,
        "placarDuplaB": placar_b,
        "dataPartida": data_partida.isoformat().replace("+00:00", "Z"),
        "observacoes": observacoes,
    }


if __name__ == "__main__":
    try:
        main()
    except Exception as erro:
        print(str(erro), file=sys.stderr)
        raise
