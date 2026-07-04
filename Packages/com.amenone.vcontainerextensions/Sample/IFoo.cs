using NullObjectGenerator;

namespace Amenone.VcontainerExtensions.Sample
{
    [InterfaceToNullObj]
    public interface IFoo
    {
        void Execute();
    }
}