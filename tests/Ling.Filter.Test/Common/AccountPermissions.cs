namespace Ling.Filter.Test.Common;

[Flags]
public enum AccountPermissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Export = 4,
    Admin = 8,
}
