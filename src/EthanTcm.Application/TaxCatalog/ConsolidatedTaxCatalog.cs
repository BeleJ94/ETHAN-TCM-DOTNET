using EthanTcm.Application.Seed;

namespace EthanTcm.Application.TaxCatalog;

public static class ConsolidatedTaxCatalog
{
    public const string Version = "2026.03.02";
    public const string OperationalSource = "Tax - Matrix_202603.02.xlsx";
    public const string LegalSource = "Liste Taxes Minieres_Updated.xlsx";

    private static readonly IReadOnlyDictionary<int, (string Code, string SourceRow)> OperationalMappings =
        new Dictionary<int, (string, string)>
        {
            [1] = ("CIT-RETURN", "5"), [2] = ("CIT-INSTALLMENT", "8"),
            [3] = ("PAYROLL-TAXES", "11"), [4] = ("VAT-RETURN", "14"),
            [5] = ("WHT-FOREIGN-SERVICES", "17"), [6] = ("VAT-WHT", "20"),
            [7] = ("WHT-INVESTMENT-INCOME", "26"), [8] = ("SUPER-PROFIT", "29"),
            [9] = ("ARSP-QST", "32"), [10] = ("RSC-EXCHANGE-CONTROL", "35"),
            [11] = ("MINING-ROYALTY", "38"), [12] = ("CONCENTRATE-TAX", "41"),
            [13] = ("ROAD-TAX", "44"), [14] = ("ENV-TAPO", "47"),
            [15] = ("ENV-TI", "50"), [16] = ("ENV-TRA", "53"),
            [17] = ("EXPLOSIVE-DEPOT-APPROVAL", "56"), [18] = ("TEMP-BLASTING-PERMIT", "59"),
            [19] = ("BLASTING-ATTENDANCE-FEE", "62"), [20] = ("BLASTER-APPROVAL", "65"),
            [21] = ("IMPORT-EXPORT-NUMBER", "68"), [22] = ("ANNUAL-SURFACE-RIGHTS", "71"),
            [23] = ("TELECOM-CHARGES", "74"), [24] = ("VEHICLE-TECHNICAL-INSPECTION", "77-78"),
            [25] = ("VEHICLE-TAXES", "80"), [26] = ("VEHICLE-PARKING-TAX", "83"),
            [27] = ("AIRCRAFT-TICKET-TAX", "86"), [28] = ("AIRCRAFT-INSPECTION-TAX", "89"),
            [29] = ("POLICE-FEE", "92"), [30] = ("VISA", "95"),
            [31] = ("WORK-PERMIT", "98"), [32] = ("RESIDENT-CARD", "101"),
            [33] = ("NATURAL-WATER-ROYALTY", "104"), [34] = ("LAND-PROPERTY-TAX", "107"),
            [35] = ("MINING-CONCESSION-SURFACE-TAX", "110"), [36] = ("RENTAL-INCOME-TAX", "113")
        };

    private static readonly Lazy<IReadOnlyCollection<TaxCatalogItem>> ItemsLazy = new(BuildItems);
    public static IReadOnlyCollection<TaxCatalogItem> Items => ItemsLazy.Value;

    private const string CommonApprover1 = "olufimpu@katangamining.com";
    private const string CommonApprover2 = "yves.ilunga@glencore.com;Godefroy.Selemani@kamotocopper.com";
    private const string CommonApprover3 = "jean-paul.kalo@glencore.com;daniel.dede@glencore.com";
    private const string CommonPayment = "etshileshe@katangamining.com";
    private const string CommonFollowUp = "vmkafunda@katangamining.com;rbapolisi@katangamining.com";

