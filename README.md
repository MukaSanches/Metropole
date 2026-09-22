# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que vida pessoal, trabalho, empresas, mercados, bairros e cidadãos evoluem continuamente.

## Versão 1.6.0 — Systemic Life

A 1.6 preserva todo o acabamento 3D da 1.5 e amplia o núcleo de vida: cidadãos agora usam **Population LOD**, **Utility AI**, necessidades adicionais, objetivos, grafo social persistente, memórias, moradias lógicas e ações contextuais por affordance.

### Vida sistêmica 1.6

- quatro níveis de simulação: Interactive, Active, Regional e Abstract;
- orçamento padrão de até 24 cidadãos interativos e 96 locais ativos;
- cidadãos distantes continuam evoluindo com cadence agregada em vez de virarem personagens 3D completos;
- Utility AI escolhe sono, alimentação, higiene, socialização, lazer, estudo, trabalho e rotina;
- novas necessidades: saúde, higiene, social, diversão e conforto;
- objetivo atual persistente por cidadão;
- grafo social com familiaridade, amizade, confiança, atração, respeito, ressentimento e romance;
- memórias sociais com importância e decaimento;
- households convertidos em moradias lógicas persistentes;
- affordances data-driven para cama, alimentação, chuveiro, sofá/TV, socialização e estudo;
- interface Vida mostra necessidades, objetivo, affordances e diagnóstico do Population LOD;
- saves 1.5/schema 1 migram automaticamente para schema 2.

A arquitetura está detalhada em `docs/SYSTEMIC_LIFE_V1.6.md`.

### Premium Polish preservado

A camada visual da 1.5 continua ativa: Forward+, cidade 3D, PBR, clima, áudio, proxies detalhados, LOD gráfico e fallback para computadores modestos.

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

A política da 1.6 permanece:

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
10. verificação de modelos, AnimationPlayer, áudio e camada sistêmica 1.6;
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
- `docs/SYSTEMIC_LIFE_V1.6.md`

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
