using System;
using amenone.VcontainerExtensions.Identifier;

namespace amenone.VcontainerExtensions.Lookups.Storage
{
    public interface IRegisterMarkerStorage : IDisposable
    {
        IRegisterMarker[] RegisterMarkers { get; }
    }
}