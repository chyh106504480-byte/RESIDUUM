using Residuum.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Residuum.Evidence
{
    [DisallowMultipleComponent]
    public sealed class GhostWritingBook : MonoBehaviour, IHoldable, IInteractable
    {
        private const string DisplayName = "鬼影书";
        private const string PickupPrompt = "[E] 捡回鬼影书";
        private const float MinimumColliderSize = 0.12f;

        private enum AppearanceMode
        {
            GameObjects,
            RendererMaterials
        }

        [Header("放置")]
        [Tooltip("用于放置射线的玩家相机；留空时会缓存主相机，仍找不到则改用书本自身位置与朝向。")]
        [SerializeField] private Camera _placementCamera;

        [Tooltip("书本目标位置相对射线起点的前向距离，单位：米。")]
        [SerializeField] private float _placementRayDistance = 1.5f;

        [Tooltip("书本目标位置相对射线起点的向下偏移，单位：米。")]
        [SerializeField] private float _downwardOffset = 2f;

        [Tooltip("可被放置射线识别为地面的物理层。")]
        [SerializeField] private LayerMask _groundLayers = Physics.DefaultRaycastLayers;

        [Tooltip("书本沿地面法线抬高的距离，避免模型轻微陷入地面，单位：米。")]
        [SerializeField] private float _surfaceOffset = 0.01f;

        [Tooltip("放到地上时启用的碰撞体，用来被交互射线打到。留空则在 Awake 按子物体 Renderer 的包围盒自动补一个 BoxCollider。")]
        [SerializeField] private Collider _placedCollider;

        [Tooltip("命中面法线向上的最小分量；数值越大，允许放置的地面越平缓。")]
        [SerializeField] private float _minimumGroundNormalY = 0.5f;

        [Header("书写判定")]
        [Tooltip("放置后两次书写判定之间的间隔，单位：秒。")]
        [SerializeField] private float _writingCheckInterval = 30f;

        [Tooltip("每次满足鬼种与鬼房条件后的书写成功率，范围 0 到 1。")]
        [SerializeField] private float _writingChance = 0.3f;

        [Header("书写外观")]
        [Tooltip("选择使用两个 GameObject 切换外观，或使用 Renderer 与两份材质切换外观。")]
        [SerializeField] private AppearanceMode _appearanceMode = AppearanceMode.GameObjects;

        [Tooltip("GameObject 外观模式下的未书写外观对象。")]
        [SerializeField] private GameObject _unwrittenAppearance;

        [Tooltip("GameObject 外观模式下的已书写外观对象。")]
        [SerializeField] private GameObject _writtenAppearance;

        [Tooltip("材质外观模式下需要切换材质的 Renderer。")]
        [SerializeField] private Renderer _writingRenderer;

        [Tooltip("材质外观模式下的未书写材质。")]
        [SerializeField] private Material _unwrittenMaterial;

        [Tooltip("材质外观模式下的已书写材质。")]
        [SerializeField] private Material _writtenMaterial;

        [Header("事件")]
        [Tooltip("书本成功放置后触发；供道具槽在 Inspector 中连接移除槽位的逻辑。")]
        public UnityEvent onPlaced = new UnityEvent();

        [Tooltip("书本被玩家捡回后触发；供道具槽在 Inspector 中连接收回槽位的逻辑。")]
        public UnityEvent onPickedUp = new UnityEvent();

        [Tooltip("书写首次出现时触发；供粒子与音效在 Inspector 中连接。")]
        public UnityEvent onWritingAppeared = new UnityEvent();

        public string ItemName => DisplayName;
        public string PromptText => PickupPrompt;
        public bool CanInteract => IsPlaced;
        public bool IsPlaced { get; private set; }
        public bool HasWriting { get; private set; }

        private WaitForSeconds _writingWait;
        private Coroutine _writingCoroutine;
        private Transform _heldParent;
        private Vector3 _heldLocalPosition;
        private Quaternion _heldLocalRotation;
        private Vector3 _heldLocalScale;
        private bool _isEquipped;
        private bool _hasHeldPose;
        private bool _ghostRoomWarningLogged;
        private bool _appearanceWarningLogged;

        private void Awake()
        {
            ValidateSettings();
            CachePlacementCamera();
            _writingWait = new WaitForSeconds(_writingCheckInterval);
            ApplyWritingAppearance(false);
            CreatePlacedColliderIfNeeded();
            SetPlacedColliderActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnRoundStart += HandleRoundStart;

            if (IsPlaced && !HasWriting)
            {
                StartWritingChecks();
            }
        }

        private void OnDisable()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
            _isEquipped = false;
            StopWritingChecks();
        }

        private void OnDestroy()
        {
            // 防御性退订，避免销毁顺序异常时给静态事件留下失效委托。
            GameEvents.OnRoundStart -= HandleRoundStart;
            StopAllCoroutines();
            _writingCoroutine = null;
            _writingWait = null;
            _placementCamera = null;
            _heldParent = null;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        public void OnEquip()
        {
            if (IsPlaced)
            {
                return;
            }

            _isEquipped = true;
            CachePlacementCamera();
            CacheHeldPose();
        }

        public void OnUnequip()
        {
            _isEquipped = false;

            // 放在地上的书要继续等鬼写字——玩家放下它就是为了去干别的。
            if (!IsPlaced)
            {
                StopWritingChecks();
            }
        }

        public void OnPrimaryUse()
        {
            if (!_isEquipped || IsPlaced)
            {
                return;
            }

            if (!TryFindGround(out RaycastHit groundHit))
            {
                Debug.LogWarning("GhostWritingBook 未在玩家前下方找到可放置地面，书本保持手持状态。", this);
                return;
            }

            transform.SetParent(null, true);
            Quaternion groundRotation = Quaternion.FromToRotation(transform.up, groundHit.normal)
                * transform.rotation;
            Vector3 groundPosition = groundHit.point + groundHit.normal * _surfaceOffset;
            transform.SetPositionAndRotation(groundPosition, groundRotation);

            IsPlaced = true;
            SetPlacedColliderActive(true);
            _isEquipped = false;
            StartWritingChecks();
            onPlaced?.Invoke();
        }

        public void Interact(GameObject interactor)
        {
            _ = interactor;

            if (!IsPlaced)
            {
                return;
            }

            IsPlaced = false;
            SetPlacedColliderActive(false);
            StopWritingChecks();
            RestoreHeldPose();
            onPickedUp?.Invoke();
        }

        private void HandleRoundStart()
        {
            StopWritingChecks();
            HasWriting = false;
            _ghostRoomWarningLogged = false;
            SetPlacedColliderActive(false);
            ApplyWritingAppearance(false);

            if (IsPlaced)
            {
                StartWritingChecks();
            }
        }

        private System.Collections.IEnumerator WritingRoutine()
        {
            while (IsPlaced && !HasWriting)
            {
                yield return _writingWait;

                if (!IsPlaced || HasWriting)
                {
                    break;
                }

                TryCreateWriting();
            }

            _writingCoroutine = null;
        }

        private void StartWritingChecks()
        {
            if (!isActiveAndEnabled || !IsPlaced || HasWriting || _writingCoroutine != null)
            {
                return;
            }

            _writingCoroutine = StartCoroutine(WritingRoutine());
        }

        private void StopWritingChecks()
        {
            if (_writingCoroutine == null)
            {
                return;
            }

            StopCoroutine(_writingCoroutine);
            _writingCoroutine = null;
        }

        private void TryCreateWriting()
        {
            float ghostRoomRadius = GameEvents.GhostRoomRadius;
            if (ghostRoomRadius <= 0f)
            {
                WarnGhostRoomMissingOnce();
                return;
            }

            // 3×3 推理表的硬守门条件：不持有鬼影书写证据的鬼永远不能生成书写。
            if (!GameEvents.GhostHasGhostWriting)
            {
                return;
            }

            float distanceToGhostRoom = Vector3.Distance(
                transform.position,
                GameEvents.GhostRoomCenter);
            if (distanceToGhostRoom > ghostRoomRadius)
            {
                return;
            }

            if (_writingChance <= 0f || Random.value > _writingChance)
            {
                return;
            }

            // 先锁定状态，避免 UnityEvent 或事件总线监听方重入时重复上报。
            HasWriting = true;
            ApplyWritingAppearance(true);
            onWritingAppeared?.Invoke();
            Debug.Log("GhostWritingBook 发现鬼影书写证据。", this);
            GameEvents.RaiseEvidenceFound(EvidenceType.GhostWriting);
        }

        private bool TryFindGround(out RaycastHit nearestGroundHit)
        {
            Transform source = _placementCamera != null
                ? _placementCamera.transform
                : transform;

            Vector3 horizontalForward = Vector3.ProjectOnPlane(source.forward, Vector3.up);
            if (horizontalForward.sqrMagnitude <= Mathf.Epsilon)
            {
                horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }

            if (horizontalForward.sqrMagnitude <= Mathf.Epsilon)
            {
                horizontalForward = Vector3.forward;
            }

            horizontalForward.Normalize();
            Vector3 castVector = horizontalForward * _placementRayDistance
                + Vector3.down * _downwardOffset;
            float castDistance = castVector.magnitude;
            RaycastHit[] hits = Physics.RaycastAll(
                source.position,
                castVector / castDistance,
                castDistance,
                _groundLayers,
                QueryTriggerInteraction.Ignore);

            nearestGroundHit = default;
            float nearestDistance = float.PositiveInfinity;
            bool foundGround = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || IsOwnCollider(hit.collider)
                    || hit.normal.y < _minimumGroundNormalY
                    || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestGroundHit = hit;
                nearestDistance = hit.distance;
                foundGround = true;
            }

            return foundGround;
        }

        private bool IsOwnCollider(Collider candidate)
        {
            Transform candidateTransform = candidate.transform;
            return candidateTransform == transform
                || candidateTransform.IsChildOf(transform)
                || transform.IsChildOf(candidateTransform);
        }

        private void CreatePlacedColliderIfNeeded()
        {
            if (_placedCollider != null)
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning(
                    "鬼影书没有可用于生成碰撞体的 Renderer，放置后将无法被捡回，请手动指定 _placedCollider",
                    this);
                return;
            }

            Bounds combinedBounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                combinedBounds.Encapsulate(renderers[index].bounds);
            }

            BoxCollider generatedCollider = gameObject.AddComponent<BoxCollider>();
            Vector3 lossyScale = transform.lossyScale;
            Vector3 worldSize = combinedBounds.size;
            generatedCollider.center = transform.InverseTransformPoint(combinedBounds.center);
            generatedCollider.size = new Vector3(
                GetLocalColliderSize(worldSize.x, lossyScale.x),
                GetLocalColliderSize(worldSize.y, lossyScale.y),
                GetLocalColliderSize(worldSize.z, lossyScale.z));
            generatedCollider.isTrigger = true;
            _placedCollider = generatedCollider;
        }

        private float GetLocalColliderSize(float worldSize, float scale)
        {
            float localSize = Mathf.Approximately(scale, 0f)
                ? worldSize
                : worldSize / Mathf.Abs(scale);
            return Mathf.Max(localSize, MinimumColliderSize);
        }

        private void SetPlacedColliderActive(bool active)
        {
            if (_placedCollider == null)
            {
                return;
            }

            _placedCollider.isTrigger = true;
            _placedCollider.enabled = active;
        }

        private void CachePlacementCamera()
        {
            if (_placementCamera == null)
            {
                _placementCamera = Camera.main;
            }
        }

        private void CacheHeldPose()
        {
            _heldParent = transform.parent;
            _heldLocalPosition = transform.localPosition;
            _heldLocalRotation = transform.localRotation;
            _heldLocalScale = transform.localScale;
            _hasHeldPose = true;
        }

        private void RestoreHeldPose()
        {
            if (!_hasHeldPose)
            {
                return;
            }

            transform.SetParent(_heldParent, false);
            transform.localPosition = _heldLocalPosition;
            transform.localRotation = _heldLocalRotation;
            transform.localScale = _heldLocalScale;
        }

        private void ApplyWritingAppearance(bool hasWriting)
        {
            if (_appearanceMode == AppearanceMode.GameObjects)
            {
                if (!CanUseGameObjectAppearance())
                {
                    WarnAppearanceMissingOnce();
                    return;
                }

                _unwrittenAppearance.SetActive(!hasWriting);
                _writtenAppearance.SetActive(hasWriting);
                return;
            }

            if (_writingRenderer == null || _unwrittenMaterial == null || _writtenMaterial == null)
            {
                WarnAppearanceMissingOnce();
                return;
            }

            _writingRenderer.sharedMaterial = hasWriting
                ? _writtenMaterial
                : _unwrittenMaterial;
        }

        private bool CanUseGameObjectAppearance()
        {
            if (_unwrittenAppearance == null || _writtenAppearance == null)
            {
                return false;
            }

            // 禁止把脚本根物体作为切换对象，否则切到另一外观时会禁用自身并中断生命周期。
            return _unwrittenAppearance != gameObject
                && _writtenAppearance != gameObject
                && _unwrittenAppearance != _writtenAppearance;
        }

        private void WarnGhostRoomMissingOnce()
        {
            if (_ghostRoomWarningLogged)
            {
                return;
            }

            _ghostRoomWarningLogged = true;
            Debug.LogWarning(
                "GhostWritingBook 无法判定书写：本回合鬼房尚未设定（GhostRoomRadius 必须大于 0）。",
                this);
        }

        private void WarnAppearanceMissingOnce()
        {
            if (_appearanceWarningLogged)
            {
                return;
            }

            _appearanceWarningLogged = true;
            Debug.LogWarning(
                "GhostWritingBook 选择的书写外观模式配置不完整；书写成功时仍会上报证据，但不会切换外观。",
                this);
        }

        private void ValidateSettings()
        {
            _placementRayDistance = Mathf.Max(0f, _placementRayDistance);
            _downwardOffset = Mathf.Max(Mathf.Epsilon, _downwardOffset);
            _surfaceOffset = Mathf.Max(0f, _surfaceOffset);
            _minimumGroundNormalY = Mathf.Clamp01(_minimumGroundNormalY);
            _writingCheckInterval = Mathf.Max(Mathf.Epsilon, _writingCheckInterval);
            _writingChance = Mathf.Clamp01(_writingChance);
        }
    }
}
