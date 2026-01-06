using System;
using System.Collections.Generic;

namespace amenone.VcontainerExtensions.Lookups.Interface
{
    public interface IViewLookupEnumerable<TKey, TValue> : IDisposable
    {
        IEnumerable<TValue> Get(TKey name);
        IEnumerable<TValue> GetAll();
        IEnumerable<TValue> GetExcept(TKey name);
        (IEnumerable<TValue> match, IEnumerable<TValue> except) GetMatchAndExcept(TKey name);
    }

    public interface IViewLookupEnumerableFromList<TKeyInList, TValue> : IDisposable
    {
        IEnumerable<TValue> Get(TKeyInList name);
        IEnumerable<TValue> GetAll();
        IEnumerable<TValue> GetExcept(TKeyInList name);
        (IEnumerable<TValue> match, IEnumerable<TValue> except) GetMatchAndExcept(TKeyInList name);
        bool ContainsKey(TKeyInList name);
    }
}