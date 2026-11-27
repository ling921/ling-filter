# Ling.Filter

[项目文档](https://github.com/ling921/ling-filter#readme) | [English](https://github.com/ling921/ling-filter/blob/master/src/Ling.Filter/README.md)

`Ling.Filter` 是一个零反射、编译期强类型的 LINQ `Expression<Func<TEntity, bool>>` 与 EF Core `IQueryable<TEntity>` 查询筛选、动态排序、分页增量源生成器，内置 Roslyn 编译期分析器与代码修复。

## 安装

```shell
dotnet add package Ling.Filter
```

## 快速开始

将筛选 DTO 声明为 `partial` 并实现 `IFilterable<TEntity>`、`ISortable<TEntity>` 和/或 `IPagedFilter`：

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

将筛选模型应用到 `IQueryable<TEntity>`，或直接生成 `Expression<Func<TEntity, bool>>`：

```csharp
// EF Core / LINQ IQueryable<TEntity>
var results = await dbContext.Orders
    .ApplyQuery(filter)
    .ToListAsync();

// 分别应用筛选、排序与分页
var filtered = dbContext.Orders.ApplyFilter(filter).ApplySort(filter).ApplyPaging(filter);

// 获取表达式树用于 Repository / Specification 模式
Expression<Func<Order, bool>> predicate = filter.ToExpression();
```

## 特性与约定

- **智能约定**：同名属性默认生成相等判断；`MinXxx`/`MaxXxx`、`StartXxx`/`EndXxx` 自动映射为 `>=` / `<=`；集合属性映射为 `InFilter`；`ValueRange<T>` 自动映射区间上下界。
- **`[StringFilter]`**：支持 `Equals`、`NotEquals`、`Contains`、`NotContains`、`StartsWith`、`EndsWith`、`Trim`、`CaseInsensitive` 与空白忽略策略。
- **`[CompareFilter]` / `[RangeFilter]`**：显式指定比较运算符或开闭区间。
- **`[DateFilter]`**：按 `Year`、`Month`、`Day`、`Hour`、`Minute`、`Second` 展开为索引友好的 `[start, end)` 半开区间查询。
- **`[MultiFieldFilter]` / `[FilterGroup]`**：单关键词跨本表与导航属性多字段匹配，以及跨属性括号逻辑分组。
- **`[InFilter]` / `[NotInFilter]` / `[CollectionFilter]`**：集合包含判断与一对多导航集合量词过滤（`Any`、`All`、`None`）。
- **`[NullCheckFilter]` / `[FlagsFilter]` / `[CustomFilter]`**：`bool?` 空值检查、位标志枚举匹配（`HasAll`、`HasAny`、`Exact`、`HasNone`）与自定义 `Expression<Func<TEntity, bool>>?` 逃生舱。
- **`[SortField]` / `[DefaultSort]` / `[FilterOptions]` / `[FilterIgnore]`**：安全动态排序、类型级配置与显式忽略未映射属性。

## 编译期诊断

内置分析器（`LF0001`–`LF0009`）在编译期检查 `partial` 修饰符、目标实体与导航属性路径、属性与特性类型兼容性、未映射属性、`[CustomFilter]` 签名、`[CollectionFilter]` 目标、冲突特性以及空 `[MultiFieldFilter]`，并为 `LF0001` 与 `LF0005` 提供 IDE 一键修复。

## 协议

[MIT](https://github.com/ling921/ling-filter/blob/master/LICENSE)
