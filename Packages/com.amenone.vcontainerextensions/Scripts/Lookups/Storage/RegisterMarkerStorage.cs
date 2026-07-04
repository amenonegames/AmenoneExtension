using System.Collections.Generic;
using System.Linq;
using Amenone.VcontainerExtensions.Identifier;

namespace Amenone.VcontainerExtensions.Lookups.Storage
{
    public class RegisterMarkerStorage : IRegisterMarkerStorage
    {
        private IRegisterMarker[] _registerMarkers;

        public IRegisterMarker[] RegisterMarkers => _registerMarkers;

        public RegisterMarkerStorage( IReadOnlyList<IRegisterMarker> registerMarkers)
        {
            _registerMarkers = registerMarkers.ToArray();
        }

        public void Dispose()
        {
            _registerMarkers = null;
        }
    }
}