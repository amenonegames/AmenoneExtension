using Presenter.DialogueState;
using Presenter.InputSwitch;
using VContainer;
using VContainer.Unity;

namespace Presenter
{
    public static class SubscribingPresenterExtensions
    {
        public static void RegisterSubscribingPresenters(this IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<InitializeDialoguePresenter>();
            builder.RegisterEntryPoint<NodeStartPresenter>();
            builder.RegisterEntryPoint<NodeEndPresenter>();
            builder.RegisterEntryPoint<DialogueEventPresenter>();
            builder.RegisterEntryPoint<InputSwitchPresenter>();
        }
    }
}
