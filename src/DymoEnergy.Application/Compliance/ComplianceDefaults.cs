using System;
using System.Collections.Generic;

namespace DymoEnergy.Compliance;

/// <summary>Built-in wording and starter lists; everything here can be edited from the page.</summary>
public static class ComplianceDefaults
{
    public record LabelDef(string Key, string Group, string Caption, string Default);

    public static readonly IReadOnlyList<LabelDef> Labels = new List<LabelDef>
    {
        new("page.title",      "Page", "Page title", "Compliance & documents"),
        new("page.subtitle",   "Page", "Subtitle", "Licences, tax filings, product certificates and the paperwork for every installation — in one place, with reminders."),
        new("btn.download",    "Page", "Download button", "Download all (ZIP)"),
        new("btn.upload",      "Page", "Upload button", "Upload document"),

        new("status.question", "Summary", "Question above the status", "Everything in order?"),
        new("status.good",     "Summary", "Status word: all good", "Yes"),
        new("status.almost",   "Summary", "Status word: some problems", "Almost"),
        new("status.bad",      "Summary", "Status word: many problems", "No"),
        new("card.licences",   "Summary", "Licences card", "Licences"),
        new("card.filings",    "Summary", "Filings card", "Filings"),
        new("card.certificates", "Summary", "Certificates card", "Certificates"),
        new("card.packs",      "Summary", "Project packs card", "Project packs"),
        new("alert.button",    "Summary", "Red banner button", "Assign someone"),
        new("alert.footnote",  "Summary", "Red banner closing sentence", "Both are asked for in tenders."),

        new("tab.licences",    "Tabs", "Licences tab", "Licences"),
        new("tab.filings",     "Tabs", "Filings tab", "Tax & filings"),
        new("tab.certificates", "Tabs", "Certificates tab", "Product certificates"),
        new("tab.packs",       "Tabs", "Project documents tab", "Project documents"),
        new("intro.licences",  "Tabs", "Licences intro", "Every licence and registration the business runs on. The bar shows how much of its validity is left."),
        new("intro.filings",   "Tabs", "Filings intro", "Returns and payments that come back every month or year. Tick them off here so nothing is missed."),
        new("intro.certificates", "Tabs", "Certificates intro", "Proof that what you sell meets the standards — needed for net metering approval, for tenders, and when a customer asks."),
        new("intro.packs",     "Tabs", "Project documents intro", "The document pack every installation must end with. A job cannot be marked handed over until the pack is complete."),

        new("renew.title",     "Licences tab", "Reminders heading", "Renewal reminders"),
        new("renew.sub",       "Licences tab", "Reminders subtitle", "Who gets told, and how early."),
        new("access.title",    "Licences tab", "Access heading", "Who can see what"),
        new("access.sub",      "Licences tab", "Access subtitle", "Documents here hold bank and tax details — not everyone should open them."),

        new("filings.month",   "Filings tab", "This-month heading", "This month"),
        new("filings.history", "Filings tab", "History heading", "Filing history"),
        new("filings.historyNote", "Filings tab", "Note under the history", ""),
        new("stat.due",        "Filings tab", "Stat: due this month", "Due this month"),
        new("stat.onTime",     "Filings tab", "Stat: filed on time", "Filed on time"),
        new("stat.late",       "Filings tab", "Stat: late filings", "Late once"),

        new("certs.badges",    "Certificates tab", "Badge glossary heading", "What each badge means"),
        new("certs.badgesNote", "Certificates tab", "Note under the glossary", "Which certificates are compulsory changes over time — confirm the current list with your supplier and the utility before a tender."),
        new("import.title",    "Certificates tab", "Import papers heading", "Import papers"),
        new("import.sub",      "Certificates tab", "Import papers subtitle", "Kept per shipment, matched to the stock entry it created."),

        new("packs.title",     "Project documents tab", "Pack grid heading", "Document pack per project"),
        new("templates.title", "Project documents tab", "Templates heading", "Templates we send"),
        new("templates.sub",   "Project documents tab", "Templates subtitle", "same wording every time"),
        new("retention.title", "Project documents tab", "Retention heading", "How long we keep things"),
        new("retention.sub",   "Project documents tab", "Retention subtitle", "Nothing is deleted automatically — this is just what the system reminds you about."),
        new("retention.note",  "Project documents tab", "Note under retention", "Check these periods with your accountant — tax and warranty rules decide them, not us."),
    };

    public static readonly string[] ImportDocs = { "LC", "Invoice", "Packing list", "Bill of entry", "Duty receipt" };

    public record LicenceSeed(string Name, string Description, string Number, string IssuedBy, string Owner, bool HasExpiry);

