using System;

namespace Amenone.ArchitectureGuard
{
    [AttributeUsage(
        AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Constructor
        | AttributeTargets.Method
        | AttributeTargets.Property
        | AttributeTargets.Field
        | AttributeTargets.Event,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class AllowUnityTimeAttribute : Attribute
    {
        public AllowUnityTimeAttribute(string reason) => Reason = reason;

        public string Reason { get; }
    }
}
