using UnityEngine;

namespace Residuum.World
{
    [DisallowMultipleComponent]
    public sealed class Door : MonoBehaviour, IInteractable
    {
        [Header("门的运动")]
        [Tooltip("相对关闭状态绕铰链 Y 轴旋转的开门角度，单位：度。可用负值选择另一侧。")]
        [SerializeField] private float _openAngle = 90f;

        [Tooltip("门完成一次开或关所需的时间，单位：秒。设为 0 将立即到达目标角度。")]
        [SerializeField] private float _openCloseDuration = 0.6f;

        [Tooltip("门的铰链 Transform。留空时使用当前 Door 所在的 Transform。")]
        [SerializeField] private Transform _hinge;

        public bool IsOpen { get; private set; }
        public string PromptText => IsOpen ? "[E] 关门" : "[E] 开门";
        public bool CanInteract => true;

        private UnityEngine.AI.NavMeshObstacle _navMeshObstacle;
        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private Quaternion _transitionStartRotation;
        private Quaternion _transitionTargetRotation;
        private float _transitionElapsed;
        private float _transitionDuration;
        private bool _isTransitioning;
        private bool _targetOpen;

        private void Awake()
        {
            if (_hinge == null)
            {
                _hinge = transform;
                Debug.LogWarning("Door 未指定铰链 Transform，已使用自身 Transform。", this);
            }

            _navMeshObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            _closedRotation = _hinge.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, _openAngle, 0f);
            ValidateSettings();
            _targetOpen = false;
            IsOpen = false;
            _isTransitioning = false;
            UpdateNavMeshObstacle();
        }

        private void Update()
        {
            if (!_isTransitioning || _hinge == null)
            {
                return;
            }

            _transitionElapsed += Time.deltaTime;
            float normalizedTime = _transitionDuration > Mathf.Epsilon
                ? Mathf.Clamp01(_transitionElapsed / _transitionDuration)
                : 1f;

            _hinge.localRotation = Quaternion.Slerp(
                _transitionStartRotation,
                _transitionTargetRotation,
                normalizedTime);

            if (normalizedTime >= 1f)
            {
                _hinge.localRotation = _transitionTargetRotation;
                _isTransitioning = false;
            }
        }

        public void Interact(GameObject interactor)
        {
            _ = interactor;

            if (!CanInteract || _hinge == null)
            {
                return;
            }

            _targetOpen = !_targetOpen;
            IsOpen = _targetOpen;
            _transitionStartRotation = _hinge.localRotation;
            _transitionTargetRotation = _targetOpen ? _openRotation : _closedRotation;
            float remainingAngle = Quaternion.Angle(_transitionStartRotation, _transitionTargetRotation);
            float fullTransitionAngle = Mathf.Abs(_openAngle);
            float remainingRatio = fullTransitionAngle > Mathf.Epsilon
                ? Mathf.Clamp01(remainingAngle / fullTransitionAngle)
                : 0f;
            _transitionDuration = _openCloseDuration * remainingRatio;
            _transitionElapsed = 0f;
            _isTransitioning = true;
            UpdateNavMeshObstacle();
        }

        private void UpdateNavMeshObstacle()
        {
            if (_navMeshObstacle != null)
            {
                _navMeshObstacle.carving = !IsOpen;
            }
        }

        private void ValidateSettings()
        {
            if (_openCloseDuration < 0f)
            {
                Debug.LogWarning("Door 的开关耗时不能为负数，已按 0 处理。", this);
                _openCloseDuration = 0f;
            }
        }

        private void OnDestroy()
        {
            _hinge = null;
            _navMeshObstacle = null;
            _isTransitioning = false;
        }
    }
}
