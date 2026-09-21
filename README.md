# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que vida pessoal, trabalho, empresas, mercados, bairros e cidadãos evoluem continuamente.

## Versão 1.2.0 — Vida & Empresas Profundas

A 1.2 amplia o jogo de um simulador econômico para uma simulação integrada de vida e negócios:

- relógio horário com rotina diária, trabalho, sono, estudo, lazer e deslocamento;
- necessidades de energia, fome, saúde, estresse, felicidade, vida social e condicionamento;
- educação, experiência profissional, reputação de carreira e relacionamentos;
- cidadãos com personalidade, escolaridade, atividade atual, humor, estresse, parceiro e mobilidade profissional;
- painel **Pessoas** para acompanhar vidas individuais e acontecimentos humanos;
- branding empresarial com nome de marca, slogan, awareness e fidelidade;
- estratégias de preço, orçamento de marketing, qualidade, inovação e reputação;
- RH com salários, moral, vagas, produtividade e troca de empregos;
- DRE diária com receita, folha, operação, aluguel, marketing, impostos e resultado;
- histórico financeiro de 90 dias com gráfico;
- market share, rival direto e intensidade de rivalidade;
- IA concorrente que ajusta preço, marketing e estratégia;
- demanda institucional para estabilizar a circulação monetária sem criar dinheiro;
- recuperação do ecossistema empresarial quando a quantidade de empresas cai;
- estados empresariais Ativa, Atenção, Crise e Encerrada;
- ciclo visual dia/noite, clima, chuva, neblina, estrelas, luzes, tráfego e pedestres;
- cidade procedural enriquecida sem depender de assets externos proprietários.

## Referências de design

A arquitetura da 1.2 foi pesquisada contra padrões de simuladores de vida, cidade e negócios como Software Inc., Big Ambitions, Capitalism Lab, Cities: Skylines II e The Sims 4. As mecânicas foram reinterpretadas para o METRÓPOLE, sem copiar código ou assets. Veja `docs/RESEARCH_V1.2.md`.

## Tecnologia

- Godot Engine 4.7.2 stable .NET
- C# / .NET 8
- Compatibility renderer
- desenho procedural 2D via CanvasItem
- núcleo de simulação separado da camada gráfica
- Inno Setup para Windows
- assets SVG próprios

## Loop de jogo

1. Crie ou continue um mundo.
2. Use **Vida** para administrar tempo, saúde, estresse, estudo e socialização.
3. Use **Carreira** para conseguir emprego e construir patrimônio.
4. Acompanhe cidadãos reais em **Pessoas**.
5. Leia oferta, demanda e preços em **Mercado**.
6. Mude de bairro em **Cidade** conforme custo e qualidade de vida.
7. Funde uma empresa com Cr$ 5.000.
8. Defina marca, slogan, preço, marketing, salários, vagas, qualidade e P&D.
9. Acompanhe DRE, caixa, dívida, market share e o rival direto.
10. Acelere o relógio e observe cidadãos e empresas reagirem ao mesmo sistema.

## Build local

Requer Godot 4.7.2 .NET e .NET 8.

```powershell
dotnet build src/Metropole.Sim/Metropole.Sim.csproj -c Release
dotnet run --project tests/Metropole.SimTests/Metropole.SimTests.csproj -c Release
dotnet build src/Metropole.Game/Metropole.Game.csproj -c Release
godot --headless --path src/Metropole.Game --export-release Windows build/Metropole.exe
```

## Licenças

O jogo usa Godot Engine sob licença MIT. Consulte `THIRD_PARTY_NOTICES.md`.
