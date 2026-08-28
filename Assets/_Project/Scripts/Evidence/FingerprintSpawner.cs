using UnityEngine;

namespace Residuum.Evidence
{
    /// <summary>
    /// 把 GhostAI.onFingerprintRequest 转发到静态的 Fingerprint.SpawnAt。
    /// UnityEvent 的持久监听挂不了静态方法，所以需要这一层实例方法。
    /// </summary>
    public sealed class FingerprintSpawner : MonoBehaviour
    {
        /// <summary>在 Inspector 里把 GhostAI.onFingerprintRequest 连到这里。</summary>
        public void Spawn(Transform target)
        {
            if (target == null)
            {
                return;
            }

            Fingerprint.SpawnAt(target);
        }
    }
}
