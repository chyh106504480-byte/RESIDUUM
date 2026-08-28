using UnityEngine;
using Residuum.Core;

namespace Residuum.UI
{
    /// <summary>
    /// 当前场景内的主菜单覆盖层。界面与开局回调均由 Inspector 注入。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuUI : MonoBehaviour
    {
        [Header("界面引用")]
        [SerializeField]
        [Tooltip("主菜单面板根节点；不能是本组件所在的 GameObject 或它的祖先。")]
        private GameObject _panelRoot;

        [SerializeField]
        [Tooltip("主菜单背景图；图片由 Inspector 设置，留空时不影响菜单功能。")]
        private UnityEngine.UI.Image _backgroundImage;

        [SerializeField]
        [Tooltip("显示主标题的文本。")]
        private TMPro.TextMeshProUGUI _titleLabel;

        [SerializeField]
        [Tooltip("显示副标题的文本。")]
        private TMPro.TextMeshProUGUI _subtitleLabel;

        [SerializeField]
        [Tooltip("开始游戏按钮。")]
        private UnityEngine.UI.Button _startButton;

        [SerializeField]
        [Tooltip("退出游戏按钮。")]
        private UnityEngine.UI.Button _quitButton;

        [Header("菜单文案")]
        [Tooltip("标题文字")]
        [SerializeField] private string _title = "残响";

        [Tooltip("副标题")]
        [SerializeField] private string _subtitle = "RESIDUUM";

        [Header("玩家控制")]
        [Tooltip("拖入玩家身上的 PlayerController 组件。菜单显示时禁用它")]
        [SerializeField] private MonoBehaviour _playerControllerBehaviour;

        [Tooltip("场景里的 ScreenFader。留空则不做过场，直接开始")]
        [SerializeField] private ScreenFader _screenFader;

        [Header("开局事件")]
        [Tooltip("在 Inspector 里连到 GameManager.StartRound")]
        public UnityEngine.Events.UnityEvent onStartRequested;

        private void Awake()
        {
            if (!ValidatePanelRoot())
            {
                enabled = false;
                return;
            }

            ApplyLabels();
            SetMenuVisible(true);
            SetPlayerControllerEnabled(false);
        }

        private void OnEnable()
        {
            GameEvents.OnRoundStart += HandleRoundStart;

            if (_startButton != null)
            {
                _startButton.onClick.AddListener(HandleStartClicked);
            }
            else
            {
                Debug.LogError("[MainMenuUI] 未注入开始游戏按钮。", this);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(HandleQuitClicked);
            }
            else
            {
                Debug.LogError("[MainMenuUI] 未注入退出游戏按钮。", this);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
            RemoveButtonListeners();
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常销毁顺序让静态事件保留失效委托。
            GameEvents.OnRoundStart -= HandleRoundStart;
            RemoveButtonListeners();
        }

        private bool ValidatePanelRoot()
        {
            if (_panelRoot == null)
            {
                Debug.LogError("[MainMenuUI] 未注入主菜单面板根节点，组件已禁用。", this);
                return false;
            }

            if (_panelRoot == gameObject || transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogError(
                    "[MainMenuUI] _panelRoot 不能是本组件所在的 GameObject 或它的祖先，组件已禁用。",
                    this);
                return false;
            }

            return true;
        }

        private void ApplyLabels()
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = _title;
            }
            else
            {
                Debug.LogError("[MainMenuUI] 未注入标题文本。", this);
            }

            if (_subtitleLabel != null)
            {
                _subtitleLabel.text = _subtitle;
            }
            else
            {
                Debug.LogError("[MainMenuUI] 未注入副标题文本。", this);
            }
        }

        private void HandleStartClicked()
        {
            if (_screenFader == null)
            {
                SetMenuVisible(false);
                SetPlayerControllerEnabled(true);
                onStartRequested?.Invoke();
                return;
            }

            _screenFader.FadeThrough(() =>
            {
                SetMenuVisible(false);
                SetPlayerControllerEnabled(true);
                onStartRequested?.Invoke();
            });
        }

        private void HandleQuitClicked()
        {
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void HandleRoundStart()
        {
            SetMenuVisible(false);
            SetPlayerControllerEnabled(true);
        }

        private void SetMenuVisible(bool isVisible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(isVisible);
            }
        }

        private void SetPlayerControllerEnabled(bool isEnabled)
        {
            if (_playerControllerBehaviour != null)
            {
                _playerControllerBehaviour.enabled = isEnabled;
            }
            else
            {
                Debug.LogError("[MainMenuUI] 未注入 PlayerController 组件。", this);
            }
        }

        private void RemoveButtonListeners()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(HandleStartClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(HandleQuitClicked);
            }
        }
    }
}
