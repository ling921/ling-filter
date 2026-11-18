using Ling.Filter;
using Ling.Filter.NativeAotSmoke;

var items = new[]
{
    new SmokeProduct { Id = 1, Name = "Mechanical Keyboard", Price = 199m, CreatedAt = new DateTime(2026, 11, 18, 8, 0, 0) },
    new SmokeProduct { Id = 2, Name = "Gaming Mouse", Price = 79m, CreatedAt = new DateTime(2026, 11, 18, 9, 0, 0) },
};

var filter = new SmokeProductFilter
{
    Name = "Keyboard",
    Price = ValueRange<decimal>.Between(100m, 300m),
    CreatedAt = new DateTime(2026, 11, 18),
    SortBy = "Price",
    Page = 1,
    PageSize = 5,
};

var predicate = filter.ToExpression().Compile();
var matched = items.Where(predicate).ToArray();
if (matched.Length != 1 || matched[0].Id != 1)
{
    throw new InvalidOperationException("Smoke filter check failed.");
}

Console.WriteLine("Ling.Filter AOT/Trimming smoke test passed.");
