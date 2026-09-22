# Changelog

## 1.6.0 - 2026-09-22

### AAA Simulation Core
- simulação LOD Statistical/Regional/Active/Interactive;
- scheduler horário em lotes;
- necessidades ampliadas e personalidade persistente;
- memória com decay e utility goals;
- affordances data-driven;
- domicílios, propriedades e veículos lógicos;
- malha de tráfego e congestionamento por distrito;
- agregação regional e orçamento de performance;
- save schema 2 com migração de saves 1.5;
- F1 com diagnóstico de população/LOD/tráfego/scheduler;
- testes determinísticos e probe de 10.000 agentes;
- renderer, assets, áudio e vida social da 1.5 preservados.


## 1.5.0 - 2026-09-21

### Realismo 3D
- HDRI urbano Poly Haven CC0;
- asfalto PBR com albedo/normal/roughness;
- pavimento PBR;
- roughness dinâmica de piso em chuva;
- SSR em Forward+ High/Ultra;
- iluminação/reflexos diurnos baseados em HDRI;
- noite continua usando atmosfera dinâmica própria.

### Vida social
- cidadãos 3D detalhados vinculados a cidadãos reais da simulação;
- seleção/interação de pessoas;
- familiaridade, afinidade, confiança e compatibilidade;
- conhecer, conversar, sair, flertar;
- namoro;
- casamento;
- tempo de qualidade;
- planejamento familiar;
- filhos entram na população;
- término de relacionamento;
- decay de relações sem contato;
- vínculos persistidos no save.

### Interface
- topbar compactada;
- navegação reduzida;
- sidebar reduzida e rolável;
- mapa com mínimo menor;
- cards sociais interativos;
- foco de câmera em pessoa selecionada;
- validação automática contra clipping em 1280×720, 1366×768, 1600×900 e 1920×1080.

### QA
- teste determinístico do ciclo social completo;
- validação de conservação monetária em ações sociais;
- validação de casamento/família no executável exportado;
- validação de cidadão 3D interativo;
- assets Poly Haven obrigatórios no pipeline;
- premium/fallback, instalação e smoke test continuam obrigatórios.

## 1.4.0 - 2026-09-21

### Assets
- camada curada de modelos CC0 da Kenney;
- edifícios comerciais e industriais detalhados;
- carros, táxi, entrega, van e veículos de serviço;
- personagens Mini Characters;
- aquisição pinada por commit e manifest SHA-256;
- assets detalhados desligados no perfil Low.

### Animação
- personagens detalhados usam AnimationPlayer importado dos GLB;
- seleção automática de clip de caminhada com fallback para idle;
- veículos detalhados animados sobre a malha viária;
- camada detalhada continua separada da simulação lógica.

### Áudio
- sons CC0 de UI da Kenney;
- ambiência urbana CC0;
- chuva CC0 sincronizada com o clima;
- volume ambiente ajustado por horário e intensidade da chuva.

### Performance
- renderer procedural/MultiMesh da 1.3 continua responsável pela densidade em massa;
- modelos detalhados funcionam como proxies próximos;
- Medium/High/Ultra controlam a quantidade da camada detalhada;
- Low mantém a cidade leve sem depender dos GLB detalhados.

### QA
- CI baixa apenas assets aprovados e pinados;
- valida presença e tamanho mínimo;
- Godot importa GLB/WAV/OGG antes da exportação;
- valida quantidade de assets detalhados;
- valida AnimationPlayer real em personagem;
- valida carregamento de oito streams obrigatórios de áudio;
- mantém testes premium, fallback, installer e execução pós-instalação.

## 1.3.0 - 2026-09-21

### Renderer
- Forward+ passa a ser o caminho gráfico premium;
- fallback automático mantém Compatibility/CanvasItem para hardware leve;
- nova cidade 3D/2.5D com câmera ortográfica;
- zoom e pan;
- prédios, árvores, tráfego e pedestres por MultiMesh;
- estados empresariais e bairros influenciam a representação visual;
- sol e iluminação atmosférica por horário;
- chuva e neblina;
- SSAO/SSIL/glow/volumetric fog condicionais ao renderer e perfil.

### Performance
- perfis Auto, Ultra, High, Medium e Low;
- Auto monitora frame time e ajusta densidade sem alterar a simulação;
- milhares de entidades lógicas não viram milhares de Nodes;
- grupos instanciados têm visible_instance_count ajustável;
- cidade premium só reconstrói estrutura em mudanças relevantes;
- renderer 2D da 1.2 preservado como modo de segurança.

### QA
- validação do executável com renderer automático;
- validação explícita da cidade premium;
- validação explícita do fallback 2D;
- export, instalação e execução pós-instalação continuam obrigatórios.

## 1.2.0 - 2026-09-21

### Vida
- relógio horário e passagem de tempo subdiária;
- rotina automática para jogador e cidadãos;
- saúde, estresse, felicidade, vida social e condicionamento;
- estudo, educação, experiência de carreira e reputação;
- relacionamentos emergentes para NPCs e jogador;
- painel Pessoas com atividades e histórias individuais.

### Empresas
- marca, slogan, awareness e fidelidade;
- preço e estratégias comerciais;
- marketing diário;
- qualidade e inovação;
- moral de funcionários e salários;
- DRE diária e histórico financeiro;
- market share e rivalidade;
- IA concorrente;
- estados de atenção, crise e encerramento;
- capital de giro inicial via crédito;
- mecanismos de reposição de empresas para evitar colapso estrutural.

### Economia
- escolha de fornecedor por preço, marca, qualidade, reputação e proximidade;
- demanda institucional com transferência real do Tesouro;
- custos separados em folha, operação, aluguel, marketing e tributos;
- ajuste de sobrevivência empresarial e nascimento de novos negócios.

## 1.1.0 - 2026-09-21

- interface redesenhada;
- mapa isométrico procedural melhorado;
- painel Cidade;
- gestão básica de empresa;
- melhorias de carreira, mercado e navegação.

## 1.0.0 - 2026-09-21

- núcleo determinístico inicial;
- população, empresas, mercados e bairros;
- carreiras, consumo, produção, crédito e falência;
- save/load atômico;
- testes headless e pipeline Windows.
