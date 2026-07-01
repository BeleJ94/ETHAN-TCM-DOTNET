namespace EthanTcm.Application.Seed;

public static class InitialTaxObligationSeedData
{
    private static readonly string[] MonthNames =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December"
    ];

    public static IReadOnlyCollection<InitialTaxObligationSeedItem> Items { get; } =
    [
        Item(1, "Finance", "Corporate Income Tax - CIT Return", "Corporate Income Tax - CIT", "Annual", "30th April Y+1", Month("April", "01/04\n15/04\n25/04")),
        Item(2, "Finance", "CIT Installment Return", "Four (4) Corporate Income Tax - CIT Instalments", "Four-Monthly", "Before: 1st August, 30th September, 30th November", Month("May", "01/05\n10/05"), Month("July", "01/07\n10/07"), Month("September", "01/09\n10/09"), Month("November", "01/11\n10/11")),
        Item(3, "Finance", "Payroll Taxes Return", "Payroll Taxes: IPR - IER - CNSS - INPP - ONEM", "Monthly", "15th M+1", AllMonths()),
        Item(4, "Finance", "VAT Return", "Value Added Tax", "Monthly", "15th M+1", AllMonths()),
        Item(5, "Finance", "WHT Return", "WHT on Foreign Service Suppliers (14%)", "Monthly", "15th M+1", AllMonths()),
        Item(6, "Finance", "WHT VAT Return", "WHT VAT on DRC State Owned Suppliers", "Monthly", "15th M+1", AllMonths()),
        Item(null, "Finance", "TP light return", "Transfer pricing", "Once a year", "30th June M+1", true, "Number missing in source matrix", AllMonths()),
        Item(7, "Finance", "WHT Income Tax (In particular on royalties and dividend payments) Return", "WHT Income Tax (In particular on royalties and dividend payments)", "Monthly", "15th M+1", AllMonths()),
        Item(8, "Finance", "Super Profit return", "Super Profit", "Exceptionally", "30th April Y+1", Month("April", "01/04\n10/04")),
        Item(9, "Finance", "ARSP (QST) Return", "ARSP (QST Subcontractor Tax)", null, null, true, "Frequency and legal deadline missing in source matrix", AllMonths()),
        Item(10, "Finance", "Redevance Suivi de Change (RSC) Return", "Redevance Suivi de Change (RSC)", "Monthly", "15th M+1", AllMonths()),
        Item(11, "Finance", "Mining Royalties Return", "Mining Royalties", "Ponctual", "5th M+1", AllMonths("Each Friday")),
        Item(12, "Finance", "Concentrate Tax Return", "Concentrate Tax", "Weekly/Advance", "Weekly (Every Friday)", AllMonths("Every Tuesday")),
        Item(13, "Finance", "Road Tax Return", "Road Tax", "Weekly/Advance", "Weekly (Every Friday)", AllMonths("Every Tuesday")),
        Item(14, "Finance", "Pollution Tax (TAPO) Return", "Pollution Tax (TAPO)", "Annual", "31st March Y+1 / 15th July Y+1", Month("March", "01/03\n10/03"), Month("June", "01/06\n10/06")),
        Item(15, "Finance", "Implantation Tax (TI) Return", "Implantation Tax (TI)", "Annual", "31st March Y+1 / 15th June Y+1", Month("March", "01/03\n10/03"), Month("June", "01/06\n10/06")),
        Item(16, "Finance", "Environmental Remunerative Tax (TRA) Return", "Environmental Remunerative Tax (TRA)", "Annual", "31st March Y+1 / 15th June Y+1", Month("March", "01/03\n10/03"), Month("June", "01/06\n10/06")),
        Item(17, "Finance & Legal", "Explosive - Warehouse approval tax Return", "Explosive - Warehouse approval tax", "Ponctual", "As needed", AllMonths()),
        Item(18, "Finance & Legal", "Tax on Temporary Blasting Permit Return", "Tax on Temporary Blasting Permit", "As needed", "As needed", AllMonths()),
        Item(19, "Finance & Legal", "Blasting attendance fee Return", "Blasting attendance fee", "As needed", "As needed", AllMonths()),
        Item(20, "Finance & Legal", "Blasters' approval (Boutefeu) Return", "Blasters' approval (Boutefeu)", "As needed", "As needed", AllMonths()),
        Item(21, "Finance", "Import & Export Number", "Import & Export Number", "Annual", "31st March Y+1", Month("March", "01/03\n10/03")),
        Item(22, "Finance", "Annual Surface rights per square", "Annual Surface rights per square", "Annual", "31st March Y+1", Month("March", "01/03\n10/03")),
        Item(23, "Finance", "Telecom Equipment Tax", "Telecom Equipment Tax", "Annual", "28th February Y+1", Month("February", "01/02\n10/02")),
        Item(24, "Finance", "Tax on Technical Inspection of Vehicle", "Tax on Technical Inspection of Vehicle and authorization of transport of goods and persons", "Annual", "DRLU/DRHKAT Announcement", Month("January", "Each 1st and 10th of the quarter"), Month("April", "Each 1st and 10th of the quarter"), Month("July", "Each 1st and 10th of the quarter"), Month("October", "Each 1st and 10th of the quarter")),
        Item(25, "Finance", "Vehicle Taxes (Vehicle tax & Special Road Traffic Tax)", "Vehicle Taxes (Vehicle tax & Special Road Traffic Tax)", "Annual", "DRLU/DRHKAT Announcement", Month("January", "Each 1st and 10th of the quarter"), Month("April", "Each 1st and 10th of the quarter"), Month("July", "Each 1st and 10th of the quarter"), Month("October", "Each 1st and 10th of the quarter")),
        Item(26, "Finance", "Vehicle Parking Tax", "Vehicle Parking Tax", "Annual", "DRLU/DRHKAT Announcement", Month("January", "Each 1st and 10th of the quarter"), Month("April", "Each 1st and 10th of the quarter"), Month("July", "Each 1st and 10th of the quarter"), Month("October", "Each 1st and 10th of the quarter")),
        Item(27, "Finance", "Tax on Aircraft Ticket", "Tax on Aircraft Ticket", "As needed", "As needed", AllMonths()),
        Item(28, "Tax on Aircraft Inspection", "Tax on Aircraft Inspection", "Tax on Aircraft Inspection", "As needed", "As needed", AllMonths()),
        Item(29, "Finance", "Police", "Police", "Monthly", "1-5 days receipt of NP", AllMonths()),
        Item(30, "Finance", "Visas", "Visas", "As per Expat contract with KCC", "Before Expat arrival", AllMonths()),
        Item(31, "Finance", "Work Permit", "Work Permit", "Per Request", "Before taking office", AllMonths()),
        Item(32, "Finance", "Resident Card", "Resident Card", "Per Request", "Any stay of more than six months", AllMonths()),
        Item(33, "Finance", "Royalty on Natural Water Consumption", "Royalty on Natural Water Consumption", "Monthly", "7 days receipt of NP", AllMonths()),
        Item(34, "Finance", "Property (Land) Tax", "Property (Land) Tax", "Annual", "1st Feb Y+1", Month("January", "01/01\n10/01")),
        Item(35, "Finance", "Surface Tax on Mining Concessions", "Surface Tax on Mining Concessions", "Annual", "1st Feb Y+1", Month("January", "01/01\n10/01")),
        Item(36, "Finance", "Tax on Rental Revenue (IRL)", "Tax on Rental Revenue (IRL)", "Annual", "1st Feb Y+1", Month("January", "01/01\n10/01"))
    ];

    private static InitialTaxObligationSeedItem Item(
        int? number,
        string department,
        string reportType,
        string taxCategory,
        string? frequency,
        string? legalDeadline,
        params InitialTaxObligationMonthlySchedule[] monthlySchedule)
    {
        return Item(number, department, reportType, taxCategory, frequency, legalDeadline, false, null, monthlySchedule);
    }

    private static InitialTaxObligationSeedItem Item(
        int? number,
        string department,
        string reportType,
        string taxCategory,
        string? frequency,
        string? legalDeadline,
        bool requiresReview,
        string? reviewReason,
        params InitialTaxObligationMonthlySchedule[] monthlySchedule)
    {
        return new InitialTaxObligationSeedItem(
            number,
            department,
            reportType,
            taxCategory,
            frequency,
            legalDeadline,
            requiresReview,
            reviewReason,
            monthlySchedule);
    }

    private static InitialTaxObligationMonthlySchedule Month(string monthName, string rawReminderText)
    {
        return new InitialTaxObligationMonthlySchedule(
            MonthNumber(monthName),
            monthName,
            rawReminderText,
            true);
    }

    private static InitialTaxObligationMonthlySchedule[] AllMonths(string? defaultReminderText = null)
    {
        return MonthNames
            .Select((month, index) => new InitialTaxObligationMonthlySchedule(
                index + 1,
                month,
                defaultReminderText,
                true))
            .ToArray();
    }

    private static int MonthNumber(string monthName)
    {
        var index = Array.FindIndex(MonthNames, item => item.Equals(monthName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new InvalidOperationException($"Unknown month '{monthName}'.");
        }

        return index + 1;
    }

}

public sealed record InitialTaxObligationSeedItem(
    int? Number,
    string Department,
    string ReportType,
    string TaxCategory,
    string? Frequency,
    string? LegalDeadline,
    bool RequiresReview,
    string? ReviewReason,
    IReadOnlyCollection<InitialTaxObligationMonthlySchedule> MonthlySchedule);

public sealed record InitialTaxObligationMonthlySchedule(
    int MonthNumber,
    string MonthName,
    string? RawReminderText,
    bool IsActive);
