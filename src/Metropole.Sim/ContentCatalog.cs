namespace Metropole.Sim;

public static class ContentCatalog
{
    public static readonly IReadOnlyList<SectorDefinition> Sectors =
    [
        new("Alimentação", "Alimentos", 18m, 1.10m),
        new("Varejo", "Bens de consumo", 35m, 1.00m),
        new("Construção", "Materiais", 55m, 0.95m),
        new("Logística", "Transporte", 42m, 1.05m),
        new("Tecnologia", "Serviços digitais", 85m, 1.35m),
        new("Saúde", "Cuidados", 70m, 1.15m),
        new("Educação", "Formação", 48m, 1.05m),
        new("Finanças", "Serviços financeiros", 90m, 1.25m),
        new("Indústria", "Manufaturados", 64m, 1.20m),
        new("Agricultura", "Insumos agrícolas", 28m, 1.00m),
        new("Energia", "Energia", 75m, 1.30m),
        new("Imobiliário", "Habitação", 120m, 0.90m),
        new("Hotelaria", "Hospedagem", 62m, 1.00m),
        new("Restaurantes", "Refeições", 32m, 1.10m),
        new("Entretenimento", "Lazer", 38m, 1.00m),
        new("Comunicação", "Mídia", 44m, 1.10m),
        new("Serviços", "Serviços gerais", 30m, 1.00m),
        new("Segurança", "Proteção", 52m, 1.00m),
        new("Limpeza", "Higiene", 24m, 0.95m),
        new("Automotivo", "Mobilidade", 68m, 1.05m),
        new("Têxtil", "Vestuário", 41m, 1.00m),
        new("Química", "Químicos", 78m, 1.15m),
        new("Metalurgia", "Metais", 82m, 1.20m),
        new("Madeira", "Madeira processada", 50m, 1.00m),
        new("Móveis", "Mobiliário", 72m, 1.05m),
        new("Papel", "Papel e impressão", 37m, 1.00m),
        new("Reciclagem", "Reciclados", 26m, 0.95m),
        new("Telecom", "Conectividade", 58m, 1.20m),
        new("Pesquisa", "Conhecimento", 95m, 1.30m),
        new("Jurídico", "Serviços jurídicos", 88m, 1.15m),
        new("Cultura", "Cultura", 29m, 0.95m),
        new("Esportes", "Esporte", 34m, 1.00m)
    ];

    private static readonly string[] Occupations =
    [
        "Administrador", "Analista", "Atendente", "Auditor", "Cientista", "Comprador", "Consultor", "Contador",
        "Coordenador", "Desenvolvedor", "Eletricista", "Engenheiro", "Estoquista", "Gerente", "Inspetor", "Instalador",
        "Mecânico", "Motorista", "Operador", "Pesquisador", "Planejador", "Professor", "Projetista", "Recepcionista",
        "Representante", "Supervisor", "Técnico", "Vendedor", "Designer", "Programador", "Nutricionista", "Enfermeiro",
        "Logístico", "Marceneiro", "Químico", "Metalúrgico", "Impressor", "Corretor", "Advogado", "Produtor"
    ];

    private static readonly string[] Specializations =
    [
        "Operações", "Qualidade", "Planejamento", "Comercial", "Processos",
        "Manutenção", "Dados", "Campo", "Produção", "Projetos"
    ];

    private static readonly string[] BusinessFormats =
    [
        "Microempresa", "Loja de bairro", "Operação móvel", "Oficina", "Escritório",
        "Centro técnico", "Distribuidora", "Fábrica compacta", "Fábrica", "Atacadista",
        "Franquia local", "Cooperativa", "Marketplace local", "Prestadora B2B", "Prestadora B2C",
        "Centro de serviços", "Operação premium", "Operação econômica", "Rede regional", "Unidade especializada"
    ];

