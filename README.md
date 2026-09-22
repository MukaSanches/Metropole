# METRÓPOLE ∞ — 1.9 Vida e escolhas

## AAA Visual Overhaul — rendering real-time

A cidade premium foi refeita para aproximar o jogo da referência visual aprovada **sem usar imagem pré-renderizada**. O que aparece no mapa é rendering 3D real do Godot:

- câmera em perspectiva e composição aérea cinematográfica;
- Forward+ Auto/Ultra em hardware compatível;
- fachadas PBR procedurais com vidro, janelas e emissão noturna;
- volumes de cobertura, skyline mais alto e menos aparência de blocos simples;
- TAA/MSAA, SSAO, SSIL, SSR, glow, AGX e fog adaptativos;
- faixas de pedestre, geometria de postes/luminárias e ruas mais detalhadas;
- árvores com copas compostas;
- mais edifícios CC0, props, veículos e cidadãos detalhados;
- queda automática de qualidade se o frame time subir, preservando gameplay e fallback leve.

Detalhes técnicos: `docs/AAA_VISUAL_OVERHAUL_V1.9.md`.

## 1.9.0 — Vida e escolhas

- Nova tela Cotidiano com 18 atividades, 5 necessidades, 3 rotinas executáveis, prática de competências, 5 marcos e diário limitado a 64 registros.
- Cada ação tem duração, custo, condições e efeitos reais; repetir a mesma ação exige intervalo de 4h. Custos são transferidos à tesouraria.
- Convites semanais contextuais para reparos, descanso ou comunidade; aceitar executa a atividade, recusar preserva o tempo.
- Decisões horárias dos cidadãos com prioridades de descanso, escola, emprego, convivência, estudo e exercício, sensíveis a personalidade, chuva e fim de semana; explicação na ficha.
- Save schema 3 com migração não destrutiva dos schemas 1/2. Rotina e diário persistentes; sucessão preserva casa/diário e reinicia a prática pessoal.
- Seis novos grupos de testes: efeitos e custos, rotinas/prática, relógio, migração real, decisões e determinismo/eventos. Validação do executável inclui Cotidiano.

Limites: atividades ocorrem pela interface e pelo relógio simulado; não há novos interiores exploráveis ou animação exclusiva para cada atividade. Os cidadãos usam regras locais determinísticas, sem IA generativa. A versão não representa todas as atividades possíveis da vida real. Não há benchmark de FPS em hardware do jogador.

Abra **Cotidiano** na navegação lateral. Escolha uma atividade ou execute uma rotina. Confira duração, custo e bloqueios antes de agir. O diário registra escolhas; a ficha de cada cidadão explica sua atividade.

### Versões anteriores

# METRÓPOLE ∞ — 1.8 Living Streets

Evolução da base 1.7, mantendo simulação, saves e fallback gráfico existentes.

- Praças com fontes de malhas compartilhadas e água animada por shader, apenas nos perfis High/Ultra.
- Vitrines com toldos e terraços com quatro modelos CC0 antes apenas baixados.
- Pedestres com percurso fechado, curvas suaves e animações com transição de 220 ms.
- Personagens parados deixam de deslizar; corrida e caminhada usam prioridade de clipes.
- Bairro visual acompanha o cidadão; personagens dormindo ou falecidos deixam as ruas.
- Personagens ocultos têm processamento suspenso; orçamento de detalhes por perfil preservado.
- Tráfego integrado por delta: mudanças de horário e chuva alteram velocidade sem saltos de posição.
- Removida alocação de dicionário de toda a população a cada atualização visual.

Validação da 1.8: consulte o workflow Windows V1.8. Não há garantia de FPS em hardware não testado.

---

# METRÓPOLE ∞

METRÓPOLE ∞ é um simulador sistêmico para Windows em que vida pessoal, relacionamentos, trabalho, empresas, mercados, bairros e cidadãos evoluem continuamente.

## Versão 1.7.0 — Visual Leap

A 1.7 preserva a simulação sistêmica da 1.6 e concentra o ciclo em apresentação, variedade visual e animação.

### Cidade e assets

- biblioteca urbana CC0 ampliada com dezenas de edifícios comerciais e industriais;
- oito residências KayKit e mobiliário urbano CC0;
- variedade ampliada de carros, utilitários e veículos de serviço;
- equipamentos e props industriais;
- cinco edifícios 3D detalhados por distrito;
- até 48 veículos detalhados e 48 cidadãos detalhados;
- props distribuídos conforme o perfil do distrito;
- iluminação pública noturna adaptativa;
- HDRI/PBR, chuva, reflexos, SSAO, SSIL, SSR, glow e fog continuam adaptativos.

### Animação

- todos os clips válidos encontrados nos personagens importados são catalogados;
- seleção contextual por atividade;
- deslocamento prioriza walk/jog/run;
- lazer e socialização priorizam variações compatíveis como wave/dance/idle;
- trabalho e estudo procuram clips de interação antes do fallback;
- nenhum clip inexistente é inventado.

### Menu principal

- cidade 3D viva como fundo;
- câmera cinematográfica suave;
- tráfego e pedestres animados;
- continuar partida e criar novo mundo;
- créditos/licenças;
- saída do jogo;
- o menu 3D faz parte da validação automática da release.

## Versão 1.6.0 — AAA Simulation Core

A 1.6 preserva o renderer, áudio e vida social da 1.5 e adiciona uma camada persistente de simulação de vida preparada para escalar sem transformar cada cidadão em um Node 3D.

### Núcleo sistêmico

- quatro níveis de detalhe: Statistical, Regional, Active e Interactive;
- scheduler horário por lotes, com cursor persistente;
- dez necessidades por cidadão;
- personalidade ampliada;
- memória social/profissional com decay e limite;
- utility goal determinístico;
- affordances data-driven para cama, geladeira, chuveiro, banheiro, TV, computador, telefone e veículo;
- domicílios e propriedades persistentes;
- veículos lógicos e tráfego entre distritos;
- agregação por região;
- orçamento explícito de performance;
- save schema 2 com migração de saves da 1.5;
- painel F1 com diagnóstico da simulação;
- probe automatizado de LOD para 10.000 agentes.

A regra permanece: **simular muito e renderizar apenas o necessário**.

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
- KayKit CC0
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
- `docs/EXTERNAL_ASSETS_V1.7.md`
- `docs/EXTERNAL_ASSETS_V1.5.md`
- `THIRD_PARTY_NOTICES.md`
