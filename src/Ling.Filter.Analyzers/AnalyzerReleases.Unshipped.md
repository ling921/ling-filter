; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
LF0001 | Usage | Error | Filter or sortable type must be declared partial and non-generic
LF0002 | Usage | Error | Target property or navigation path not found on entity
LF0003 | Usage | Error | Filter attribute is incompatible with DTO property type
LF0004 | Usage | Error | Filter property type is incompatible with target entity property type
LF0005 | Usage | Warning | Public filter property is unmapped and lacks FilterIgnoreAttribute
LF0006 | Usage | Error | CustomFilterAttribute method has an invalid signature
LF0007 | Usage | Error | CollectionFilterAttribute target navigation or element path is invalid
LF0008 | Usage | Error | Conflicting filter attributes applied to the same property
LF0009 | Usage | Warning | MultiFieldFilterAttribute specifies no target fields
