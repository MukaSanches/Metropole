# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que vida pessoal, trabalho, empresas, mercados, bairros e cidadãos evoluem continuamente.

## Versão 1.4.0 — Assets, Animação & Áudio

A 1.4 mantém a arquitetura adaptativa da 1.3 e substitui parte da aparência de protótipo por conteúdo visual e sonoro real, gratuito e auditado.

### Assets integrados

- prédios urbanos Kenney CC0 usados como landmarks de alta qualidade;
- luminárias e props urbanos Kenney CC0;
- seis cidadãos Quaternius CC0, rigados e com animações embutidas;
- walking animation real para cidadãos próximos da câmera;
- áudio de tráfego, chuva e vento com licença CC0 rastreada;
- efeitos de clique e hover Kenney UI Audio;
- `docs/ASSET_PROVENANCE_V1.4.md` documenta cada origem e licença.

### Cidade premium

Em hardware compatível, o jogo usa o caminho 3D/2.5D:

- Godot Forward+;
- câmera 3D ortográfica isométrica;
- zoom e pan suaves;
- prédios 3D procedurais para escala + landmarks GLB reais nos perfis High/Ultra;
- bairros com riqueza/atividade refletidas visualmente;
- empresa do jogador destacada;
- estados empresariais refletidos na aparência;
- estradas, calçadas e malha urbana;
- árvores instanciadas;
- tráfego instanciado e variável por horário;
- pedestres MultiMesh para escala + cidadãos GLB animados próximos da câmera;
- iluminação solar dinâmica;
- dia/noite;
- chuva e neblina;
- ambiência dinâmica de trânsito, chuva e vento;
- feedback sonoro de interface;
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

A 1.4 mantém os sistemas anteriores:

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
- GLB importado para landmarks e cidadãos próximos
- AnimationPlayer para clips humanoides
- AudioServer + buses UI/Ambience/Weather
- CanvasItem renderer 2D preservado
- núcleo de simulação separado da camada gráfica
- Inno Setup para Windows

## Engenharia Godot

A referência permanente do projeto está em:

- skills/godot-master/SKILL.md
- docs/GODOT_VISUAL_MECHANICS_ROADMAP.md
- docs/RESEARCH_V1.2.md
- docs/ASSET_PROVENANCE_V1.4.md

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
