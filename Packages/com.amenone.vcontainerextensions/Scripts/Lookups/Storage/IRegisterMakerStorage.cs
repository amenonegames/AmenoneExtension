using System;
using Amenone.VcontainerExtensions.Identifier;

namespace Amenone.VcontainerExtensions.Lookups.Storage
{
    public interface IRegisterMarkerStorage : IDisposable
    {
        IRegisterMarker[] RegisterMarkers { get; }
    }
}