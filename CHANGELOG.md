# Changelog

## 1.6.0 - 2026-09-22

### Vida sistêmica
- Population LOD com níveis Interactive, Active, Regional e Abstract;
- orçamento lógico de cidadãos próximos sem converter toda a população em Nodes 3D;
- cadence de atualização agregada para população regional e abstrata;
- Utility AI para trabalho, estudo, sono, alimentação, higiene, socialização, lazer e rotina;
- objetivo atual persistente em jogador e cidadãos;
- saúde, higiene, necessidade social, diversão e conforto adicionados ao modelo.

### Relações e moradia
- grafo social persistente com familiaridade, amizade, confiança, atração, respeito, ressentimento e romance;
- interações sociais limitadas por orçamento e indexadas para long-runs;
- memórias sociais com peso emocional, importância, decaimento e limite;
- households materializados como moradias lógicas com moradores, capacidade, qualidade e aluguel de referência.

### Interações
- catálogo de affordances data-driven;
- ações contextuais de dormir, comer, banho, relaxamento, socialização e estudo;
- painel Vida exibe novas necessidades, objetivo e diagnóstico da simulação;
- painel Pessoas exibe objetivo e nível de detalhe de cada cidadão em destaque.

### Persistência
- schema de save elevado para 2;
- migração automática de saves schema 1 da 1.5;
- backup atômico preservado.

### QA e performance
- testes de LOD, Utility AI, affordances, relações, moradias e migração;
- testes econômicos e long-run existentes mantidos;
- grafo social indexado por par e limitado para evitar crescimento indefinido;
- nenhuma dependência third-party nova adicionada ao core 1.6.

## 1.5.0 - 2026-09-21

### Acabamento 3D
- câmera isométrica com interpolação suave de posição, zoom e rotação;
- foco rápido no bairro/empresa do jogador;
- rotação de câmera em 90° e reset de visão;
- material PBR dedicado para asfalto, com roughness/specular reagindo à chuva;
- correção de cor dinâmica por clima;
- névoa e scattering ajustados ao estado meteorológico;
- glow e volumetric fog limitados aos perfis/renderers adequados;
- luminárias urbanas com emissão e luz local sem sombras;
- acentos luminosos de distrito;
- anti-aliasing, TAA/MSAA/SMAA/FXAA e mesh LOD adaptados ao perfil de qualidade.

### Cidade e cidadãos
- personagens 3D detalhados passam a representar cidadãos reais;
- posição de pedestres segue o bairro real do cidadão;
- cidadãos dormindo/em casa deixam de aparecer andando na rua;
- animação alterna entre movimento e idle conforme atividade;
- velocidade da animação reage à energia do cidadão;
- tráfego e pedestres continuam separados da verdade lógica da simulação.

### Áudio
- transições de volume da cidade e chuva agora usam crossfade;
- ambiência muda suavemente entre dia, horário de pico e noite;
- chuva leve/forte muda intensidade sem cortes abruptos.

### Performance
- iluminação local é limitada por perfil: Ultra > High > Medium > Low;
- Low não usa luzes locais nem camada detalhada pesada;
- mesh LOD fica mais agressivo em perfis inferiores;
- efeitos Forward+ permanecem condicionados ao renderer;
- simulação continua independente da taxa de quadros.

### QA
- validação premium agora exige material de estrada, camada de polimento e pelo menos 12 luminárias;
- mantém validação de assets CC0, AnimationPlayer, áudio, fallback, export, instalação e execução pós-instalação.

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
