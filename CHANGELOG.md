# Changelog

## 1.4.0 - 2026-09-21

### Assets CC0
- integração automatizada de Kenney City Kit Commercial, Industrial, Suburban e Roads;
- integração do Kenney Car Kit;
- integração do Kenney Mini Characters;
- procedência e licença CC0 registradas no build e na documentação;
- assets externos são baixados no pipeline de build, mantendo o repositório de código enxuto.

### Cidade e animação
- prédios 3D licenciados passam a complementar os MultiMeshes procedurais;
- veículos reais do pack Kenney circulam nas vias principais;
- personagens 3D reais passam a circular nos bairros;
- clips importados são reproduzidos quando diretamente compatíveis;
- locomação visível tem fallback controlado pelo Godot quando um clip externo exige retargeting;
- qualidade continua escalável para preservar PCs leves.

### Áudio
- 100 sons de interface Kenney disponíveis no pacote;
- clique e hover recebem feedback sonoro;
- chuva loopável reage ao clima;
- ambiência de multidão reage ao horário e população;
- buses separados para UI, Weather e Ambience.

### QA
- pipeline baixa e valida quantidade mínima de modelos, personagens e sons;
- validação do executável exige assets realmente presentes no PCK;
- cidade premium exige prédios, veículos e personagens instanciados;
- pelo menos um personagem visível animado é obrigatório;
- fallback 2D continua validado separadamente.

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
