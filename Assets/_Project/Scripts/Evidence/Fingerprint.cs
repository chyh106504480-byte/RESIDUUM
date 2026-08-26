using System.Collections;
using UnityEngine;

namespace Residuum.Evidence
{
    [DisallowMultipleComponent]
    public sealed class Fingerprint : MonoBehaviour
    {
        [Header("显示")]
        [Tooltip("组成指纹图案的渲染器；留空时只自动查找 Fingerprint 所在物体上的 Renderer，避免误隐藏门或开关模型。")]
        [SerializeField] private Renderer[] _renderers;

        [Header("生命周期")]
        [Tooltip("指纹激活后保持存在的时长，单位：秒。到期后会自动失效并隐藏。")]
        [SerializeField] private float _activeDuration = 60f;

        public bool IsActive { get; private set; }

        private Coroutine _expirationCoroutine;

        private void Awake()
        {
            ValidateSettings();
            CacheRenderersIfNeeded();
            SetVisible(false);

            if (!HasRenderer())
            {
                Debug.LogError("Fingerprint 未找到可控制的 Renderer，指纹无法显示。", this);
            }
        }

        private void OnDisable()
        {
            StopExpiration();
            IsActive = false;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            _expirationCoroutine = null;
            _renderers = null;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        public void Activate()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!enabled)
            {
                enabled = true;
            }

            StopExpiration();
            IsActive = true;
            SetVisible(false);

            if (!isActiveAndEnabled)
            {
                IsActive = false;
                Debug.LogWarning(
                    "Fingerprint 所在层级仍未启用，无法启动自动失效计时，本次激活已取消。",
                    this);
                return;
            }

            _expirationCoroutine = StartCoroutine(ExpireAfterDuration());
        }

        public void Reveal()
        {
            if (!IsActive)
            {
                return;
            }

            SetVisible(true);
        }

        public static Fingerprint SpawnAt(Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("Fingerprint.SpawnAt 收到空目标，无法激活预布置的指纹点位。");
                return null;
            }

            Fingerprint fingerprint = target.GetComponentInChildren<Fingerprint>(true);
            if (fingerprint == null)
            {
                Debug.LogWarning(
                    $"目标 {target.name} 及其子物体中未找到预布置的 Fingerprint。",
                    target);
                return null;
            }

            fingerprint.Activate();
            return fingerprint;
        }

        private IEnumerator ExpireAfterDuration()
        {
            yield return new WaitForSeconds(_activeDuration);

            _expirationCoroutine = null;
            IsActive = false;
            SetVisible(false);
        }

        private void StopExpiration()
        {
            if (_expirationCoroutine == null)
            {
                return;
            }

            StopCoroutine(_expirationCoroutine);
            _expirationCoroutine = null;
        }

        private void CacheRenderersIfNeeded()
        {
            if (_renderers != null && _renderers.Length > 0)
            {
                return;
            }

            _renderers = GetComponents<Renderer>();
        }

        private bool HasRenderer()
        {
            if (_renderers == null)
            {
                return false;
            }

            foreach (Renderer fingerprintRenderer in _renderers)
            {
                if (fingerprintRenderer != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetVisible(bool isVisible)
        {
            if (_renderers == null)
            {
                return;
            }

            foreach (Renderer fingerprintRenderer in _renderers)
            {
                if (fingerprintRenderer != null)
                {
                    fingerprintRenderer.enabled = isVisible;
                }
            }
        }

        private void ValidateSettings()
        {
            _activeDuration = Mathf.Max(Mathf.Epsilon, _activeDuration);
        }
    }
}
