# Changelog

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
