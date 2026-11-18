using Ling.Filter.Test.Common;

namespace Ling.Filter.Test.Filters;

public readonly partial record struct ValueStructEmployeeFilter(int? Id) : IFilterable<EmployeeEntity>;
