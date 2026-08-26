using Residuum.Core;
using UnityEngine;

namespace Residuum.World
{
    [DisallowMultipleComponent]
    public sealed class RoomManager : MonoBehaviour
    {
        [Header("场景依赖")]
        [Tooltip("玩家的 Transform，由 Inspector 注入，用于读取当前位置。")]
        [SerializeField] private Transform _player;

        [Header("温度模拟")]
        [Tooltip("远离鬼房影响范围时的基础室温，单位：摄氏度。")]
        [SerializeField] private float _baseRoomTemperature = 12f;

        [Tooltip("鬼房中心位置的温度，单位：摄氏度。")]
        [SerializeField] private float _ghostRoomCenterTemperature = -2f;

        [Tooltip("鬼房温度影响的最大半径，单位：米。")]
        [SerializeField] private float _temperatureInfluenceRadius = 6f;

        [Tooltip("两次温度读取之间的间隔，单位：秒。")]
        [SerializeField] private float _temperatureUpdateInterval = 0.5f;

        [Tooltip("温度读数变化超过此值时才广播，单位：摄氏度。")]
        [SerializeField] private float _temperatureChangeThreshold = 0.1f;

        public static RoomManager Instance { get; private set; }
        public RoomVolume CurrentPlayerRoom { get; private set; }
        public RoomVolume GhostRoom { get; private set; }

        private RoomVolume[] _rooms;
        private WaitForSeconds _temperatureWait;
        private Coroutine _temperatureCoroutine;
        private float _lastBroadcastTemperature;
        private bool _hasBroadcastTemperature;
        private bool _isDuplicate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                _isDuplicate = true;
                Debug.LogError("场景中存在重复的 RoomManager，已销毁当前重复实例。", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ValidateSettings();
            _rooms = FindObjectsByType<RoomVolume>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            if (_isDuplicate)
            {
                return;
            }

            SubscribeToRooms();
            CurrentPlayerRoom = FindOccupiedRoom();
            _hasBroadcastTemperature = false;
            _temperatureWait = new WaitForSeconds(_temperatureUpdateInterval);

            if (_player == null)
            {
                Debug.LogError("RoomManager 未在 Inspector 注入玩家 Transform，无法广播温度。", this);
                return;
            }

            _temperatureCoroutine = StartCoroutine(BroadcastTemperatureRoutine());
        }

        private void OnDisable()
        {
            UnsubscribeFromRooms();

            if (_temperatureCoroutine != null)
            {
                StopCoroutine(_temperatureCoroutine);
                _temperatureCoroutine = null;
            }

            CurrentPlayerRoom = null;
            _temperatureWait = null;
            _hasBroadcastTemperature = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _rooms = null;
            _player = null;
            GhostRoom = null;
            CurrentPlayerRoom = null;
            _temperatureWait = null;
            _temperatureCoroutine = null;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        public void SelectGhostRoom()
        {
            GhostRoom = null;
            int candidateCount = 0;

            if (_rooms != null)
            {
                foreach (RoomVolume room in _rooms)
                {
                    if (room != null && room.CanBeGhostRoom)
                    {
                        candidateCount++;
                    }
                }
            }

            if (candidateCount == 0)
            {
                Debug.LogError("RoomManager 找不到可作为鬼房候选的 RoomVolume，GhostRoom 保持为空。", this);
                return;
            }

            int selectedCandidateIndex = Random.Range(0, candidateCount);
            foreach (RoomVolume room in _rooms)
            {
                if (room == null || !room.CanBeGhostRoom)
                {
                    continue;
                }

                if (selectedCandidateIndex == 0)
                {
                    GhostRoom = room;
                    break;
                }

                selectedCandidateIndex--;
            }

            Debug.Log(
                $"已选择鬼房：{GhostRoom.RoomName}（ID: {GhostRoom.RoomId}）。",
                GhostRoom);
        }

        public float GetTemperatureAt(Vector3 pos)
        {
            if (GhostRoom == null || _temperatureInfluenceRadius <= 0f)
            {
                return _baseRoomTemperature;
            }

            float distanceToGhostRoom = Vector3.Distance(pos, GhostRoom.Center);
            if (distanceToGhostRoom >= _temperatureInfluenceRadius)
            {
                return _baseRoomTemperature;
            }

            float normalizedDistance = distanceToGhostRoom / _temperatureInfluenceRadius;
            return Mathf.Lerp(
                _ghostRoomCenterTemperature,
                _baseRoomTemperature,
                normalizedDistance);
        }

        private System.Collections.IEnumerator BroadcastTemperatureRoutine()
        {
            while (true)
            {
                float temperature = GetTemperatureAt(_player.position);
                if (!_hasBroadcastTemperature
                    || Mathf.Abs(temperature - _lastBroadcastTemperature) > _temperatureChangeThreshold)
                {
                    _lastBroadcastTemperature = temperature;
                    _hasBroadcastTemperature = true;
                    GameEvents.RaisePlayerTemperatureChanged(temperature);
                }

                yield return _temperatureWait;
            }
        }

        private void HandlePlayerPresenceChanged(RoomVolume room, bool hasPlayer)
        {
            if (hasPlayer)
            {
                CurrentPlayerRoom = room;
                return;
            }

            if (CurrentPlayerRoom == room)
            {
                CurrentPlayerRoom = FindOccupiedRoom();
            }
        }

        private RoomVolume FindOccupiedRoom()
        {
            if (_rooms == null)
            {
                return null;
            }

            foreach (RoomVolume room in _rooms)
            {
                if (room != null && room.HasPlayer)
                {
                    return room;
                }
            }

            return null;
        }

        private void SubscribeToRooms()
        {
            if (_rooms == null)
            {
                return;
            }

            foreach (RoomVolume room in _rooms)
            {
                if (room != null)
                {
                    room.OnPlayerPresenceChanged += HandlePlayerPresenceChanged;
                }
            }
        }

        private void UnsubscribeFromRooms()
        {
            if (_rooms == null)
            {
                return;
            }

            foreach (RoomVolume room in _rooms)
            {
                if (room != null)
                {
                    room.OnPlayerPresenceChanged -= HandlePlayerPresenceChanged;
                }
            }
        }

        private void ValidateSettings()
        {
            if (_temperatureInfluenceRadius < 0f)
            {
                Debug.LogWarning("RoomManager 的温度影响半径不能为负数，已按 0 处理。", this);
                _temperatureInfluenceRadius = 0f;
            }

            if (_temperatureUpdateInterval <= 0f)
            {
                Debug.LogWarning("RoomManager 的温度更新间隔必须大于 0，已按最小正数处理。", this);
                _temperatureUpdateInterval = Mathf.Epsilon;
            }

            if (_temperatureChangeThreshold < 0f)
            {
                Debug.LogWarning("RoomManager 的温度广播阈值不能为负数，已按 0 处理。", this);
                _temperatureChangeThreshold = 0f;
            }
        }
    }
}
