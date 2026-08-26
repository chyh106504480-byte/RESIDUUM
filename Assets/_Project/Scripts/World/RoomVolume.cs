using System;
using UnityEngine;
using UnityEngine.AI;

namespace Residuum.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RoomVolume : MonoBehaviour
    {
        [Header("房间配置")]
        [Tooltip("房间的中文显示名称，用于场景标注与日志。")]
        [SerializeField] private string _roomName = "未命名房间";

        [Tooltip("房间的唯一 ID，用于日志与调试。")]
        [SerializeField] private string _roomId;

        [Tooltip("是否允许本房间参与鬼房随机选择。")]
        [SerializeField] private bool _canBeGhostRoom = true;

        [Tooltip("用于判定玩家进入和离开房间的玩家层。")]
        [SerializeField] private LayerMask _playerLayerMask = ~0;

        [Header("导航采样")]
        [Tooltip("随机点吸附到 NavMesh 时允许搜索的最大距离，单位：米。")]
        [SerializeField] private float _navMeshSampleDistance = 2f;

        public bool HasPlayer { get; private set; }
        public event Action<RoomVolume, bool> OnPlayerPresenceChanged;

        public string RoomName => _roomName;
        public string RoomId => _roomId;
        public bool CanBeGhostRoom => _canBeGhostRoom;
        public Vector3 Center => _boxCollider != null
            ? _boxCollider.transform.TransformPoint(_boxCollider.center)
            : transform.position;

        private readonly System.Collections.Generic.HashSet<Collider> _playerColliders =
            new System.Collections.Generic.HashSet<Collider>();
        private BoxCollider _boxCollider;

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null)
            {
                Debug.LogError("RoomVolume 缺少必需的 BoxCollider，无法检测房间或生成随机点。", this);
                enabled = false;
                return;
            }

            if (!_boxCollider.isTrigger)
            {
                Debug.LogError("RoomVolume 的 BoxCollider 必须勾选 Is Trigger。", this);
            }

            ValidateSettings();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOnPlayerLayer(other))
            {
                return;
            }

            _playerColliders.Add(other);
            SetPlayerPresence(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsOnPlayerLayer(other))
            {
                return;
            }

            _playerColliders.Remove(other);
            if (_playerColliders.Count == 0)
            {
                SetPlayerPresence(false);
            }
        }

        private void OnDisable()
        {
            _playerColliders.Clear();
            SetPlayerPresence(false);
        }

        private void OnDestroy()
        {
            _playerColliders.Clear();
            OnPlayerPresenceChanged = null;
            _boxCollider = null;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        public Vector3 GetRandomPointInside()
        {
            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider>();
            }

            if (_boxCollider == null)
            {
                Debug.LogWarning("RoomVolume 缺少 BoxCollider，无法采样 NavMesh，已返回物体中心。", this);
                return transform.position;
            }

            Vector3 halfSize = _boxCollider.size * 0.5f;
            Vector3 localPoint = _boxCollider.center + new Vector3(
                UnityEngine.Random.Range(-halfSize.x, halfSize.x),
                -halfSize.y,
                UnityEngine.Random.Range(-halfSize.z, halfSize.z));
            Vector3 worldPoint = _boxCollider.transform.TransformPoint(localPoint);

            if (NavMesh.SamplePosition(
                    worldPoint,
                    out NavMeshHit hit,
                    _navMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                return hit.position;
            }

            Debug.LogWarning(
                $"房间“{_roomName}”（ID: {_roomId}）附近未找到已烘焙的 NavMesh，已返回 Collider 中心。",
                this);
            return Center;
        }

        private bool IsOnPlayerLayer(Collider other)
        {
            return other != null
                && (_playerLayerMask.value & (1 << other.gameObject.layer)) != 0;
        }

        private void SetPlayerPresence(bool hasPlayer)
        {
            if (HasPlayer == hasPlayer)
            {
                return;
            }

            HasPlayer = hasPlayer;
            OnPlayerPresenceChanged?.Invoke(this, hasPlayer);
        }

        private void ValidateSettings()
        {
            if (_navMeshSampleDistance < 0f)
            {
                Debug.LogWarning("RoomVolume 的 NavMesh 采样最大距离不能为负数，已按 0 处理。", this);
                _navMeshSampleDistance = 0f;
            }
        }

        private void OnDrawGizmos()
        {
            BoxCollider roomCollider = GetComponent<BoxCollider>();
            if (roomCollider == null)
            {
                return;
            }

            Gizmos.color = _canBeGhostRoom ? Color.cyan : Color.gray;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = roomCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(roomCollider.center, roomCollider.size);
            Gizmos.matrix = previousMatrix;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                roomCollider.transform.TransformPoint(roomCollider.center),
                _roomName);
#endif
        }
    }
}
