# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que vida pessoal, trabalho, empresas, mercados, bairros e cidadãos evoluem continuamente.

## Versão 1.4.0 — Cidade, personagens, animação e áudio CC0

A 1.4 preserva a arquitetura escalável da 1.3 e substitui parte importante da apresentação genérica por uma camada curada de assets gratuitos e publicáveis.

### Assets 3D reais

No renderer premium, a cidade agora combina a infraestrutura procedural/MultiMesh com modelos CC0:

- edifícios comerciais Kenney;
- edifícios industriais Kenney;
- kit urbano/viário Kenney;
- carros civis, táxi, van e entrega;
- veículos de serviço;
- personagens Mini Characters;
- personagens com animações importadas diretamente dos GLB.

A camada externa é adicional: o renderer procedural continua sendo a base de escala e o fallback de baixo custo.

### Animação

- cidadãos detalhados recebem animação de caminhada importada quando disponível;
- movimento no mundo continua vinculado ao relógio e à atividade urbana;
- veículos detalhados percorrem a malha viária;
- tráfego e população visual continuam escalando conforme o perfil gráfico;
- animação visual nunca altera a verdade da simulação.

### Áudio

A 1.4 adiciona uma primeira paisagem sonora real:

- clique, hover, confirmação, erro, abrir e voltar;
- ambiência urbana CC0;
- chuva CC0 ligada ao clima real da simulação;
- volume da cidade varia entre dia e noite;
- chuva forte tem presença maior;
- áudio é opcional para a simulação: falha sonora nunca corrompe economia/save.

### Licenciamento

Os assets externos desta versão são CC0. O build baixa arquivos pinados/reprodutíveis e gera SHA-256 do pacote externo.

Veja:
- `docs/EXTERNAL_ASSETS_V1.4.md`
- `THIRD_PARTY_NOTICES.md`

### Desempenho

- Forward+ continua sendo o caminho premium;
- Compatibility/CanvasItem continua sendo o fallback;
- Low remove a camada detalhada externa;
- Medium usa uma amostra pequena;
- High/Ultra aumentam edifícios, veículos e personagens;
- população lógica continua independente da quantidade de modelos renderizados;
- MultiMesh continua responsável pela massa visual barata.

### Validação

A release só passa se:

1. núcleo C# compilar;
2. testes da simulação passarem;
3. assets CC0 pinados forem baixados e validados;
4. Godot importar os GLB/WAV/OGG;
5. o projeto C# do Godot compilar;
6. o executável exportado abrir;
7. UI e gameplay validarem;
8. modo premium carregar modelos detalhados;
9. pelo menos um personagem importar AnimationPlayer;
10. os 8 assets de áudio obrigatórios carregarem;
11. fallback leve continuar funcionando;
12. instalador gerar, instalar e abrir.

## Tecnologia

- Godot Engine 4.7.2 stable .NET
- C# / .NET 8
- Forward+ premium / Compatibility fallback
- SubViewport + Camera3D ortográfica
- MultiMesh para escala
- GLB/glTF para assets detalhados
- AnimationPlayer para clips importados
- AudioStreamPlayer para UI/ambiente
- núcleo determinístico separado da apresentação
- Inno Setup para Windows

## Engenharia Godot

- `skills/godot-master/SKILL.md`
- `docs/GODOT_VISUAL_MECHANICS_ROADMAP.md`
- `docs/RESEARCH_V1.2.md`
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

Godot Engine usa MIT. Os assets externos da 1.4 são CC0; consulte `THIRD_PARTY_NOTICES.md`.
