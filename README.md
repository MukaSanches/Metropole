# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que vida pessoal, trabalho, empresas, mercados, bairros e cidadãos evoluem continuamente.

## Versão 1.5.0 — Premium Polish

A 1.5 é uma versão de acabamento. O objetivo não é aumentar o número de sistemas por aumentar, mas elevar a apresentação 3D ao nível de uma produção comercial mantendo o fallback para computadores modestos.

### Render premium

- Godot Forward+ como caminho de maior qualidade;
- câmera ortográfica isométrica com movimento, zoom e rotação suavizados;
- foco na atividade principal do jogador;
- material PBR para asfalto com resposta à chuva;
- iluminação urbana local com orçamento por perfil gráfico;
- emissão noturna e acentos de distrito;
- correção de cor e atmosfera dependentes do clima;
- SSAO/SSIL/glow/volumetric fog somente onde o renderer e o perfil permitem;
- LOD e antialiasing ajustados automaticamente à qualidade;
- assets CC0 detalhados da 1.4 preservados.

### Cidadãos visuais ligados à simulação

Os personagens 3D não são mais apenas figurantes independentes.

Cada proxy detalhado referencia um cidadão lógico real:
- usa o bairro atual desse cidadão;
- some das ruas quando está dormindo/em casa;
- alterna movimento/idle conforme atividade;
- animação reage à energia;
- a simulação continua existindo mesmo quando o proxy visual é removido por desempenho.

### Áudio

- UI CC0;
- ambiência urbana;
- chuva sincronizada com a meteorologia;
- crossfade em vez de cortes secos;
- intensidade muda por horário de pico, noite e chuva forte.

### Controles da cidade

Na visão premium:
- roda do mouse: zoom;
- botão direito/meio + arrastar: pan;
- **FOCAR**: centraliza bairro/empresa do jogador;
- **GIRAR**: rotaciona a câmera 90°;
- **RESET**: volta para a visão geral;
- **GRÁFICOS**: Auto / Ultra / High / Medium / Low.

### Desempenho

A política da 1.5 permanece:

**simular muito e renderizar apenas o necessário.**

- núcleo C# determinístico separado do rendering;
- MultiMesh para massa urbana;
- proxies 3D detalhados somente em quantidade limitada;
- iluminação local sem sombras;
- LOD mais agressivo em máquinas fracas;
- modo Low elimina os efeitos mais caros;
- Compatibility/CanvasItem continua como fallback.

### Validação da release

A build Windows precisa passar por:
1. build do núcleo;
2. matriz de testes da simulação;
3. aquisição/verificação dos assets CC0;
4. build C# Godot;
5. importação de GLB/WAV/OGG/shaders;
6. exportação Windows;
7. validação automática;
8. validação premium forçada;
9. validação do fallback leve;
10. verificação de modelos, AnimationPlayer, áudio e camada 1.5;
11. smoke test;
12. instalador;
13. instalação silenciosa;
14. execução pós-instalação;
15. SHA-256.

## Tecnologia

- Godot Engine 4.7.2 stable .NET
- C# / .NET 8
- Forward+ premium
- Compatibility fallback
- PBR StandardMaterial3D + ShaderMaterial
- TAA/MSAA/SMAA/FXAA por perfil
- MultiMesh
- glTF/GLB
- AnimationPlayer
- AudioStreamPlayer
- Inno Setup

## Engenharia

- `skills/godot-master/SKILL.md`
- `docs/GODOT_VISUAL_MECHANICS_ROADMAP.md`
- `docs/EXTERNAL_ASSETS_V1.4.md`

## Build local

~~~powershell
./tools/fetch_cc0_assets.ps1
dotnet build src/Metropole.Sim/Metropole.Sim.csproj -c Release
dotnet run --project tests/Metropole.SimTests/Metropole.SimTests.csproj -c Release
dotnet build src/Metropole.Game/Metropole.Game.csproj -c Release
godot --headless --path src/Metropole.Game --editor --quit
godot --headless --path src/Metropole.Game --export-release Windows build/Metropole.exe
~~~

## Licenças

Godot Engine usa MIT. Os assets externos selecionados usam CC0; consulte `THIRD_PARTY_NOTICES.md`.
