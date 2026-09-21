# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que cidadãos, empresas, mercados e bairros continuam evoluindo sem depender do jogador.

## Versão 1.1.0

Esta versão transforma a primeira base técnica em uma experiência muito mais legível e jogável:

- interface refeita com navegação, hierarquia visual e painéis compactos;
- topbar corrigida: dinheiro, emprego, data e controles não ficam mais espremidos;
- textos multilinha corrigidos;
- mapa isométrico procedural redesenhado com edifícios volumétricos, vias, árvores, iluminação e tráfego animado;
- identidade vetorial própria em SVG;
- tela inicial refeita;
- painel de cidade com mudança real de bairro e custo econômico;
- carreira com trabalhar, descansar e pedir demissão;
- mercado com compra em quantidades e leitura de oferta/demanda;
- gestão de empresa com aportes e ajuste de vagas;
- atalhos Ctrl+S e Esc;
- janela mínima de 1280×720 para preservar a composição;
- smoke test do executável exportado e do executável instalado.

O núcleo continua incluindo geração procedural determinística, população e empresas simuladas, preços dinâmicos, contratação/demissão, crédito, insolvência, demografia, sucessão familiar, save atômico e execução offline.

## Tecnologia

- Godot Engine 4.7.2 stable .NET
- C# / .NET 8
- núcleo de simulação separado do motor gráfico
- Inno Setup para o instalador Windows
- assets SVG próprios

## Jogando

1. Crie um mundo ou continue seu save.
2. Abra **Carreira** e aceite uma vaga.
3. Trabalhe para ganhar dinheiro e descanse para recuperar energia.
4. Use **Mercado** para comprar alimentação e acompanhar a economia.
5. Explore **Cidade** e mude de bairro quando fizer sentido.
6. Acumule Cr$ 5.000 e abra sua empresa.
7. Ajuste vagas, faça aportes e acompanhe o caixa.
8. Acelere o tempo e observe a cidade evoluir.
9. Use Ctrl+S para salvar.

## Build local

Requer Godot 4.7.2 .NET e .NET 8.

```powershell
dotnet build src/Metropole.Sim/Metropole.Sim.csproj -c Release
dotnet run --project tests/Metropole.SimTests/Metropole.SimTests.csproj -c Release
dotnet build src/Metropole.Game/Metropole.Game.csproj -c Release
godot --headless --path src/Metropole.Game --export-release Windows build/Metropole.exe
```

## Licenças

O código deste repositório não concede licença de redistribuição por padrão. O jogo usa Godot Engine sob licença MIT. Consulte `THIRD_PARTY_NOTICES.md`.
