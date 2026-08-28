using UnityEngine;
using Residuum.Core;

namespace Residuum.UI
{
    /// <summary>
    /// 回合中的暂停菜单。禁用玩家控制后提供继续游戏或重载当前场景两个操作。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [Tooltip("暂停菜单的面板根节点。不能是本组件所在对象或它的祖先。")]
        [SerializeField] private GameObject _panelRoot;

        [Tooltip("关闭暂停菜单并继续游戏的按钮。")]
        [SerializeField] private UnityEngine.UI.Button _resumeButton;

        [Tooltip("重载当前场景并返回主菜单的按钮。")]
        [SerializeField] private UnityEngine.UI.Button _quitToMenuButton;

        [Tooltip("呼出/关闭菜单的按键")]
        [SerializeField] private UnityEngine.InputSystem.Key _toggleKey =
            UnityEngine.InputSystem.Key.Escape;

        [Tooltip("拖入玩家身上的 PlayerController。菜单打开时禁用它")]
        [SerializeField] private MonoBehaviour _playerControllerBehaviour;

        [Tooltip("可选：返回主菜单前做一次渐暗过场")]
        [SerializeField] private ScreenFader _screenFader;

        private bool _isMenuOpen;
        private bool _isRoundActive;
        private bool _hasValidPanelRoot;

        private void Awake()
        {
            if (_panelRoot == null)
            {
                Debug.LogError("[PauseMenuUI] 未注入暂停菜单面板根节点。", this);
                enabled = false;
                return;
            }

            if (transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogError(
                    "[PauseMenuUI] _panelRoot 不能是本组件所在的 GameObject 或它的祖先，否则隐藏面板会同时禁用 PauseMenuUI。",
                    this);
                enabled = false;
                return;
            }

            _hasValidPanelRoot = true;
            _isMenuOpen = false;
            _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundEnd += HandleRoundEnd;

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(HandleResumeClicked);
            }
            else
            {
                Debug.LogError("[PauseMenuUI] 未注入继续游戏按钮。", this);
            }

            if (_quitToMenuButton != null)
            {
                _quitToMenuButton.onClick.AddListener(HandleQuitToMenuClicked);
            }
            else
            {
                Debug.LogError("[PauseMenuUI] 未注入返回主菜单按钮。", this);
            }
        }

        private void Update()
        {
            if (!_isRoundActive)
            {
                return;
            }

            UnityEngine.InputSystem.Keyboard keyboard =
                UnityEngine.InputSystem.Keyboard.current;

            if (keyboard == null ||
                _toggleKey == UnityEngine.InputSystem.Key.None ||
                !keyboard[_toggleKey].wasPressedThisFrame)
            {
                return;
            }

            SetMenuOpen(!_isMenuOpen);
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            RemoveButtonListeners();
            CloseMenuIfNeeded();
        }

        private void OnDestroy()
        {
            // 防御性清理，避免异常销毁顺序让静态事件或按钮保留失效委托。
            UnsubscribeFromEvents();
            RemoveButtonListeners();
            CloseMenuIfNeeded();
        }

        private void HandleRoundStart()
        {
            _isRoundActive = true;
            SetMenuOpen(false);
        }

        private void HandleRoundEnd(RoundResult result)
        {
            _isRoundActive = false;
            SetMenuOpen(false);
        }

        private void HandleResumeClicked()
        {
            SetMenuOpen(false);
        }

        private void HandleQuitToMenuClicked()
        {
            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            if (!Application.CanStreamedLevelBeLoaded(scene.name))
            {
                Debug.LogError(
                    $"[PauseMenuUI] 场景 {scene.name} 不在 Build Settings 中，无法返回主菜单，请到 File > Build Profiles 把它加进去",
                    this);
                return;
            }

            // 场景重载前先恢复玩家控制，让新场景从正确的光标状态开始。
            SetMenuOpen(false);

            if (_screenFader != null)
            {
                _screenFader.FadeThrough(() => ReloadScene(scene.name));
                return;
            }

            ReloadScene(scene.name);
        }

        private void SetMenuOpen(bool isOpen)
        {
            if (!_hasValidPanelRoot)
            {
                return;
            }

            bool wasOpen = _isMenuOpen || _panelRoot.activeSelf;
            _isMenuOpen = isOpen;
            _panelRoot.SetActive(isOpen);

            if (isOpen)
            {
                SetPlayerControllerEnabled(false);
            }
            else if (wasOpen)
            {
                SetPlayerControllerEnabled(true);
            }
        }

        private void SetPlayerControllerEnabled(bool isEnabled)
        {
            if (_playerControllerBehaviour != null)
            {
                _playerControllerBehaviour.enabled = isEnabled;
            }
        }

        private void CloseMenuIfNeeded()
        {
            if (!_hasValidPanelRoot)
            {
                return;
            }

            bool panelIsActive = _panelRoot != null && _panelRoot.activeSelf;
            if (!_isMenuOpen && !panelIsActive)
            {
                return;
            }

            _isMenuOpen = false;
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            SetPlayerControllerEnabled(true);
        }

        private static void ReloadScene(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;
        }

        private void RemoveButtonListeners()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }

            if (_quitToMenuButton != null)
            {
                _quitToMenuButton.onClick.RemoveListener(HandleQuitToMenuClicked);
            }
        }
    }
}