    public static IReadOnlyDictionary<string, TaxOperationalAssignments> OperationalAssignments { get; } =
        new Dictionary<string, TaxOperationalAssignments>(StringComparer.OrdinalIgnoreCase)
        {
            ["CIT-RETURN"] = A("olufimpu@katangamining.com;pitchou.kassanda@katangamining.com", "Godefroy.Selemani@kamotocopper.com", "yves.ilunga@glencore.com", null, "sntambwe@katangamining.com"),
            ["CIT-INSTALLMENT"] = A("olufimpu@katangamining.com;pitchou.kassanda@katangamining.com", "Godefroy.Selemani@kamotocopper.com", null, null, "sntambwe@katangamining.com"),
            ["PAYROLL-TAXES"] = A("mtutonda@katangamining.com;pomponmponyo@katangamining.com;kkeisler@katangamining.com", null, null, null, "mtutonda@katangamining.com"),
            ["VAT-RETURN"] = A("mtutonda@katangamining.com", null, null, null, "mtutonda@katangamining.com"),
            ["WHT-FOREIGN-SERVICES"] = A("sntambwe@katangamining.com", null, null, null, "sntambwe@katangamining.com"),
            ["VAT-WHT"] = A("mtutonda@katangamining.com;calonzo@katangamining.com", null, null, null, "sntambwe@katangamining.com"),
            ["TP-LIGHT-RETURN"] = A("olufimpu@katangamining.com;pitchou.kassanda@katangamining.com", "Godefroy.Selemani@kamotocopper.com", "yves.ilunga@glencore.com", null, "aelonga@katangamining.com", "N/A"),
            ["WHT-INVESTMENT-INCOME"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com", null, null, null, "mtutonda@katangamining.com"),
            ["SUPER-PROFIT"] = A("olufimpu@katangamining.com;osteen.wasso@kamotocopper.com", "Godefroy.Selemani@kamotocopper.com", "yves.ilunga@glencore.com", null, "sntambwe@katangamining.com"),
            ["ARSP-QST"] = A("aelonga@katangamining.com", null, null, null, "stephanie.mayala@glencore.com"),
            ["RSC-EXCHANGE-CONTROL"] = A("aelonga@katangamining.com;Ruzane.Holtzhausen@glencore.co.za", null, null, null, "jules.ndibazokize@glencore.com;aelonga@kantangamining.com"),
            ["MINING-ROYALTY"] = A("lmujing@katangamining.com;pkitungwa@katangamining.com", "osteen.wasso@kamotocopper.com", "douglas.ross@kamotocopper.com", "yves.ilunga@glencore.com", "Customs dealer"),
            ["CONCENTRATE-TAX"] = A("nditend@katangamining.com", "douglas.ross@kamotocopper.com;osteen.wasso@kamotocopper.com", "Godefroy.Selemani@kamotocopper.com", "yves.ilunga@glencore.com", "Customs dealer"),
            ["ROAD-TAX"] = A("nditend@katangamining.com", "douglas.ross@kamotocopper.com;osteen.wasso@kamotocopper.com", "Godefroy.Selemani@kamotocopper.com", "yves.ilunga@glencore.com", "Customs dealer"),
            ["ENV-TAPO"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com", null, null, null, "ange.moanda@glencore.com;sntambwe@katangamining.com"),
            ["ENV-TI"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com", null, null, null, "ange.moanda@glencore.com;sntambwe@katangamining.com"),
            ["ENV-TRA"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com", null, null, null, "ange.moanda@glencore.com;sntambwe@katangamining.com"),
            ["EXPLOSIVE-DEPOT-APPROVAL"] = LegalA("clenga@katangamining.com"),
            ["TEMP-BLASTING-PERMIT"] = LegalA("clenga@katangamining.com"),
            ["BLASTING-ATTENDANCE-FEE"] = LegalA("clenga@katangamining.com"),
            ["BLASTER-APPROVAL"] = LegalA("clenga@katangamining.com"),
            ["IMPORT-EXPORT-NUMBER"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;sylvie.mbumb@glencore.com", null, null, null, "sntambwe@katangamining.com"),
            ["ANNUAL-SURFACE-RIGHTS"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;sylvie.mbumb@glencore.com", null, "yves.ilunga@glencore.com;dmavungu@katangamining.com;Godefroy.Selemani@kamotocopper.com", null, "sntambwe@katangamining.com"),
            ["TELECOM-CHARGES"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;alain.mavungu@katangamining.com", null, null, null, "calonzo@katangamining.com"),
            ["VEHICLE-TECHNICAL-INSPECTION"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;pkanyinda@katangamining.com", null, null, null, "calonzo@katangamining.com"),
            ["VEHICLE-TAXES"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;pkanyinda@katangamining.com", null, null, null, "calonzo@katangamining.com"),
            ["VEHICLE-PARKING-TAX"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;pkanyinda@katangamining.com", null, null, null, "calonzo@katangamining.com"),
            ["AIRCRAFT-TICKET-TAX"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;fsanki@katangamining.com", null, null, null, "calonzo@katangamining.com"),
            ["AIRCRAFT-INSPECTION-TAX"] = SpecialA("mtutonda@katangamining.com;sntambwe@katangamining.com;fsanki@katangamining.com"),
            ["POLICE-FEE"] = SpecialA("mtutonda@katangamining.com;sntambwe@katangamining.com;clenga@katangamining.com"),
            ["VISA"] = A("pmukwasa@katangamining.com", "rbitumba1@katangamining.com", "yves.ilunga@glencore.com", null, "pmukwasa@katangamining.com"),
            ["WORK-PERMIT"] = A("pmukwasa@katangamining.com", "rbitumba1@katangamining.com", "yves.ilunga@glencore.com", null, "pmukwasa@katangamining.com"),
            ["RESIDENT-CARD"] = A("pmukwasa@katangamining.com", "rbitumba1@katangamining.com", "yves.ilunga@glencore.com", null, "pmukwasa@katangamining.com"),
            ["NATURAL-WATER-ROYALTY"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;lnamwimba@katangamining.com", null, null, null, "calonzo@katangamining.com"),
            ["LAND-PROPERTY-TAX"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;clenga@katangamining.com", null, null, null, "calonzo@katangamining.com"),
            ["MINING-CONCESSION-SURFACE-TAX"] = A("mtutonda@katangamining.com;sntambwe@katangamining.com;clenga@katangamining.com", null, null, null, "calonzo@katangamining.com"),
            ["RENTAL-INCOME-TAX"] = SpecialA("mtutonda@katangamining.com;sntambwe@katangamining.com;clenga@katangamining.com")
        };

    public static IReadOnlyCollection<TaxCatalogConflictDefinition> KnownConflicts { get; } =
    [
        Conflict("CIT-INSTALLMENT", "Frequency", "Four (4) instalments", "Three instalments per year", "4"),
        Conflict("WHT-INVESTMENT-INCOME", "RateOrTaxableBasis", "10% Mining Code rate", "20% under FY2026 source", "7"),
        Conflict("ENV-TI", "Frequency", "Annual", "Punctual", "26"),
        Conflict("EXPLOSIVE-DEPOT-APPROVAL", "Frequency", "Ponctual", "Annual", "37"),
        Conflict("RENTAL-INCOME-TAX", "Frequency", "Annual", "Monthly", "45"),
        Conflict("RENTAL-INCOME-TAX", "Frequency", "Annual", "Monthly/Annual duplicate", "56"),
        Conflict("RSC-EXCHANGE-CONTROL", "Frequency", "Monthly", "Punctual", "48")
    ];

    public static IReadOnlyCollection<TaxCatalogReconciliation> Reconciliation { get; } =
    [
        R("3","CIT-RETURN","ENRICH_EXISTING"), R("4","CIT-INSTALLMENT","ENRICH_EXISTING_WITH_CONFLICT"),
        R("5","WHT-FOREIGN-SERVICES","ENRICH_EXISTING"), R("6","WHT-INVESTMENT-INCOME","ENRICH_EXISTING"),
        R("7","WHT-INVESTMENT-INCOME","ENRICH_EXISTING_WITH_RATE_CONFLICT"), R("8","PAYROLL-IPR","CREATE_FROM_PAYROLL_PARENT"),
        R("9","SUPER-PROFIT","ENRICH_EXISTING"), R("10","WHT-CAPITAL-GAIN","NEW"),
        R("11","PAYROLL-IERE","CREATE_FROM_PAYROLL_PARENT"), R("12","VAT-RETURN","ENRICH_EXISTING"),
        R("13","VAT-WHT","ENRICH_EXISTING"), R("14","PAYROLL-CNSS","CREATE_FROM_PAYROLL_PARENT"),
        R("15","PAYROLL-INPP","CREATE_FROM_PAYROLL_PARENT"), R("16","PAYROLL-ONEM","CREATE_FROM_PAYROLL_PARENT"),
        R("17","IMPORT-DUTIES","NEW"), R("18","EXPORT-FEE","NEW"),
        R("19","FUEL-LUBRICANT-ROYALTY","NEW"), R("20","CUSTOMS-REGIME","REVIEW_INCOMPLETE"),
        R("21","MINING-ROYALTY","ENRICH_EXISTING"), R("22","MORTGAGE-REGISTRATION-FEE","NEW"),
        R("23","TRANSFER-REGISTRATION-FEE","NEW"), R("24","FARMOUT-OPTION-FEE","NEW"),
        R("25","SIGNATURE-BONUS","NEW_REVIEW_MISSING_DEADLINE"), R("26","ENV-TI","ENRICH_EXISTING_WITH_FREQUENCY_CONFLICT"),
        R("27","ENV-TRA","ENRICH_EXISTING"), R("28","ENV-TAPO","ENRICH_EXISTING"),
        R("29","IMPORT-EXPORT-NUMBER","ENRICH_EXISTING"), R("30","MINING-ROYALTY","MERGE_DUPLICATE"),
        R("36","WORK-PERMIT","ENRICH_EXISTING"), R("37","EXPLOSIVE-DEPOT-APPROVAL","ENRICH_EXISTING_WITH_FREQUENCY_CONFLICT"),
        R("38","DREDGER-REGISTRATION-FEE","NEW_REVIEW_MISSING_DEADLINE"), R("39","PROCESSING-ENTITY-ANNUAL-FEE","NEW_REVIEW_MISSING_RATE_AND_DEADLINE"),
        R("40","ANNUAL-SURFACE-RIGHTS","ENRICH_EXISTING"), R("41","VEHICLE-TAX","SPLIT_OR_LINK_TO_VEHICLE-TAXES"),
        R("42","LAND-PROPERTY-TAX","ENRICH_EXISTING"), R("43","SPECIAL-ROAD-TRAFFIC-TAX","SPLIT_OR_LINK_TO_VEHICLE-TAXES"),
        R("44","MINING-CONCESSION-SURFACE-TAX","ENRICH_EXISTING"), R("45","RENTAL-INCOME-TAX","ENRICH_EXISTING_WITH_FREQUENCY_CONFLICT"),
        R("46","ROAD-TAX;CONCENTRATE-TAX","SPLIT_SOURCE_ROW_BETWEEN_TWO_EXISTING"), R("47","MINING-CONCESSION-SURFACE-TAX","MERGE_DUPLICATE"),
        R("48","RSC-EXCHANGE-CONTROL","ENRICH_EXISTING_WITH_FREQUENCY_CONFLICT"), R("49","ROAD-TAX;CONCENTRATE-TAX","MERGE_DUPLICATE_AND_SPLIT"),
        R("50","OTHER-TAXES-AGGREGATE","REVIEW_DO_NOT_CREATE_ACTIVE_TAX"), R("51","DEFORESTATION-TAX","NEW"),
        R("52","TELECOM-CHARGES","ENRICH_EXISTING_REVIEW_MISSING_FIELDS"), R("53","BLASTER-APPROVAL","ENRICH_EXISTING_REVIEW_MISSING_FIELDS"),
        R("54","LAND-PROPERTY-TAX","MERGE_DUPLICATE"), R("55","VEHICLE-TAX","MERGE_DUPLICATE"),
        R("56","RENTAL-INCOME-TAX","MERGE_DUPLICATE_WITH_FREQUENCY_CONFLICT"), R("57","SPECIAL-ROAD-TRAFFIC-TAX","MERGE_DUPLICATE"),
        R("58","MINING-CONCESSION-SURFACE-TAX","MERGE_DUPLICATE"), R("59","EXPORT-PROCEEDS-REPATRIATION","NEW"),
        R("60","RSC-EXCHANGE-CONTROL","MERGE_DUPLICATE"), R("31-35","MINING-ROYALTY","CREATE_ALLOCATION_RULES_NOT_TAXES")
    ];

    public static IReadOnlyCollection<TaxAllocationDefinition> MiningRoyaltyAllocations { get; } =
    [
        new(31, 44m, "Central Power"), new(32, 23m, "Province"),
        new(33, 14m, "Administrative Decentralized Entities (EAD)"),
        new(34, 11m, "FONAREV"), new(35, 8m, "Mining Fund for Future Generations")
    ];

    private static IReadOnlyCollection<TaxCatalogItem> BuildItems()
    {
        var items = InitialTaxObligationSeedData.Items.Select(item =>
        {
            var mapping = item.Number.HasValue
                ? OperationalMappings[item.Number.Value]
                : ("TP-LIGHT-RETURN", "23");
            return new TaxCatalogItem(
                mapping.Item1, mapping.Item2, item.Number?.ToString(), item.Department,
                item.ReportType, item.TaxCategory, item.Frequency, item.LegalDeadline,
                null, null, null, null, false, item.RequiresReview, false);
        }).ToList();

        AddChildren(items);
        AddNew(items);
        ApplyLegalData(items);
        return items;
    }

    private static void AddChildren(List<TaxCatalogItem> items)
    {
        var payroll = items.Single(x => x.CanonicalCode == "PAYROLL-TAXES");
        items.AddRange(new[]
        {
            Child(payroll, "PAYROLL-IPR", "Payroll Tax - Personal Income Tax", "8"),
            Child(payroll, "PAYROLL-IERE", "IERE/DPE", "11"),
            Child(payroll, "PAYROLL-CNSS", "Payroll/CNSS", "14"),
            Child(payroll, "PAYROLL-INPP", "Payroll/INPP", "15"),
            Child(payroll, "PAYROLL-ONEM", "Payroll/ONEM", "16"),
            Child(items.Single(x => x.CanonicalCode == "VEHICLE-TAXES"), "VEHICLE-TAX", "Vehicle tax", "41"),
            Child(items.Single(x => x.CanonicalCode == "VEHICLE-TAXES"), "SPECIAL-ROAD-TRAFFIC-TAX", "Special road traffic tax", "43")
        });
    }

    private static TaxCatalogItem Child(TaxCatalogItem parent, string code, string name, string legalRow) =>
        parent with
        {
            CanonicalCode = code, SourceRow = $"derived:{parent.SourceRow}",
            ExternalNumber = null, Name = name, Category = name,
            LegalSourceRow = legalRow, IsNew = true, RequiresReview = false
        };

    private static void AddNew(List<TaxCatalogItem> items)
    {
        items.AddRange(new[]
        {
            New("WHT-CAPITAL-GAIN", "Withholding tax on capital gain", "10", "Punctual", "No later than the 15th of the month following payment", "DGI"),
            New("IMPORT-DUTIES", "Import duties", "17", "Punctual", "At each importation", "DGDA"),
            New("EXPORT-FEE", "Export fee", "18", "Punctual", "At each exportation", "DGDA"),
            New("FUEL-LUBRICANT-ROYALTY", "Ground fuel and lubricants royalty", "19", "Punctual", "Within 8 days after receipt of the perception note", "DGDA"),
            New("MORTGAGE-REGISTRATION-FEE", "Proportional fee for approval and registration of mortgages", "22", "Punctual", "Within 8 days after receipt of the perception note", "DGRAD"),
            New("TRANSFER-REGISTRATION-FEE", "Proportional fee for approval and registration of transfers", "23", "Punctual", "Within 8 days after receipt of the perception note", "DGRAD"),
            New("FARMOUT-OPTION-FEE", "Proportional fee for approval and registration of farm-out, option and transfer contract", "24", "Punctual", "Within 8 days after receipt of the perception note", "DGRAD"),
            New("SIGNATURE-BONUS", "Signature bonus", "25", "Punctual", null, "DGRAD", true),
            New("DREDGER-REGISTRATION-FEE", "Dredger registration fee", "38", "Punctual", null, "DGRAD", true),
            New("PROCESSING-ENTITY-ANNUAL-FEE", "Annual fee and security for processing entities", "39", "Annual", null, "DGRAD", true),
            New("DEFORESTATION-TAX", "Deforestation Tax", "51", "Punctual", "Before commencing land-clearing work", "DGRAD/FFN"),
            New("EXPORT-PROCEEDS-REPATRIATION", "Repatriation of Export income", "59", "Punctual", "Within 15 days from payment of the principal amount", "Central Bank")
        });
    }

    private static TaxCatalogItem New(string code, string name, string row, string frequency, string? deadline, string authority, bool review = false) =>
        new(code, $"legal:{row}", null, "Finance", name, name, frequency, deadline,
            row, authority, null, null, true, review || deadline is null, true);

    private static void ApplyLegalData(List<TaxCatalogItem> items)
    {
        var legal = LegalRows.ToDictionary(x => x.CanonicalCode);
        for (var i = 0; i < items.Count; i++)
        {
            if (!legal.TryGetValue(items[i].CanonicalCode, out var row))
                continue;
            items[i] = items[i] with
            {
                LegalSourceRow = row.SourceRow, Authority = row.Authority,
                RateOrTaxableBasis = row.Rate, LegalReference = row.Law,
                PenaltyOrComments = row.Comments, BusinessCycle = row.Cycle,
                Process = row.Process
            };
        }
    }

    // The consolidated legal source is represented as versioned C# objects. Duplicate
    // source rows are retained through aliases/source references by the synchronizer.
    public static IReadOnlyCollection<TaxLegalData> LegalRows { get; } =
    [
        L("CIT-RETURN","3","30% of Income or 1/100th of Turnover in case of loss","DGI","Art 247 of the Mining Code","Late filing 25%; late payment 2% per month; financial statements required.","Turnover/sales of mining products"),
        L("CIT-INSTALLMENT","4","30%, 30% and 20% provisional instalments","DGI","Art 60 of the Finance Law 2025","Failure to pay and file leads to a 50% penalty.",null),
        L("WHT-FOREIGN-SERVICES","5","14% WHT on non-resident services","DGI","Art 246 bis Mining Code; Law no. 23/053","Late filing/payment penalties.", "Service provider/operational expenses"),
        L("WHT-INVESTMENT-INCOME","6","10% dividends; 14% equipment rental on 70% taxable basis","DGI","Art 245 Mining Code","Late filing/payment penalties.","Expenses"),
        L("PAYROLL-IPR","8","30% maximum; progressive scale","DGI","Articles 118 and 123 of Law no. 23/053","Progressive calculation and late penalties.","Operational expenses"),
        L("SUPER-PROFIT","9","50% of gross operating surplus","DGI","Art 530 Mining Regulation and Art 251 bis Mining Code","Late filing/payment penalties.","Turnover/sales of mining products"),
        L("WHT-CAPITAL-GAIN","10","30% of capital gain","DGI","Art 253 bis Mining Code","Late filing/payment penalties.","Deductible expense"),
        L("PAYROLL-IERE","11","12.5% or 25% of gross personal income","DGI","Art 244 bis Mining Code; Articles 145 to 149 of Law no. 23/053","12.5% first ten years, 25% afterward.","Operational expenses"),
        L("VAT-RETURN","12","16%; 0% export","DGI","Art 35 VAT Law","Late filing/payment penalties.",null),
        L("VAT-WHT","13","16%","DGI","Art 16 Finance Law 2018","Fine may equal non-withheld amount.",null),
        L("PAYROLL-CNSS","14","5% employee and 13% employer; gross salary basis","CNSS","Article 3 of Decree no. 18/041","0.50% per late day.",null),
        L("PAYROLL-INPP","15","3.5% / 3% / 2% by workforce","INPP",null,"Late filing/payment penalties.",null),
        L("PAYROLL-ONEM","16","0.2% of gross personal income","ONEM","Art 1 of Ministerial Order no. 095/2018","50% filing penalty and 0.5% per late day.",null),
        L("IMPORT-DUTIES","17","2%, 5% and 10% preferential rates","DGDA","Art 232 bis Mining Code","Rates depend on project stage.",null),
        L("EXPORT-FEE","18","1% of gross commercial value","DGDA","Art 234 Mining Code","Export service remuneration.",null),
        L("FUEL-LUBRICANT-ROYALTY","19","5% on fuel allocated to mining activity","DGDA","Art 232 Mining Code; Art 36 Ordinance-Law 2013","Late penalties.",null),
        L("MINING-ROYALTY","21","3.5% precious metals; 10% strategic Cobalt","DGRAD","Arts 240 and 241 Mining Code","Due on exit of merchantable product.",null),
        L("MORTGAGE-REGISTRATION-FEE","22","0.5%, 0.3%, 0.2%, 0.1% progressive bands","DGRAD","Art 171 Mining Code; Art 364 Mining Regulation","Late penalties.",null),
        L("TRANSFER-REGISTRATION-FEE","23","1% of immediate sale price","DGRAD","Art 185 ter Mining Code; Art 380 Mining Regulation","Record within 5 days of approval.",null),
        L("FARMOUT-OPTION-FEE","24","1%","DGRAD","Arts 187 and 193 Mining Code; Arts 381 and 384 Mining Regulation","Late penalties.",null),
        L("SIGNATURE-BONUS","25","10% of selected offer","DGRAD","Annex to Interministerial Order no. 0349 of 2014",null,null),
        L("ENV-TI","26","Depends on installed capacity","DGRAD","Art 39 Environmental Law 2011","Paid once during implementation/modification.",null),
        L("ENV-TRA","27","Depends on installed capacity","DGRAD","Art 39 Environmental Law 2011","Payment no later than 15 June.",null),
        L("ENV-TAPO","28","Flat rate by polluting unit","DGRAD","Art 39 Environmental Law 2011",null,null),
        L("IMPORT-EXPORT-NUMBER","29","USD 500","DGRAD/External trade","Art 1 Interministerial Order 2019",null,null),
        L("WORK-PERMIT","36","USD 2,880 plus submission fees","DGRAD/DGM and Labour Ministry","Art 3 Interministerial Order 2004","Additional visa/card/form fees.",null),
        L("EXPLOSIVE-DEPOT-APPROVAL","37","417,000 mines; 141,510 quarries","DGRAD",null,null,null),
        L("DREDGER-REGISTRATION-FEE","38","9,434,000 large; 3,301,900 medium","DGRAD",null,null,null),
        L("PROCESSING-ENTITY-ANNUAL-FEE","39",null,"DGRAD",null,null,null),
        L("ANNUAL-SURFACE-RIGHTS","40","Rate per day and quadrangle","CAMI","Art 199 Mining Code; Art 400 Mining Regulation","Late penalties.",null),
        L("VEHICLE-TAX","41","Depends on engine horsepower","DIL","Art 237 Mining Code","Provincial collection.",null),
        L("LAND-PROPERTY-TAX","42","Depends on location and building type","DIL","Art 41 Tax Code",null,null),
        L("SPECIAL-ROAD-TRAFFIC-TAX","43","Depends on engine power","DIL","Art 239 Mining Code",null,null),
        L("MINING-CONCESSION-SURFACE-TAX","44","Depends on mining area","DIL","Art 238 Mining Code",null,null),
        L("RENTAL-INCOME-TAX","45","20% of rent","DIL",null,null,null),
        L("ROAD-TAX","46","USD 100 per metric ton","DRNOFLU",null,null,null),
        L("CONCENTRATE-TAX","46","USD 100 per metric ton","DRNOFLU",null,null,null),
        L("RSC-EXCHANGE-CONTROL","48","2/1000 inbound/outbound transfers","BCC","Art 270 Mining Code","BCC",null,"File review; approval; declaration; receipt; evidence."),
        L("DEFORESTATION-TAX","51","USD 1,800 or USD 1,200 per hectare","DGRAD/FFN",null,null,null),
        L("TELECOM-CHARGES","52",null,null,null,null,null),
        L("BLASTER-APPROVAL","53",null,null,null,null,null),
        L("EXPORT-PROCEEDS-REPATRIATION","59","60% of export income","Central Bank","Art 269 Mining Code",null,"Linked to sales/income")
    ];

    private static TaxLegalData L(string code, string row, string? rate, string? authority, string? law, string? comments, string? cycle, string? process = null) =>
        new(code, row, rate, authority, law, comments, cycle, process);
    private static TaxCatalogConflictDefinition Conflict(string code, string field, string existing, string incoming, string row) =>
        new(code, field, existing, incoming, LegalSource, row);
    private static TaxCatalogReconciliation R(string row, string codes, string action) => new(row, codes, action);

    private static TaxOperationalAssignments A(
        string preparers, string? approver1 = null, string? approver2 = null,
        string? approver3 = null, string? submission = null, string? payment = null) =>
        new(Split(preparers), Split(approver1 ?? CommonApprover1),
            Split(approver2 ?? CommonApprover2), Split(approver3 ?? CommonApprover3),
            Split(payment ?? CommonPayment), Split(submission), Split(CommonFollowUp));

    private static TaxOperationalAssignments LegalA(string preparers) =>
        A(preparers, "fbunout@katangamining.com", "dmavungu@katangamining.com",
            "yves.ilunga@glencore.com", "KCC & Orica");

    private static TaxOperationalAssignments SpecialA(string preparers) =>
        A(preparers, CommonApprover1,
            "nkabale@katangamining.com;dmavungu@katangamining.com;etienne.coetzee@katangamining.com;chantal.wessels@glencore.co.za",
            CommonApprover3, "jules.ndibazokize@glencore.com");

    private static IReadOnlyCollection<string> Split(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            ? []
            : value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

public sealed record TaxCatalogItem(
    string CanonicalCode, string SourceRow, string? ExternalNumber, string Department,
    string Name, string Category, string? Frequency, string? RawDeadlineText,
    string? LegalSourceRow, string? Authority, string? RateOrTaxableBasis,
    string? LegalReference, bool IsNew, bool RequiresReview, bool ForceInactive,
    string? PenaltyOrComments = null, string? BusinessCycle = null, string? Process = null);

public sealed record TaxLegalData(
    string CanonicalCode, string SourceRow, string? Rate, string? Authority,
    string? Law, string? Comments, string? Cycle, string? Process);
public sealed record TaxCatalogConflictDefinition(
    string CanonicalCode, string FieldName, string? ExistingValue, string? IncomingValue,
    string SourceName, string SourceRow);
public sealed record TaxAllocationDefinition(int SourceRow, decimal Percentage, string Beneficiary);
public sealed record TaxCatalogReconciliation(string SourceRow, string CanonicalCodes, string Action)
{
    public bool AppliesTo(string canonicalCode) =>
        CanonicalCodes.Split(';', StringSplitOptions.TrimEntries).Contains(canonicalCode, StringComparer.OrdinalIgnoreCase);
}
public sealed record TaxOperationalAssignments(
    IReadOnlyCollection<string> Preparers,
    IReadOnlyCollection<string> Approver1,
    IReadOnlyCollection<string> Approver2,
    IReadOnlyCollection<string> Approver3,
    IReadOnlyCollection<string> PaymentProcessOwners,
    IReadOnlyCollection<string> SubmissionProcessOwners,
    IReadOnlyCollection<string> FollowUpOwners);
