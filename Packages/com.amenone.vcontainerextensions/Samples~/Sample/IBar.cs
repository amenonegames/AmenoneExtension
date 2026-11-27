using Amenone.VcontainerExtensions.Identifier;

namespace Amenone.VcontainerExtensions.Sample
{
    public interface IBar : INameable<string>
    {
        void Execute();
    }
}