# Changelog

## 1.3.0 - 2026-09-21

### Renderer adaptativo
- cinco perfis gráficos: Automático, Ultra, Alto, Equilibrado e Leve;
- controle automático de qualidade com base em FPS sustentado;
- frequência de redraw ajustável sem alterar a simulação;
- densidade escalável de pedestres, tráfego, chuva, árvores e empresas visíveis;
- efeitos visuais reduzidos progressivamente antes de qualquer perda de mecânica;
- indicador de FPS/perfil efetivo na interface;
- nova tela Gráficos com orçamento visual explícito.

### Cidade viva
- sinais de marca e estado operacional nos prédios;
- guindastes visuais para empresas em crescimento;
- veículos de entrega em negócios de maior faturamento;
- marcas visuais de estabelecimentos fechados;
- pulsos econômicos por distrito;
- mais feedback visual para empresa do jogador.

### Compatibilidade e desempenho
- simulação continua independente do FPS;
- modo Automático começa em perfil equilibrado e sobe/desce conforme folga de desempenho;
- perfil Leve reduz apenas representação visual;
- validação percorre todos os perfis gráficos em 1280×720, 1600×900 e 1920×1080;
- executável exportado continua sendo a unidade final de validação.

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

### Gráficos
- dia/noite;
- sol, lua e estrelas;
- chuva e neblina;
- iluminação noturna;
- tráfego variável por horário;
- pedestres;
- parques e cidade procedural mais detalhada;
- novos ícones Vida e Pessoas.

### Qualidade
- novos testes para relógio horário, vida, branding, finanças e resiliência econômica;
- instalador e pipeline Windows atualizados para 1.2.0.

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
