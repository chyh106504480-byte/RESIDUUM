using System;
using System.Collections;
using UnityEngine;

namespace Residuum.UI
{
    /// <summary>
    /// 在黑屏期间执行回调的通用画面渐变组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFader : MonoBehaviour
    {
        [Tooltip("控制整层透明度与点击拦截的 CanvasGroup。")]
        [SerializeField] private CanvasGroup _fadeGroup;

        [Tooltip("覆盖全屏的黑色图片。")]
        [SerializeField] private UnityEngine.UI.Image _fadeImage;

        [Tooltip("黑屏期间显示提示文字的文本组件。")]
        [SerializeField] private TMPro.TextMeshProUGUI _messageLabel;

        [Tooltip("渐暗到全黑的秒数")]
        [Min(0f)] [SerializeField] private float _fadeOutDuration = 0.8f;

        [Tooltip("从全黑渐亮的秒数")]
        [Min(0f)] [SerializeField] private float _fadeInDuration = 1.2f;

        [Tooltip("全黑状态至少保持的秒数，让加载有个呼吸")]
        [Min(0f)] [SerializeField] private float _holdDuration = 0.3f;

        [Tooltip("黑屏时显示的文字")]
        [SerializeField] private string _message = "正在进入…";

        private Coroutine _fadeRoutine;
        private bool _isFading;

        private void Awake()
        {
            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = 0f;
                _fadeGroup.blocksRaycasts = false;
            }

            if (_messageLabel != null)
            {
                _messageLabel.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            _isFading = false;

            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = 0f;
                _fadeGroup.blocksRaycasts = false;
            }

            if (_messageLabel != null)
            {
                _messageLabel.gameObject.SetActive(false);
            }
        }

        /// <summary>渐暗到全黑 → 执行 onBlack → 渐亮。重复调用时忽略后来的。</summary>
        public void FadeThrough(Action onBlack)
        {
            if (_isFading)
            {
                return;
            }

            string missingFields = GetMissingFieldNames();
            if (!string.IsNullOrEmpty(missingFields))
            {
                Debug.LogWarning(
                    $"[ScreenFader] 以下引用未注入：{missingFields}。已跳过渐变并直接执行 onBlack。",
                    this);
                onBlack?.Invoke();
                return;
            }

            _isFading = true;
            _fadeRoutine = StartCoroutine(FadeThroughRoutine(onBlack));
        }

        private IEnumerator FadeThroughRoutine(Action onBlack)
        {
            _fadeGroup.blocksRaycasts = true;
            _messageLabel.text = _message;
            _messageLabel.gameObject.SetActive(true);

            yield return FadeAlpha(0f, 1f, _fadeOutDuration);

            onBlack?.Invoke();

            float holdElapsed = 0f;
            while (holdElapsed < _holdDuration)
            {
                holdElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _messageLabel.gameObject.SetActive(false);
            yield return FadeAlpha(1f, 0f, _fadeInDuration);

            _fadeGroup.blocksRaycasts = false;
            _fadeRoutine = null;
            _isFading = false;
        }

        private IEnumerator FadeAlpha(float startAlpha, float targetAlpha, float duration)
        {
            if (duration <= 0f)
            {
                _fadeGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeGroup.alpha = Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _fadeGroup.alpha = targetAlpha;
        }

        private string GetMissingFieldNames()
        {
            string missingFields = string.Empty;

            if (_fadeGroup == null)
            {
                missingFields = nameof(_fadeGroup);
            }

            if (_fadeImage == null)
            {
                missingFields = AppendFieldName(missingFields, nameof(_fadeImage));
            }

            if (_messageLabel == null)
            {
                missingFields = AppendFieldName(missingFields, nameof(_messageLabel));
            }

            return missingFields;
        }

        private static string AppendFieldName(string fieldNames, string fieldName)
        {
            return string.IsNullOrEmpty(fieldNames) ? fieldName : $"{fieldNames}、{fieldName}";
        }
    }
}