    public static readonly LicenceSeed[] Licences =
    {
        new("Trade licence", "City Corporation · renew every year", "", "City Corporation", "Admin", true),
        new("VAT registration (BIN)", "No expiry, keep details current", "", "NBR", "Accounts", false),
        new("TIN certificate", "Company income tax number", "", "NBR", "Accounts", false),
        new("Import registration (IRC)", "Needed to import panels and inverters", "", "CCI&E", "Admin", true),
        new("Fire licence", "Warehouse", "", "Fire Service", "Store manager", true),
        new("Warehouse rent agreement", "Term of the rent agreement", "", "Landlord", "Admin", true),
        new("Electrical supervisor licence", "For the engineer who signs installations", "", "", "Engineer", true),
        new("Bank solvency certificate", "Asked for in tenders", "", "", "Accounts", true),
        new("Trade body membership", "Renewed with the yearly subscription", "", "", "Admin", true),
    };

    public record ItemSeed(ComplianceItemKind Kind, string Title, string? Description = null, string? Extra = null,
                           string? Color = null, int? Number = null, bool Flag = false);

    public static readonly ItemSeed[] Items =
    {
        new(ComplianceItemKind.Badge, "IEC 61215", "Panel performance and durability test — the usual proof of quality for solar panels.", Color: "#0B5A34"),
        new(ComplianceItemKind.Badge, "IEC 61730", "Panel safety test (electrical and fire).", Color: "#0B5A34"),
        new(ComplianceItemKind.Badge, "IEC 62109", "Inverter safety standard.", Color: "#0B5A34"),
        new(ComplianceItemKind.Badge, "Grid code", "Inverter is allowed to connect to the grid — the utility asks for this on net-metering jobs.", Color: "#1E40AF"),
        new(ComplianceItemKind.Badge, "Factory", "Manufacturer certificate and warranty letter, in the supplier's name.", Color: "#5F6B63"),
        new(ComplianceItemKind.Badge, "IEC 62619", "Safety standard for industrial lithium batteries.", Color: "#0B5A34"),
        new(ComplianceItemKind.Badge, "UN 38.3", "Transport safety test for lithium batteries.", Color: "#0B5A34"),
        new(ComplianceItemKind.Badge, "IEC 62509", "Performance and functioning of battery charge controllers.", Color: "#0B5A34"),

        new(ComplianceItemKind.DocType, "Signed quote"),
        new(ComplianceItemKind.DocType, "Survey report"),
        new(ComplianceItemKind.DocType, "Net metering"),
        new(ComplianceItemKind.DocType, "Serial list"),
        new(ComplianceItemKind.DocType, "Handover sheet"),
        new(ComplianceItemKind.DocType, "Warranty pack"),

        new(ComplianceItemKind.Template, "Quotation & contract", "Prices, scope, payment terms, what is not included", "v1", "#0E6B3F"),
        new(ComplianceItemKind.Template, "Site survey report", "Roof, shade, load, photos", "v1", "#0E6B3F"),
        new(ComplianceItemKind.Template, "Net metering application pack", "Drawings the utility asks for", "v1", "#2563EB"),
        new(ComplianceItemKind.Template, "Installation job sheet", "Signed by the customer on the day", "v1", "#0E6B3F"),
        new(ComplianceItemKind.Template, "Handover sheet", "What was installed, serials, how to use it", "v1", "#0E6B3F"),
        new(ComplianceItemKind.Template, "Warranty card", "Per product, with serial and end date", "v1", "#D97706"),

        new(ComplianceItemKind.Retention, "Invoices and VAT papers", "Sales, purchase and import documents", Number: 6),
        new(ComplianceItemKind.Retention, "Project document packs", "Survey to handover, per installation", Number: 10),
        new(ComplianceItemKind.Retention, "Warranty and serial records", "Until the longest warranty ends", Number: 25),
        new(ComplianceItemKind.Retention, "Staff and salary records", "Kept with HR", Number: 6),

        new(ComplianceItemKind.AccessRule, "Owner", Extra: "Everything"),
        new(ComplianceItemKind.AccessRule, "Accounts", Extra: "Licences, Filings, Import papers"),
        new(ComplianceItemKind.AccessRule, "Engineer", Extra: "Certificates, Project documents"),
        new(ComplianceItemKind.AccessRule, "Counter staff", Extra: "Project documents"),

        new(ComplianceItemKind.Reminder, "60, 30 and 7 days before expiry", "Email + dashboard card to the owner of the licence", Flag: true),
        new(ComplianceItemKind.Reminder, "Monthly filing reminder", "On the 5th, to Accounts", Flag: true),
        new(ComplianceItemKind.Reminder, "Certificate expiry", "When a product certificate has 30 days left", Flag: false),
    };

    public record FilingSeed(string Title, string Detail, int Day, string Owner, string Action);

    public static readonly FilingSeed[] Filings =
    {
        new("Monthly VAT return", "Prepare and submit the return", 15, "Accounts", "Open report"),
        new("Supplier VAT deduction certificates", "Certificates for supplier payments", 20, "Accounts", "Start"),
        new("Staff income tax deduction", "Monthly deposit for staff", 25, "Accounts", "Start"),
        new("Purchase and sales registers", "Kept up to date", 31, "Accounts", "View"),
    };
}
