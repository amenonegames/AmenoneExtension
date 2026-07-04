using System.Collections.Generic;
using System.Linq;
using Amenone.VcontainerExtensions.Identifier;
using Amenone.VcontainerExtensions.Lookups.Interface;
using Amenone.VcontainerExtensions.Lookups.Storage;
using VContainer;

namespace Amenone.VcontainerExtensions.Lookups
{
    public abstract class
        ListNameableLookupEnumerableBase< TKeyInList, TValue > : IViewLookupEnumerableFromList<TKeyInList, TValue>
        where TValue : IListNameable<TKeyInList>
    {
        private ILookup<IEnumerable<TKeyInList>, TValue> _lookup { get; set; }
        private List<TKeyInList> _AllKeys { get; set; }

        [Inject]
        protected ListNameableLookupEnumerableBase(IRegisterMarkerStorage list)
        {

            _lookup = list.RegisterMarkers
                .OfType<TValue>()
                .ToLookup(x => x.Names);

            _AllKeys = new List<TKeyInList>();
            foreach (var key in _lookup.Select(x => x.Key))
            {
                if(key is null) continue;
                _AllKeys.AddRange(key);
            }
        }

        public IEnumerable<TValue> Get(TKeyInList name)
        {
            return _lookup.Where(x => x.Key.Contains(name)).SelectMany(x => x);
        }

        public IEnumerable<TValue> GetAll()
        {
            return _lookup.SelectMany(x => x);
        }

        public IEnumerable<TValue> GetExcept(TKeyInList name)
        {
            List<TValue> except = new();

            foreach (var keyValue in _lookup)
            {
                if (keyValue.Key.Contains(name)) continue;
                except.AddRange(keyValue);
            }

            return except;
        }

        public (IEnumerable<TValue> match, IEnumerable<TValue> except) GetMatchAndExcept(TKeyInList name)
        {
            var except = new List<TValue>();
            List<TValue> match = new();

            foreach (var keyValue in _lookup)
                if (keyValue.Key.Contains(name)) match.AddRange(keyValue);
                else except.AddRange(keyValue);

            return (match, except);
        }

        public bool ContainsKey(TKeyInList name)
        {
            return _AllKeys.Contains(name);
        }

        public void Dispose()
        {
            _AllKeys.Clear();
            _AllKeys = null;
            _lookup = null;
        }
    }
}