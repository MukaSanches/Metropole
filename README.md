# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que vida pessoal, relacionamentos, trabalho, empresas, mercados, bairros e cidadãos evoluem continuamente.

## Versão 1.5.0 — Realismo & Vida Social

A 1.5 foi construída para resolver duas limitações da 1.4: a cidade ainda precisava de materiais/iluminação mais convincentes e a vida social precisava virar gameplay real, não apenas estatística.

### Realismo 3D

O renderer premium continua em Godot 4.7.2 Forward+ e agora adiciona:

- HDRI urbano CC0 da Poly Haven para iluminação/reflexos diurnos;
- asfalto PBR CC0 com albedo, normal e roughness;
- calçada/pavimento PBR CC0;
- superfícies molhadas com roughness dinâmica durante chuva;
- SSR em High/Ultra quando Forward+ estiver ativo;
- SSAO, SSIL, glow e fog adaptativos;
- sol, horário, clima e ambiente continuam reagindo à simulação;
- assets detalhados Kenney continuam sobre uma base procedural/MultiMesh;
- perfis automáticos reduzem custo gráfico antes de reduzir qualquer mecânica.

A Poly Haven publica HDRIs, texturas e modelos sob CC0. Consulte `docs/EXTERNAL_ASSETS_V1.5.md`.

### Pessoas interativas

Cidadãos 3D detalhados agora são vinculados a cidadãos reais da simulação. No renderer premium, clicar em um personagem seleciona aquela pessoa e abre sua ficha no painel Pessoas.

Cada relação com o jogador possui:

- familiaridade;
- afinidade;
- confiança;
- compatibilidade de personalidade;
- histórico de interação;
- estado do vínculo.

Fluxo jogável:

**desconhecido → conhecido → amizade → interesse → namoro → casamento → família**

Ações disponíveis incluem:

- conhecer;
- conversar;
- sair juntos;
- flertar;
- pedir em namoro;
- passar tempo com parceiro;
- pedir em casamento;
- planejar filho;
- terminar relacionamento/casamento.

Tempo e dinheiro usados nessas ações entram na mesma economia do resto do jogo.

### Relacionamentos persistentes

- relações enfraquecem lentamente quando ignoradas;
- namoro exige familiaridade, afinidade e confiança;
- casamento exige tempo mínimo de namoro, vínculo forte e recursos;
- casamento entra no histórico da cidade;
- filhos entram como cidadãos reais da população;
- sucessão encerra corretamente o vínculo romântico da geração anterior;
- casamento e parceiro continuam persistidos no save.

### UI sem cortes

A composição foi reduzida e reorganizada para evitar clipping:

- navegação mais estreita;
- sidebar menor e rolável;
- mapa pode reduzir até 400 px sem quebrar;
- estatísticas do topo ficaram mais compactas;
- painel inicial foi reduzido;
- painéis críticos ganharam validação de bounds.

O executável é testado automaticamente em:

- 1280×720;
- 1366×768;
- 1600×900;
- 1920×1080.

A release falha se TopBar, navegação, mapa, sidebar ou barra de status saírem da área útil.

### Performance

A regra continua sendo: população lógica não é população renderizada.

- cidadãos lógicos: C# determinístico;
- massa visual: MultiMesh;
- personagens próximos: GLB animado;
- Low: remove camada detalhada;
- Medium/High/Ultra: aumentam proxies;
- renderer automático reduz qualidade quando o frame time sobe;
- economia e relacionamentos não dependem de FPS.

## Tecnologia

- Godot Engine 4.7.2 stable .NET
- C# / .NET 8
- Forward+ premium / Compatibility fallback
- Camera3D ortográfica em SubViewport
- MultiMesh para densidade
- GLB/glTF + AnimationPlayer
- Poly Haven PBR/HDRI CC0
- Kenney CC0
- OpenGameArt CC0
- Inno Setup para Windows

## Validação da release

1. build do núcleo;
2. testes de simulação;
3. testes de amizade/namoro/casamento/família;
4. matriz multi-seed de 5 anos;
5. download/verificação dos assets CC0;
6. build C# do Godot;
7. importação de GLB/WAV/OGG/HDR/JPG;
8. export Windows;
9. validação do executável;
10. validação social dentro do executável;
11. validação de layout em quatro resoluções;
12. validação de personagens 3D interativos;
13. validação premium;
14. validação fallback leve;
15. smoke test;
16. instalador;
17. instalação silenciosa;
18. execução pós-instalação;
19. SHA-256.

## Build local

~~~powershell
./tools/fetch_cc0_assets.ps1
dotnet build src/Metropole.Sim/Metropole.Sim.csproj -c Release
dotnet run --project tests/Metropole.SimTests/Metropole.SimTests.csproj -c Release
dotnet build src/Metropole.Game/Metropole.Game.csproj -c Release
godot --headless --path src/Metropole.Game --editor --quit
godot --headless --path src/Metropole.Game --export-release Windows build/Metropole.exe
~~~

## Engenharia e licenças

- `skills/godot-master/SKILL.md`
- `docs/GODOT_VISUAL_MECHANICS_ROADMAP.md`
- `docs/EXTERNAL_ASSETS_V1.5.md`
- `THIRD_PARTY_NOTICES.md`
