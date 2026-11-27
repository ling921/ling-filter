# Ling.Filter

[English](README.md) | 简体中文

[![Build](https://github.com/ling921/ling-filter/actions/workflows/test.yml/badge.svg)](https://github.com/ling921/ling-filter/actions/workflows/test.yml)
[![NuGet](https://img.shields.io/nuget/v/Ling.Filter.svg)](https://www.nuget.org/packages/Ling.Filter/)

`Ling.Filter` 是一个零反射、编译期强类型的 LINQ `Expression<Func<TEntity, bool>>` 与 EF Core `IQueryable<TEntity>` 查询筛选、动态排序、分页增量源生成器，内置完整的 Roslyn 编译期分析器与一键代码修复。

## 安装

```shell
dotnet add package Ling.Filter
```

包内已包含运行时契约（`IFilterable<TEntity>`、`ISortable<TEntity>`、`IPagedFilter`、`ValueRange<T>` 与筛选特性），以及内置的增量源生成器、Roslyn 分析器和代码修复提供程序。

## 核心特性

- **零反射 & 原生支持 Native AOT**：基于 Roslyn `IIncrementalGenerator` 编译期直接生成标准局部变量闭包 Lambda，100% 支持 EF Core SQL 参数化与查询计划缓存。
- **完备的筛选能力与约定优于配置**：
  - **字符串匹配**：`Equals`、`NotEquals`、`Contains`、`NotContains`、`StartsWith`、`EndsWith`，支持 `Trim`、`CaseInsensitive` 与空白忽略策略。
  - **数值与区间筛选**：支持比较符、`MinXxx`/`MaxXxx`/`StartXxx`/`EndXxx` 自动约定以及 `ValueRange<T>` 上下界开闭区间。
  - **索引友好（Sargable）日期精度匹配**：`Year`、`Month`、`Day`、`Hour`、`Minute`、`Second` 精度自动展开为 `[start, end)` 区间查询，避免数据库列函数导致索引失效。
  - **组合条件**：`[MultiFieldFilter]` 单关键词跨本表及导航属性多字段搜索，以及 `[FilterGroup]` 跨属性括号逻辑分组。
  - **集合与导航穿透**：`[InFilter]` / `[NotInFilter]`（空集合自动跳过），以及一对多子集合 `[CollectionFilter]`（`Any` / `All` / `None`）。
  - **空值检查、位标志与自定义逃生舱**：`[NullCheckFilter]`、`[FlagsFilter]`、`[CustomFilter]`。
  - **安全动态排序与分页**：实现 `ISortable<TEntity>` 与 `IPagedFilter`，使用 `query.ApplyQuery(filter)` 一站式应用。
- **编译期防呆校验**：内置 `LF0001`–`LF0009` 诊断分析器与 `CodeFixProvider`（如自动补全 `partial`、一键添加 `[FilterIgnore]`）。

## 快速开始

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

// 在 EF Core IQueryable<Order> 上一站式应用筛选、排序与分页：
var orders = await dbContext.Orders
    .ApplyQuery(filter)
    .ToListAsync();

// 或获取 Expression<Func<Order, bool>> 用于 Repository / Specification 模式：
Expression<Func<Order, bool>> predicate = filter.ToExpression();
```

也可以通过 `ApplyFilter`、`ApplySort` 和 `ApplyPaging` 单独应用筛选、排序或分页。

## 筛选特性一览

| 特性 / 类型 | 用途 |
| --- | --- |
| `[StringFilter]` | 字符串匹配模式（`Contains`、`StartsWith`、`EndsWith`、`Equals` 等）、去除首尾空白、大小写敏感度与空白忽略策略。 |
| `[CompareFilter]` | 显式比较运算符（`Equal`、`NotEqual`、`GreaterThan`、`GreaterThanOrEqual`、`LessThan`、`LessThanOrEqual`）。 |
| `[RangeFilter]` / `ValueRange<T>` | 闭区间或半开区间筛选，支持配置上下界是否包含端点与可选 `DatePrecision` 日期精度。 |
| `[DateFilter]` | 按 `Year`、`Month`、`Day`、`Hour`、`Minute`、`Second` 展开为索引友好的 `[start, end)` 区间查询。 |
| `[MultiFieldFilter]` | 将单个关键词同时匹配多个本表或导航属性（`OR` / `AND`）。 |
| `[FilterGroup]` | 将多个筛选属性组合为带括号的逻辑子表达式。 |
| `[InFilter]` / `[NotInFilter]` | 集合包含 / 排除检查（`Contains` / `!Contains`）。 |
| `[CollectionFilter]` | 导航集合量词过滤（`Any`、`All`、`None`）。 |
| `[NullCheckFilter]` | 由 `bool?` 属性驱动的 `== null` / `!= null` 空值检查。 |
| `[FlagsFilter]` | 位标志枚举匹配（`HasAll`、`HasAny`、`Exact`、`HasNone`）。 |
| `[CustomFilter]` | 自定义 `Expression<Func<TEntity, bool>>?` 筛选方法的逃生舱（支持 `Group` 分组）。 |
| `[SortField]` / `[DefaultSort]` | 将允许的排序键映射到实体或导航属性路径，并配置单级或多级默认排序（`OrderBy`/`ThenBy`）。 |
| `[FilterOptions]` / `[FilterIgnore]` | 配置类型级组合逻辑、字符串与排序白名单选项，或显式忽略未映射的 DTO 属性。 |

## 编译期诊断

| 诊断 ID | 说明 |
| --- | --- |
| `LF0001` | 筛选或排序类型（及其所有包含类型）必须声明为 `partial` 且非泛型。 |
| `LF0002` | 指定的目标属性或导航路径在目标实体上不存在或不可访问。 |
| `LF0003` | 筛选特性应用在了不兼容的 DTO 属性类型上。 |
| `LF0004` | 筛选属性类型无法与映射的目标实体属性类型进行比较。 |
| `LF0005` | 筛选模型上的公开可读属性未能映射到实体属性，且未标记 `[FilterIgnore]`。 |
| `LF0006` | 标记了 `[CustomFilter]` 的方法签名无效。 |
| `LF0007` | `[CollectionFilter]` 指定的导航属性不是集合，或元素属性路径无效。 |
| `LF0008` | 同一属性上同时标记了相互冲突的筛选特性。 |
| `LF0009` | `[MultiFieldFilter]` 未指定任何目标字段。 |

## 示例

- **[EF Core & SQLite 示例](samples/Ling.Filter.Sample)** — 演示 `OrderFilter` 结合 `MultiFieldFilter`、`ValueRange<T>`、`DateFilter`、`CollectionFilter`、`NullCheckFilter`、动态排序与分页在内存 SQLite EF Core 上下文中的用法与生成的参数化 SQL。
  运行：`dotnet run --project samples/Ling.Filter.Sample`

## 开发

- 构建：`dotnet build Ling.Filter.slnx`
- 测试：`dotnet test Ling.Filter.slnx`
- 打包：`dotnet pack src/Ling.Filter/Ling.Filter.csproj -c Release`

## 参与贡献

欢迎提交 Issue 或 Pull Request。行为变更请同时补充对应测试。

## 协议

[MIT](LICENSE)
