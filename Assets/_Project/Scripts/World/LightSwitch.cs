using Residuum.Core;
using UnityEngine;

namespace Residuum.World
{
    /// <summary>
    /// 由玩家交互控制 Inspector 中指定的一组灯，并在新回合恢复开局状态。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LightSwitch : MonoBehaviour, IInteractable
    {
        [Tooltip("这个开关控制的灯。可以是一盏也可以是一屋子")]
        [SerializeField] private Light[] _lights;

        [Tooltip("开局时这组灯是否亮着")]
        [SerializeField] private bool _startsOn = true;

        [Tooltip("灯亮着时的提示")]
        [SerializeField] private string _turnOffPromptText = "[E] 关灯";

        [Tooltip("灯灭着时的提示")]
        [SerializeField] private string _turnOnPromptText = "[E] 开灯";

        public string PromptText => _isOn ? _turnOffPromptText : _turnOnPromptText;
        public bool CanInteract => HasValidLight();

        private bool _isOn;

        private void Awake()
        {
            if (!HasValidLight())
            {
                Debug.LogError($"LightSwitch：物体“{name}”未配置任何有效的 Light。", this);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnRoundStart += HandleRoundStart;
        }

        private void Start()
        {
            ApplyState(_startsOn);
        }

        private void OnDisable()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
        }

        private void OnDestroy()
        {
            // 防御性退订，避免异常销毁顺序让静态事件留下失效委托。
            GameEvents.OnRoundStart -= HandleRoundStart;
            _lights = null;
        }

        public void Interact(GameObject interactor)
        {
            _ = interactor;

            if (!CanInteract)
            {
                return;
            }

            ApplyState(!_isOn);
        }

        private void HandleRoundStart()
        {
            ApplyState(_startsOn);
        }

        private void ApplyState(bool isOn)
        {
            _isOn = isOn;
            if (_lights == null)
            {
                return;
            }

            for (int index = 0; index < _lights.Length; index++)
            {
                Light controlledLight = _lights[index];
                if (controlledLight != null)
                {
                    controlledLight.enabled = _isOn;
                }
            }
        }

        private bool HasValidLight()
        {
            if (_lights == null || _lights.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < _lights.Length; index++)
            {
                if (_lights[index] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
