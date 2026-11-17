using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Logic
{
    public class DefaultYarnCommands
    {
        [YarnCommand("wait")]
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