    private static readonly string[] ProductRoots =
    [
        "Kit", "Pacote", "Linha", "Peça", "Módulo", "Componente", "Serviço", "Plano", "Unidade", "Conjunto",
        "Solução", "Carga", "Lote", "Produto", "Equipamento", "Recurso", "Suprimento", "Material", "Item", "Sistema",
        "Acessório", "Estrutura", "Ferramenta", "Consumível", "Entrega"
    ];

    private static readonly string[] Materials =
    [
        "Básico", "Madeira", "Aço", "Alumínio", "Papel", "Fibra", "Vidro", "Polímero", "Têxtil", "Cerâmica"
    ];

    private static readonly string[] Grades =
    [
        "Econômico", "Padrão", "Reforçado", "Profissional", "Premium",
        "Compacto", "Industrial", "Sustentável", "Rápido", "Especial"
    ];

    private static readonly string[] ResourceMaterials =
    [
        "Madeira", "Aço", "Alumínio", "Cobre", "Vidro", "Areia", "Argila", "Papel", "Celulose", "Água",
        "Fibra", "Borracha", "Polímero", "Tecido", "Couro", "Grão", "Óleo", "Sal", "Carvão", "Silício",
        "Fertilizante", "Tinta", "Resina", "Gás", "Minério"
    ];

    private static readonly string[] ResourceForms =
    [
        "Bruto", "Selecionado", "Processado", "Laminado", "Granulado",
        "Refinado", "Reciclado", "Compactado", "Tratado", "Industrial",
        "Seco", "Líquido", "Misturado", "Cortado", "Moldado",
        "Prensado", "Purificado", "Reforçado", "Padronizado", "Premium"
    ];

    private static readonly string[] BuildingUses =
    [
        "Casa", "Apartamento", "Loja", "Escritório", "Galpão", "Fábrica", "Fazenda", "Mercado", "Oficina", "Escola",
        "Clínica", "Hospital", "Hotel", "Restaurante", "Armazém", "Centro logístico", "Laboratório", "Estúdio", "Terminal", "Garagem",
        "Centro comercial", "Torre mista", "Depósito", "Centro esportivo", "Centro cultural", "Data center", "Usina", "Serraria", "Gráfica", "Cooperativa"
    ];

    private static readonly string[] BuildingClasses =
    [
        "Compacto", "Básico", "Popular", "Padrão", "Conforto", "Amplo", "Premium", "Corporativo", "Industrial", "Alta eficiência"
    ];

    private static readonly string[] SkillDomains =
    [
        "Administração", "Comunicação", "Finanças", "Tecnologia", "Engenharia", "Logística", "Comercial", "Produção", "Qualidade", "Liderança",
        "Dados", "Design", "Manutenção", "Segurança", "Saúde", "Educação", "Pesquisa", "Construção", "Operações", "Negociação"
    ];

    private static readonly string[] SkillCompetencies =
    [
        "Fundamentos", "Execução", "Planejamento", "Diagnóstico", "Otimização",
        "Coordenação", "Análise", "Automação", "Estratégia", "Especialização"
    ];

    private static readonly string[] EventCauses =
    [
        "Escassez", "Excesso de oferta", "Aumento de demanda", "Queda de demanda", "Gargalo logístico",
        "Expansão de capacidade", "Falha operacional", "Mudança salarial", "Pressão de custos", "Entrada de concorrente",
        "Saída de concorrente", "Crédito restrito", "Crédito abundante", "Valorização imobiliária", "Desvalorização imobiliária",
        "Crescimento populacional", "Envelhecimento populacional", "Mudança tecnológica", "Aumento de produtividade", "Queda de produtividade"
    ];

    private static readonly string[] EventIntensities =
    [
        "leve", "moderado", "relevante", "forte", "severo",
        "curto", "persistente", "localizado", "amplo", "estrutural"
    ];

    private static readonly string[] EventScopes = ["empresa", "bairro", "setor", "cidade", "cadeia produtiva"];

