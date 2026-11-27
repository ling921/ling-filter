# Ling.Filter

[Project documentation](https://github.com/ling921/ling-filter#readme) | [简体中文](https://github.com/ling921/ling-filter/blob/master/src/Ling.Filter/README.zh-CN.md)

`Ling.Filter` is a zero-reflection, compile-time strongly-typed LINQ `Expression<Func<TEntity, bool>>` and EF Core `IQueryable<TEntity>` filter, sort, and pagination source generator, with bundled Roslyn analyzers and code fixes.

## Installation

```shell
dotnet add package Ling.Filter
```

## Quick Start

Mark your filter DTO as `partial` and implement `IFilterable<TEntity>`, `ISortable<TEntity>`, and/or `IPagedFilter`:

```csharp
using System.Linq.Expressions;
using Ling.Filter;
using Ling.Filter.Annotations;

[DefaultSort(nameof(Order.CreatedAt), Descending = true)]
public partial class OrderFilter : IFilterable<Order>, ISortable<Order>, IPagedFilter
{
    [MultiFieldFilter(nameof(Order.OrderNo), "Customer.Name")]
    public string? Keyword { get; set; }

    public ValueRange<decimal>? TotalAmount { get; set; }

    [DateFilter(DatePrecision.Day, nameof(Order.CreatedAt))]
    public DateTime? CreatedDate { get; set; }

    public OrderStatus[]? Statuses { get; set; }

    [CollectionFilter(nameof(Order.Items), CollectionQuantifier.Any, nameof(OrderItem.SkuCode), StringMode = StringFilterMode.StartsWith)]
    public string? ItemSkuPrefix { get; set; }

    [NullCheckFilter(nameof(Order.ShippedAt))]
    public bool? IsShipped { get; set; }

    [SortField("customerName", Target = "Customer.Name")]
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
```

Apply the filter to any `IQueryable<TEntity>` or convert it directly to an `Expression<Func<TEntity, bool>>`:

```csharp
// EF Core / LINQ IQueryable<TEntity>
var results = await dbContext.Orders
    .ApplyQuery(filter)
    .ToListAsync();

// Individual stages
var filtered = dbContext.Orders.ApplyFilter(filter).ApplySort(filter).ApplyPaging(filter);

// Expression tree for Repository / Specification patterns
Expression<Func<Order, bool>> predicate = filter.ToExpression();
```

## Attributes & Conventions

- **Conventions**: Matching property names generate equality predicates; `MinXxx`/`MaxXxx` and `StartXxx`/`EndXxx` map to `>=` / `<=`; collection properties map to `InFilter`; `ValueRange<T>` maps to range bounds.
- **`[StringFilter]`**: `Equals`, `NotEquals`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `Trim`, `CaseInsensitive`, and blank-string ignore policies.
- **`[CompareFilter]` / `[RangeFilter]`**: Explicit comparison operators and interval bounds.
- **`[DateFilter]`**: Index-friendly (sargable) `[start, end)` range expansion for `Year`, `Month`, `Day`, `Hour`, `Minute`, and `Second`.
- **`[MultiFieldFilter]` / `[FilterGroup]`**: Single keyword across multiple entity or navigation paths, and parenthesized `AND`/`OR` groups.
- **`[InFilter]` / `[NotInFilter]` / `[CollectionFilter]`**: Collection containment and child navigation quantifiers (`Any`, `All`, `None`).
- **`[NullCheckFilter]` / `[FlagsFilter]` / `[CustomFilter]`**: Nullable-boolean null checks, bitwise enum flags (`HasAll`, `HasAny`, `Exact`, `HasNone`), and custom `Expression<Func<TEntity, bool>>?` escape hatches.
- **`[SortField]` / `[DefaultSort]` / `[FilterOptions]` / `[FilterIgnore]`**: Safe dynamic sorting, type-level options, and unmapped property exclusion.

## Diagnostics

The bundled analyzer (`LF0001`–`LF0009`) validates `partial` modifiers, target entity and navigation paths, property/attribute type compatibility, unmapped properties, `[CustomFilter]` signatures, `[CollectionFilter]` targets, conflicting attributes, and empty `[MultiFieldFilter]` declarations at compile time, with IDE code fixes for `LF0001` and `LF0005`.

## License

[MIT](https://github.com/ling921/ling-filter/blob/master/LICENSE)
