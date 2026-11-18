using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Logic
{
    public class DefaultYarnCommands
    {
        public void RegisterTo(CommandDispatcher dispatcher)
        {
            dispatcher.RegisterCommandsFromInstance(this);
            // wait は [YarnCommand] にしない。Yarn Spinner のソースジェネレータが名前の同じ属性を拾い、CancellationToken の引数を Yarn の型でないと見て、言語サーバー用の命令の一覧（ysls.json）で containsErrors にするため
            dispatcher.RegisterCommandHandler<float, CancellationToken>("wait", Wait);
        }

        public async UniTask Wait(float seconds, CancellationToken token = default)
        {
            await UniTask.Delay((int)(seconds * 1000), cancellationToken: token);
        }

        [YarnCommand("log")]
        public void Log(string message)
        {
            Debug.Log($"[Yarn Command] {message}");
        }

        [YarnCommand("set_active")]
        public void SetActive(GameObject target, bool active)
        {
            target.SetActive(active);
        }

        [YarnCommand("destroy")]
        public void DestroyObject(GameObject target)
        {
            Object.Destroy(target);
        }
    }
}
