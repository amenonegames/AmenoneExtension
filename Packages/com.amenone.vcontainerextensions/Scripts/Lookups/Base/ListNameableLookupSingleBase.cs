using System.Collections.Generic;
using System.Linq;
using Amenone.VcontainerExtensions.Identifier;
using Amenone.VcontainerExtensions.Lookups.Interface;
using Amenone.VcontainerExtensions.Lookups.Storage;
using VContainer;

namespace Amenone.VcontainerExtensions.Lookups
{
    public abstract class
        ListNameableLookupSingleBase<TKeyInList,  TValue > : IViewLookupSingleInstanceFromList<TKeyInList, TValue >
        where TValue : IListNameable<TKeyInList>
    {
        protected Dictionary<IEnumerable<TKeyInList>, TValue> _dictionary { get; set; }
        private List<TKeyInList> _AllKeys { get; set; }

        [Inject]
        protected ListNameableLookupSingleBase(IRegisterMarkerStorage list)
        {
            _dictionary = list.RegisterMarkers
                .OfType<TValue>()
                .ToDictionary(x => x.Names);

            _AllKeys = new List<TKeyInList>();
            foreach (var key in _dictionary.Select(x => x.Key)) _AllKeys.AddRange(key);
        }

        public IEnumerable<TValue> Get(TKeyInList name)
        {
            IEnumerable<KeyValuePair<IEnumerable<TKeyInList>,TValue>> result =  _dictionary.Where(x => x.Key.Contains(name));
            IEnumerable<TValue> returnResult = result.Select(x => x.Value);

            return returnResult;
        }

        public IEnumerable<TValue> GetAll()
        {
            return _dictionary.Values;
        }

        public IEnumerable<TValue> GetExcept(TKeyInList name)
        {
            List<TValue> except = new();

            foreach (var keyValue in _dictionary)
            {
                if (keyValue.Key.Equals(name)) continue;
                except.Add(keyValue.Value);
            }

            return except;
        }

        public (TValue match, IEnumerable<TValue> except) GetMatchAndExcept(TKeyInList name)
        {
            List<TValue> except = new();
            TValue match = default;

            foreach (var keyValue in _dictionary)
                if (keyValue.Key.Equals(name)) match = keyValue.Value;
                else except.Add(keyValue.Value);

            return (match, except);
        }

        public bool ContainsKey(TKeyInList name)
        {
            return _AllKeys.Contains(name);
        }

        public void Dispose()
        {
            _dictionary.Clear();
            _AllKeys.Clear();
            _dictionary = null;
            _AllKeys = null;
        }
    }
}