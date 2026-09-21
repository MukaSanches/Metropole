# METRÓPOLE ∞

METRÓPOLE ∞ é um jogo de simulação sistêmica para Windows em que cidadãos, empresas, mercados e bairros continuam evoluindo sem depender do jogador.

## Estado da V1.0.0

Esta base implementa um núcleo jogável e verificável:

- geração procedural determinística por seed;
- mapa isométrico 2D gerado em tempo real;
- população e empresas simuladas localmente;
- carreiras, vagas, salários e desemprego;
- consumo, estoques, produção, oferta/demanda e formação de preços;
- contratação, demissão, crédito, insolvência e falência;
- abertura de microempresa pelo jogador;
- necessidades básicas e progressão financeira;
- calendário e velocidades de simulação independentes do FPS;
- histórico causal de eventos econômicos;
- save manual, autosave, backup e gravação atômica;
- catálogo data-driven/composicional de profissões, negócios, produtos, recursos, edifícios, competências, eventos e tecnologias;
- testes headless do núcleo de simulação;
- pipeline Windows com build, testes, export do Godot e instalador Inno Setup.

O README descreve apenas o que está implementado no código. A distribuição final só deve ser chamada de validada quando o workflow de CI/release estiver verde e o artefato Windows tiver sido executado com sucesso.

## Tecnologia

- Godot Engine 4.7.2 stable .NET
- C# / .NET 8
- núcleo de simulação separado do motor gráfico
- Inno Setup para o instalador Windows

Veja [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Jogando

1. Crie um mundo informando nome e seed.
2. Observe a cidade funcionar no mapa.
3. Abra **Carreira** e aceite uma vaga.
4. Use **Trabalhar +1 dia** para avançar e receber salário.
5. Compre alimentação e acompanhe os preços.
6. Acumule capital e abra uma microempresa.
7. Acelere o tempo e observe contratações, demissões, preços, falências e novas empresas.
8. Salve e continue depois.

## Build local

Requer Godot 4.7.2 .NET e .NET 8.

```powershell
dotnet build src/Metropole.Sim/Metropole.Sim.csproj -c Release
dotnet run --project tests/Metropole.SimTests/Metropole.SimTests.csproj -c Release
dotnet build src/Metropole.Game/Metropole.Game.csproj -c Release
godot --headless --path src/Metropole.Game --export-release Windows build/Metropole.exe
```

## Licenças

O código deste repositório não concede licença de redistribuição por padrão. O jogo usa Godot Engine sob licença MIT. Consulte [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
