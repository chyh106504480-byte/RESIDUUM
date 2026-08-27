using UnityEngine;
using Residuum.Core;

namespace Residuum.UI
{
    /// <summary>
    /// 回合结算界面。通过事件总线接收完整结算信息，并由 Inspector 负责界面与重开逻辑连线。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultScreenUI : MonoBehaviour
    {
        [Header("界面引用")]
        [SerializeField]
        [Tooltip("结算面板根节点；开局隐藏，且不能与本组件位于同一个 GameObject。")]
        private GameObject _panelRoot;

        [SerializeField]
        [Tooltip("显示结算等级字母（S/A/C/F）的文本。")]
        private TMPro.TextMeshProUGUI _gradeLabel;

        [SerializeField]
        [Tooltip("显示结算一句话结论的文本。")]
        private TMPro.TextMeshProUGUI _titleLabel;

        [SerializeField]
        [Tooltip("显示真凶、判定、证据与用时明细的多行文本。")]
        private TMPro.TextMeshProUGUI _detailLabel;

        [SerializeField]
        [Tooltip("请求重新开始本局的按钮。")]
        private UnityEngine.UI.Button _restartButton;

        [Header("玩家控制")]
        [SerializeField]
        [Tooltip("拖入玩家身上的 PlayerController 组件。结算面板弹出时会禁用它，从而同时停掉移动、视角与光标锁定。")]
        private MonoBehaviour _playerControllerBehaviour;

        [Header("评级文案")]
        [SerializeField]
        [Tooltip("Perfect 结算时显示的标题。")]
        private string _perfectTitle = "完美收工";

        [SerializeField]
        [Tooltip("Success 结算时显示的标题。")]
        private string _successTitle = "判定正确";

        [SerializeField]
        [Tooltip("Survived 结算时显示的标题。")]
        private string _survivedTitle = "活着回来了，但判错了";

        [SerializeField]
        [Tooltip("Died 结算时显示的标题。")]
        private string _diedTitle = "你没能离开那栋房子";

        [Header("评级颜色")]
        [SerializeField]
        [Tooltip("Perfect 结算时等级与标题使用的颜色。")]
        private Color _perfectColor = new Color(1f, 0.85f, 0.3f);

        [SerializeField]
        [Tooltip("Success 结算时等级与标题使用的颜色。")]
        private Color _successColor = new Color(0.4f, 0.85f, 0.45f);

        [SerializeField]
        [Tooltip("Survived 结算时等级与标题使用的颜色。")]
        private Color _survivedColor = new Color(0.75f, 0.75f, 0.75f);

        [SerializeField]
        [Tooltip("Died 结算时等级与标题使用的颜色。")]
        private Color _diedColor = new Color(0.7f, 0.15f, 0.15f);

        [Header("明细文案")]
        [SerializeField]
        [Tooltip("结算明细格式；参数依次为真凶、玩家判定、证据数、用时。")]
        [TextArea(3, 4)]
        private string _detailTemplate = "真凶：{0}\n你的判定：{1}\n证据：{2} / 2\n用时：{3}";

        [SerializeField]
        [Tooltip("玩家没有提交鬼种判定时显示的文案。")]
        private string _noGuessText = "未提交判定";

        [Header("重开事件")]
        [SerializeField]
        [Tooltip("点击再来一局后触发；请在 Inspector 连到 GameManager.StartRound。")]
        public UnityEngine.Events.UnityEvent onRestartRequested =
            new UnityEngine.Events.UnityEvent();

        private void Awake()
        {
            if (_panelRoot == gameObject)
            {
                Debug.LogError(
                    "[ResultScreenUI] _panelRoot 不能是本组件所在的 GameObject，否则组件会失去接收结算事件的能力。",
                    this);
                return;
            }

            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundSummary += HandleRoundSummary;

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(HandleRestartClicked);
            }
            else
            {
                Debug.LogError("[ResultScreenUI] 未注入再来一局按钮。", this);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundSummary -= HandleRoundSummary;

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常销毁顺序让静态事件保留失效委托。
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundSummary -= HandleRoundSummary;

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
        }

        private void HandleRoundStart()
        {
            SetPanelVisible(false);
            SetPlayerControllerEnabled(true);
        }

        private void HandleRoundSummary(RoundSummary summary)
        {
            if (_panelRoot == null)
            {
                Debug.LogError("[ResultScreenUI] 未注入结算面板根节点，无法显示结算。", this);
                return;
            }

            if (_panelRoot == gameObject)
            {
                Debug.LogError(
                    "[ResultScreenUI] _panelRoot 不能是本组件所在的 GameObject，无法显示结算。",
                    this);
                return;
            }

            ApplyResultPresentation(summary);
            _panelRoot.SetActive(true);
            SetPlayerControllerEnabled(false);
        }

        private void HandleRestartClicked()
        {
            SetPanelVisible(false);
            SetPlayerControllerEnabled(true);
            onRestartRequested?.Invoke();
        }

        private void ApplyResultPresentation(RoundSummary summary)
        {
            string grade;
            string title;
            Color color;

            switch (summary.Result)
            {
                case RoundResult.Perfect:
                    grade = "S";
                    title = _perfectTitle;
                    color = _perfectColor;
                    break;

                case RoundResult.Success:
                    grade = "A";
                    title = _successTitle;
                    color = _successColor;
                    break;

                case RoundResult.Survived:
                    grade = "C";
                    title = _survivedTitle;
                    color = _survivedColor;
                    break;

                case RoundResult.Died:
                    grade = "F";
                    title = _diedTitle;
                    color = _diedColor;
                    break;

                default:
                    Debug.LogError($"[ResultScreenUI] 未处理的结算结果：{summary.Result}。", this);
                    return;
            }

            if (_gradeLabel != null)
            {
                _gradeLabel.text = grade;
                _gradeLabel.color = color;
            }
            else
            {
                Debug.LogError("[ResultScreenUI] 未注入等级文本。", this);
            }

            if (_titleLabel != null)
            {
                _titleLabel.text = title;
                _titleLabel.color = color;
            }
            else
            {
                Debug.LogError("[ResultScreenUI] 未注入标题文本。", this);
            }

            if (_detailLabel != null)
            {
                string guessedGhostName = summary.HasGuess ? summary.GuessedGhostName : _noGuessText;
                string elapsedTime = FormatElapsedTime(summary.ElapsedSeconds);
                _detailLabel.text = string.Format(
                    _detailTemplate,
                    summary.ActualGhostName,
                    guessedGhostName,
                    summary.FoundEvidenceCount,
                    elapsedTime);
            }
            else
            {
                Debug.LogError("[ResultScreenUI] 未注入明细文本。", this);
            }
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (_panelRoot == null)
            {
                Debug.LogError("[ResultScreenUI] 未注入结算面板根节点。", this);
                return;
            }

            if (_panelRoot == gameObject)
            {
                Debug.LogError(
                    "[ResultScreenUI] _panelRoot 不能是本组件所在的 GameObject。",
                    this);
                return;
            }

            _panelRoot.SetActive(isVisible);
        }

        private void SetPlayerControllerEnabled(bool isEnabled)
        {
            if (_playerControllerBehaviour != null)
            {
                _playerControllerBehaviour.enabled = isEnabled;
            }
        }

        private static string FormatElapsedTime(float elapsedSeconds)
        {
            System.TimeSpan elapsedTime = System.TimeSpan.FromSeconds(Mathf.Max(elapsedSeconds, 0f));
            return string.Format("{0}:{1:00}", (int)elapsedTime.TotalMinutes, elapsedTime.Seconds);
        }
    }
}
