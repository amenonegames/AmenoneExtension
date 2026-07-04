using System.Collections.Generic;
using System.Linq;
using Amenone.VcontainerExtensions.Lookups.Interface;
using Amenone.VcontainerExtensions.Lookups.Storage;
using VContainer;

namespace Amenone.VcontainerExtensions.Lookups
{
    public abstract class NameableHashSetBase<T> : IViewHashSet<T>
    {
        [Inject]
        protected NameableHashSetBase(IRegisterMarkerStorage list)
        {
            _hash = list.RegisterMarkers
                .OfType<T>()
                .ToHashSet();
        }

        private HashSet<T> _hash { get; set; }

        public IEnumerable<T> GetAll()
        {
            return _hash;
        }

        public void Dispose()
        {
            _hash.Clear();
            _hash = null;
        }
    }
}