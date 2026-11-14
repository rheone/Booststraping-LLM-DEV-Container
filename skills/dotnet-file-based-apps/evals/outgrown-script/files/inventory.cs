// Inventory report: reads items.csv-like data embedded below and prints stock summaries.
var items = Seed.Load();
var report = new ReportBuilder(items);

Console.WriteLine(report.Header());
foreach (var line in report.LowStock(threshold: 5))
    Console.WriteLine(line);
Console.WriteLine(report.ValueByCategory());
Console.WriteLine(report.Footer());
return items.Count == 0 ? 1 : 0;

enum Category { Hardware, Software, Office }

record Item(string Sku, string Name, Category Category, int Quantity, decimal UnitPrice)
{
    public decimal Value => Quantity * UnitPrice;
}

static class Seed
{
    public static List<Item> Load() =>
    [
        new("HW-001", "Keyboard", Category.Hardware, 12, 49.99m),
        new("HW-002", "Mouse", Category.Hardware, 3, 19.99m),
        new("HW-003", "Monitor", Category.Hardware, 2, 229.00m),
        new("SW-001", "IDE License", Category.Software, 40, 89.00m),
        new("SW-002", "Backup Suite", Category.Software, 1, 300.00m),
        new("OF-001", "Paper Ream", Category.Office, 55, 6.50m),
        new("OF-002", "Stapler", Category.Office, 4, 12.25m),
    ];
}

sealed class ReportBuilder(List<Item> items)
{
    public string Header() => $"Inventory report: {items.Count} SKUs";

    public IEnumerable<string> LowStock(int threshold) =>
        items.Where(i => i.Quantity < threshold)
             .OrderBy(i => i.Quantity)
             .Select(i => $"LOW  {i.Sku,-7} {i.Name,-14} qty={i.Quantity}");

    public string ValueByCategory() =>
        string.Join(Environment.NewLine,
            items.GroupBy(i => i.Category)
                 .OrderBy(g => g.Key)
                 .Select(g => $"{g.Key,-9} {Money.Format(g.Sum(i => i.Value))}"));

    public string Footer() => $"Total value: {Money.Format(items.Sum(i => i.Value))}";
}

static class Money
{
    public static string Format(decimal amount) =>
        amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
}
