namespace Amenone.ArchitectureGuard.Analyzers
{
    internal enum UnityTimeUsage
    {
        Unguarded,
        ScaledTimeRead,
        UnscaledTimeRead,
        SettingWrite,
    }
}
