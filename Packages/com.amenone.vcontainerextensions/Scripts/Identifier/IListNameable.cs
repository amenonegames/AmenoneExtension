using System.Collections.Generic;

namespace Amenone.VcontainerExtensions.Identifier
{
    public interface IListNameable<T> 
    {
        IEnumerable<T> Names { get; }
    }
}