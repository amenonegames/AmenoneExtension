using System;
using System.Collections.Generic;

namespace Amenone.VcontainerExtensions.Lookups.Interface
{
    public interface IViewHashSet<T> : IDisposable
    {
        IEnumerable<T> GetAll();
    }
}