    private static readonly string[] TechnologyAreas =
    [
        "Automação", "Logística", "Energia", "Produção", "Qualidade", "Dados", "Vendas", "Atendimento", "Finanças", "Segurança",
        "Manutenção", "Materiais", "Construção", "Agricultura", "Saúde", "Educação", "Comunicação", "Mobilidade", "Reciclagem", "Armazenagem",
        "Robótica", "Sensoriamento", "Planejamento", "Distribuição", "Gestão", "Projetos", "Design", "Pesquisa", "Eficiência", "Capacitação"
    ];

    public static IReadOnlyList<ProfessionDefinition> Professions { get; } = BuildProfessions();
    public static IReadOnlyList<BusinessArchetype> Businesses { get; } = BuildBusinesses();
    public static IReadOnlyList<ProductDefinition> Products { get; } = BuildProducts();
    public static IReadOnlyList<ResourceDefinition> Resources { get; } = BuildResources();
    public static IReadOnlyList<BuildingDefinition> Buildings { get; } = BuildBuildings();
    public static IReadOnlyList<SkillDefinition> Skills { get; } = BuildSkills();
    public static IReadOnlyList<EventDefinition> Events { get; } = BuildEvents();
    public static IReadOnlyList<TechnologyDefinition> Technologies { get; } = BuildTechnologies();

    public static ContentMetrics Metrics => new(
        Professions.Count,
        Professions.Count * 5,
        Businesses.Count,
        Products.Count,
        Resources.Count,
        Buildings.Count,
        Skills.Count,
        Events.Count,
        Technologies.Count);

    private static IReadOnlyList<ProfessionDefinition> BuildProfessions()
    {
        var list = new List<ProfessionDefinition>(Occupations.Length * Specializations.Length);
        var i = 0;
        foreach (var occupation in Occupations)
        foreach (var specialization in Specializations)
        {
            var sector = Sectors[i % Sectors.Count];
            var factor = 0.75m + ((i % 11) * 0.08m);
            list.Add(new(occupation, specialization, sector.Name, factor));
            i++;
        }
        return list;
    }

    private static IReadOnlyList<BusinessArchetype> BuildBusinesses() =>
        Sectors.SelectMany(s => BusinessFormats.Select(f => new BusinessArchetype(s.Name, f))).ToArray();

    private static IReadOnlyList<ProductDefinition> BuildProducts()
    {
        var list = new List<ProductDefinition>(ProductRoots.Length * Materials.Length * Grades.Length);
        var i = 0;
        foreach (var root in ProductRoots)
        foreach (var material in Materials)
        foreach (var grade in Grades)
        {
            var family = Sectors[i % Sectors.Count].ProductFamily;
            list.Add(new($"{root} {material} {grade}", family, material, grade));
            i++;
        }
        return list;
    }

    private static IReadOnlyList<ResourceDefinition> BuildResources() =>
        ResourceMaterials.SelectMany(m => ResourceForms.Select(f => new ResourceDefinition(m, f))).ToArray();

    private static IReadOnlyList<BuildingDefinition> BuildBuildings() =>
        BuildingUses.SelectMany(u => BuildingClasses.Select(c => new BuildingDefinition(u, c))).ToArray();

    private static IReadOnlyList<SkillDefinition> BuildSkills() =>
        SkillDomains.SelectMany(d => SkillCompetencies.Select(c => new SkillDefinition(d, c))).ToArray();

    private static IReadOnlyList<EventDefinition> BuildEvents() =>
        EventCauses.SelectMany(c => EventIntensities.SelectMany(i => EventScopes.Select(s => new EventDefinition(c, i, s)))).ToArray();

    private static IReadOnlyList<TechnologyDefinition> BuildTechnologies() =>
        TechnologyAreas.SelectMany(a => Enumerable.Range(1, 10).Select(t => new TechnologyDefinition(a, t))).ToArray();
}
