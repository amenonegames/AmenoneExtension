using System;

namespace Amenone.ArchitectureGuard
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class AllowUnityMessageAttribute : Attribute
    {
        public AllowUnityMessageAttribute(string reason) => Reason = reason;

        public string Reason { get; }
    }
}
