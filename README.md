# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que vida pessoal, trabalho, empresas, mercados, bairros e cidadãos evoluem continuamente.

## Versão 1.3.0 — Renderer Adaptativo

A 1.3 preserva toda a simulação de vida e empresas da 1.2 e muda o teto gráfico do jogo.

### Cidade premium

Em hardware compatível, o jogo usa o caminho 3D/2.5D:

- Godot Forward+;
- câmera 3D ortográfica isométrica;
- zoom e pan suaves;
- prédios 3D procedurais;
- bairros com riqueza/atividade refletidas visualmente;
- empresa do jogador destacada;
- estados empresariais refletidos na aparência;
- estradas, calçadas e malha urbana;
- árvores instanciadas;
- tráfego instanciado e variável por horário;
- pedestres instanciados e afetados por horário/clima;
- iluminação solar dinâmica;
- dia/noite;
- chuva e neblina;
- SSAO, SSIL, glow e volumetric fog ativados somente quando renderer/perfil permitem.

### Desempenho

A arquitetura foi desenhada para evitar a regra “um Node para cada cidadão”.

- MultiMesh agrupa prédios, árvores, carros e pedestres;
- a população lógica continua no núcleo C# determinístico;
- só proxies visuais são renderizados;
- a densidade visual é ajustada sem alterar a economia;
- o renderer antigo em CanvasItem permanece como fallback leve;
- se o Godot cair em gl_compatibility, o jogo escolhe automaticamente o renderer 2D;
- perfil AUTO monitora frame time e reduz/aumenta densidade com histerese;
- perfis manuais Ultra / High / Medium / Low podem ser selecionados na própria tela da cidade.

### Validação

O pipeline Windows testa separadamente:

1. simulação e matriz multi-seed;
2. compilação Godot C#;
3. importação;
4. exportação Windows;
5. interface no renderer automático;
6. estrutura 3D premium forçada em Compatibility;
7. fallback 2D forçado;
8. smoke test do executável;
9. geração do instalador;
10. instalação silenciosa;
11. execução pós-instalação;
12. SHA-256.

## Simulação

A 1.3 mantém os sistemas anteriores:

- relógio horário;
- energia, fome, saúde, estresse, felicidade, social e condicionamento;
- estudo e progressão profissional;
- cidadãos com personalidade e rotina;
- emprego e mobilidade profissional;
- relacionamentos;
- bairros;
- mercados;
- empresas;
- branding;
- preço e estratégia;
- marketing;
- qualidade e P&D;
- RH;
- DRE;
- dívida e capital de giro;
- market share;
- rivalidade;
- IA concorrente;
- crise e falência;
- histórico financeiro;
- sucessão.

## Tecnologia

- Godot Engine 4.7.2 stable .NET
- C# / .NET 8
- Forward+ como caminho gráfico premium
- Compatibility como fallback
- SubViewport + Camera3D ortográfica
- MultiMesh para instancing
- CanvasItem renderer 2D preservado
- núcleo de simulação separado da camada gráfica
- Inno Setup para Windows
- pipeline de assets CC0 auditável em `tools/fetch-third-party-assets.ps1`
- áudio dinâmico por buses UI / Weather / Ambience

## Engenharia Godot

A referência permanente do projeto está em:

- skills/godot-master/SKILL.md
- docs/GODOT_VISUAL_MECHANICS_ROADMAP.md
- docs/RESEARCH_V1.2.md

## Build local

Requer Godot 4.7.2 .NET e .NET 8.

~~~powershell
dotnet build src/Metropole.Sim/Metropole.Sim.csproj -c Release
dotnet run --project tests/Metropole.SimTests/Metropole.SimTests.csproj -c Release
dotnet build src/Metropole.Game/Metropole.Game.csproj -c Release
godot --headless --path src/Metropole.Game --export-release Windows build/Metropole.exe
~~~

## Licenças

O jogo usa Godot Engine sob licença MIT. Consulte THIRD_PARTY_NOTICES.md.
