using System;
using System.Collections.Generic;

namespace amenone.VcontainerExtensions.Lookups.Interface
{
    public interface IViewHashSet<T> : IDisposable
    {
        IEnumerable<T> GetAll();
    }
}