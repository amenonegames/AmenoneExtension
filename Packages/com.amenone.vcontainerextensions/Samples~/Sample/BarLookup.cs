using Amenone.VcontainerExtensions.Lookups;
using Amenone.VcontainerExtensions.Lookups.Storage;

namespace Amenone.VcontainerExtensions.Sample
{
    public class BarLookup : NameableLookupEnumerableBase<string,IBar>
    {
        public BarLookup(IRegisterMarkerStorage list) : base(list)
        {
        }
    }
}