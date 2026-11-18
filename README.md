# Ling.Filter

English | [简体中文](README.zh-CN.md)

[![Build](https://github.com/ling921/ling-filter/actions/workflows/test.yml/badge.svg)](https://github.com/ling921/ling-filter/actions/workflows/test.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Filter.svg)](https://www.nuget.org/packages/Ling.Filter/)

`Ling.Filter` is a zero-reflection, compile-time strongly-typed LINQ `Expression<Func<TEntity, bool>>` and EF Core `IQueryable<TEntity>` filter, sort, and pagination source generator, with built-in Roslyn analyzers and code fixes.

## Installation

```shell
dotnet add package Ling.Filter
```

The package bundles the runtime contracts (`IFilterable<TEntity>`, `ISortable<TEntity>`, `IPagedFilter`, `ValueRange<T>`, and filter attributes) together with the incremental source generator, Roslyn analyzers, and code fixes.

## Features

- **Zero Reflection & Native AOT Ready**: Uses Roslyn `IIncrementalGenerator` to emit native C# closure-parameterized LINQ expressions with full EF Core SQL parameterization and query plan caching support.
- **Rich Declarative Annotations & Smart Conventions**:
  - **String filtering**: `Equals`, `NotEquals`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `Trim`, `CaseInsensitive`, and blank-string ignore policies.
  - **Numeric & comparable filtering**: `==`, `!=`, `>`, `>=`, `<`, `<=`, `MinXxx`/`MaxXxx`/`StartXxx`/`EndXxx` conventions, and `ValueRange<T>` with configurable inclusive/exclusive bounds.
  - **Index-friendly (Sargable) date precision**: Automatically expands `Year`, `Month`, `Day`, `Hour`, `Minute`, and `Second` precision into half-open `[start, end)` range predicates instead of wrapping columns in SQL functions.
  - **Multi-field & logical grouping**: `[MultiFieldFilter]` for cross-property/navigation keyword search and `[FilterGroup]` for parenthesized `AND`/`OR` logic across properties.
  - **Collection & navigation quantifiers**: `[InFilter]` / `[NotInFilter]` (empty collections automatically skipped) and `[CollectionFilter]` (`Any`, `All`, `None`) for one-to-many navigations.
  - **Null checks, bitwise flags & custom predicates**: `[NullCheckFilter]`, `[FlagsFilter]`, and `[CustomFilter]`.
  - **Safe dynamic sorting & pagination**: Implement `ISortable<TEntity>` and `IPagedFilter` and apply everything in one call with `query.ApplyQuery(filter)`.
- **Compile-Time Safety**: Ships with Roslyn analyzers (`LF0001`–`LF0009`) and IDE quick fixes (`CodeFixProvider`).

## Quick Start

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

    [CollectionFilter(nameof(Order.Items), CollectionQuantifier.Any, "SkuCode", StringFilterMode.StartsWith)]
    public string? ItemSkuPrefix { get; set; }

    [NullCheckFilter(nameof(Order.ShippedAt))]
    public bool? IsShipped { get; set; }

    [SortField("customerName", TargetProperty = "Customer.Name")]
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

// Usage with EF Core IQueryable<Order>:
var orders = await dbContext.Orders
    .ApplyQuery(filter)
    .ToListAsync();

// Or obtain Expression<Func<Order, bool>> for Repository / Specification patterns:
Expression<Func<Order, bool>> predicate = filter.ToExpression();
```

You can also apply filtering, sorting, or paging individually via `ApplyFilter`, `ApplySort`, and `ApplyPaging`.

## Filter Attributes

| Attribute / Type | Purpose |
| --- | --- |
| `[StringFilter]` | String matching mode (`Contains`, `StartsWith`, `EndsWith`, `Equals`, etc.), trimming, case sensitivity, and ignore policy. |
| `[CompareFilter]` | Explicit comparison operator (`Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`). |
| `[RangeFilter]` / `ValueRange<T>` | Closed or half-open interval filtering with configurable bound inclusivity. |
| `[DateFilter]` | Sargable `[start, end)` range expansion for `Year`, `Month`, `Day`, `Hour`, `Minute`, and `Second`. |
| `[MultiFieldFilter]` | Matches a single keyword against multiple entity or navigation properties (`OR` / `AND`). |
| `[FilterGroup]` | Groups multiple filter properties into a parenthesized logical sub-expression. |
| `[InFilter]` / `[NotInFilter]` | Collection containment checks (`Contains` / `!Contains`). |
| `[CollectionFilter]` | Navigation collection quantifiers (`Any`, `All`, `None`). |
| `[NullCheckFilter]` | `== null` / `!= null` checks driven by a `bool?` property. |
| `[FlagsFilter]` | Bitwise flag matching (`HasAny`, `HasAll`, `Exact`). |
| `[CustomFilter]` | Escape hatch for custom `Expression<Func<TEntity, bool>>` or `IQueryable<TEntity>` filter methods. |
| `[SortField]` / `[DefaultSort]` | Maps allowed sort keys to entity/navigation paths and sets default ordering. |
| `[FilterOptions]` / `[FilterIgnore]` | Configures type-level combination logic/options or excludes unmapped DTO properties. |

## Diagnostics

| ID | Summary |
| --- | --- |
| `LF0001` | Filter or sortable type (and any containing types) must be declared `partial` and non-generic. |
| `LF0002` | Specified target property or navigation path does not exist or is inaccessible on the target entity. |
| `LF0003` | Filter attribute is applied to an incompatible DTO property type. |
| `LF0004` | Filter property type cannot be compared against the mapped target entity property type. |
| `LF0005` | Public readable property on filter model could not be mapped to any entity property and lacks `[FilterIgnore]`. |
| `LF0006` | Method marked with `[CustomFilter]` has an invalid signature. |
| `LF0007` | `[CollectionFilter]` navigation property is not a collection or element property path is invalid. |
| `LF0008` | Conflicting or mutually exclusive filter attributes are applied to the same property. |
| `LF0009` | `[MultiFieldFilter]` specifies zero target fields. |

## Samples

- **[EF Core & SQLite Sample](samples/Ling.Filter.Sample)** — Demonstrates `OrderFilter` with `MultiFieldFilter`, `ValueRange<T>`, `DateFilter`, `CollectionFilter`, `NullCheckFilter`, sorting, and paging against an in-memory SQLite EF Core context.
  Run: `dotnet run --project samples/Ling.Filter.Sample`

## Development

- Build: `dotnet build Ling.Filter.slnx`
- Test: `dotnet test Ling.Filter.slnx`
- Pack: `dotnet pack src/Ling.Filter/Ling.Filter.csproj -c Release`

## Contributing

Contributions are welcome. Please open an issue or pull request and include tests for behavior changes.

## License

[MIT](LICENSE)
