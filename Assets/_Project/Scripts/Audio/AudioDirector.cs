using UnityEngine;
using Residuum.Core;
using Residuum.Evidence;

namespace Residuum.Audio
{
    public sealed class AudioDirector : MonoBehaviour
    {
        [Header("场景引用")]
        [Tooltip("拖入场景里的 Ghost 物体")]
        [SerializeField] private Transform _ghostTransform;

        [Tooltip("拖入场景里的 Player 物体")]
        [SerializeField] private Transform _playerTransform;

        [Header("心跳")]
        [Tooltip("心跳音源。建议 2D，Spatial Blend 设为 0")]
        [SerializeField] private AudioSource _heartbeatSource;

        [Tooltip("心跳音效")]
        [SerializeField] private AudioClip _heartbeatClip;

        [Tooltip("鬼在这个距离以内心跳达到最强，单位：米")]
        [Min(0.1f)]
        [SerializeField] private float _heartbeatNearDistance = 3f;

        [Tooltip("鬼超过这个距离心跳完全停止，单位：米")]
        [Min(0.1f)]
        [SerializeField] private float _heartbeatFarDistance = 15f;

        [Tooltip("最近距离时的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _heartbeatMaxVolume = 1f;

        [Tooltip("最近距离时的播放速率")]
        [SerializeField] private float _heartbeatMaxPitch = 1.6f;

        [Tooltip("最远距离时的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _heartbeatMinVolume = 0f;

        [Tooltip("最远距离时的播放速率")]
        [SerializeField] private float _heartbeatMinPitch = 0.8f;

        [Tooltip("两次距离采样之间的间隔秒数")]
        [Min(0.02f)]
        [SerializeField] private float _heartbeatUpdateInterval = 0.1f;

        [Tooltip("音量与音调向目标值靠拢的速度，避免突变")]
        [Min(0.01f)]
        [SerializeField] private float _heartbeatLerpSpeed = 3f;

        [Tooltip("鬼现身后心跳持续的秒数")]
        [Min(0f)]
        [SerializeField] private float _manifestHeartbeatDuration = 4f;

        [Header("氛围音效")]
        [Tooltip("氛围音效音源。建议 2D，Spatial Blend 设为 0")]
        [SerializeField] private AudioSource _oneShotSource;

        [Header("哈气声")]
        [Tooltip("巡逻阶段随机或鬼现身结束后播放的哈气声，可留空")]
        [SerializeField] private AudioClip _ghostManifestClip;

        [Tooltip("哈气声的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _ghostManifestVolume = 1f;

        [Tooltip("巡逻阶段随机哈气声的判定间隔秒数")]
        [Min(1f)]
        [SerializeField] private float _ambientBreathCheckInterval = 25f;

        [Tooltip("巡逻阶段每次判定触发的概率，应当很小")]
        [Range(0f, 1f)]
        [SerializeField] private float _ambientBreathChance = 0.15f;

        [Tooltip("鬼现身结束后触发哈气声的概率，应当中等")]
        [Range(0f, 1f)]
        [SerializeField] private float _postManifestBreathChance = 0.5f;

        [Tooltip("鬼现身开始到现身结束的秒数，应与 GhostAI 的显形秒数一致")]
        [Min(0f)]
        [SerializeField] private float _manifestDuration = 2f;

        [Tooltip("猎杀开始时播放的音效，可留空")]
        [SerializeField] private AudioClip _huntStartClip;

        [Tooltip("猎杀开始音效的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _huntStartVolume = 1f;

        [Tooltip("猎杀结束时播放的音效，可留空")]
        [SerializeField] private AudioClip _huntEndClip;

        [Tooltip("猎杀结束音效的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _huntEndVolume = 1f;

        [Tooltip("停电时播放的音效，可留空")]
        [SerializeField] private AudioClip _blackoutClip;

        [Tooltip("停电音效的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _blackoutVolume = 1f;

        [Tooltip("发现证据时播放的音效，可留空")]
        [SerializeField] private AudioClip _evidenceFoundClip;

        [Tooltip("发现证据音效的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _evidenceFoundVolume = 1f;

        [Tooltip("玩家被鬼抓住时播放的音效，可留空")]
        [SerializeField] private AudioClip _playerCaughtClip;

        [Tooltip("玩家被抓音效的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _playerCaughtVolume = 1f;

        [Tooltip("理智首次进入危险区时播放的音效，可留空")]
        [SerializeField] private AudioClip _sanityCriticalClip;

        [Tooltip("理智危险音效的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _sanityCriticalVolume = 1f;

        [Header("喘气声")]
        [Tooltip("玩家冲刺体力耗尽时播放的喘气声，可留空")]
        [SerializeField] private AudioClip _staminaDepletedClip;

        [Tooltip("冲刺体力耗尽喘气声的音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _staminaDepletedVolume = 1f;

        private bool _heartbeatAvailable;
        private bool _oneShotAvailable;
        private bool _isRoundActive;
        private bool _isHunting;
        private bool _isManifesting;
        private bool _postManifestBreathPending;
        private float _ambientBreathTimer;
        private float _postManifestBreathRemaining;
        private float _manifestHeartbeatRemaining;
        private float _heartbeatSampleTimer;
        private float _targetHeartbeatVolume;
        private float _targetHeartbeatPitch;

        private void Awake()
        {
            ValidateReferences();
            ResetHeartbeatState();
            ResetBreathState(false);
        }

        private void OnEnable()
        {
            GameEvents.OnRoundStart += HandleRoundStart;
            GameEvents.OnRoundEnd += HandleRoundEnd;
            GameEvents.OnGhostEvent += HandleGhostEvent;
            GameEvents.OnHuntStart += HandleHuntStart;
            GameEvents.OnHuntEnd += HandleHuntEnd;
            GameEvents.OnBlackoutChanged += HandleBlackoutChanged;
            GameEvents.OnEvidenceFound += HandleEvidenceFound;
            GameEvents.OnPlayerCaught += HandlePlayerCaught;
            GameEvents.OnSanityCritical += HandleSanityCritical;
            GameEvents.OnStaminaDepleted += HandleStaminaDepleted;
        }

        private void OnDisable()
        {
            GameEvents.OnRoundStart -= HandleRoundStart;
            GameEvents.OnRoundEnd -= HandleRoundEnd;
            GameEvents.OnGhostEvent -= HandleGhostEvent;
            GameEvents.OnHuntStart -= HandleHuntStart;
            GameEvents.OnHuntEnd -= HandleHuntEnd;
            GameEvents.OnBlackoutChanged -= HandleBlackoutChanged;
            GameEvents.OnEvidenceFound -= HandleEvidenceFound;
            GameEvents.OnPlayerCaught -= HandlePlayerCaught;
            GameEvents.OnSanityCritical -= HandleSanityCritical;
            GameEvents.OnStaminaDepleted -= HandleStaminaDepleted;

            ResetHeartbeatState();
            ResetBreathState(false);
        }

        private void Update()
        {
            UpdateBreathState();

            if (!_heartbeatAvailable)
            {
                return;
            }

            if (_manifestHeartbeatRemaining > 0f)
            {
                _manifestHeartbeatRemaining = Mathf.Max(
                    0f,
                    _manifestHeartbeatRemaining - Time.deltaTime);
            }

            bool heartbeatRequested = _isHunting || _manifestHeartbeatRemaining > 0f;
            if (heartbeatRequested && _heartbeatClip != null)
            {
                _heartbeatSampleTimer -= Time.deltaTime;
                if (_heartbeatSampleTimer <= 0f)
                {
                    SampleHeartbeatTargets();
                    _heartbeatSampleTimer = _heartbeatUpdateInterval;
                }
            }
            else
            {
                _targetHeartbeatVolume = 0f;
                _targetHeartbeatPitch = _heartbeatMinPitch;
            }

            if (heartbeatRequested
                && _heartbeatClip != null
                && !Mathf.Approximately(_targetHeartbeatVolume, 0f))
            {
                EnsureHeartbeatPlaying();
            }

            SmoothHeartbeat(heartbeatRequested && _heartbeatClip != null);
        }

        private void ValidateReferences()
        {
            if (_heartbeatSource == null)
            {
                Debug.LogWarning("AudioDirector：_heartbeatSource 未设置，心跳功能将跳过。", this);
            }

            if (_ghostTransform == null)
            {
                Debug.LogWarning("AudioDirector：_ghostTransform 未设置，心跳功能将跳过。", this);
            }

            if (_playerTransform == null)
            {
                Debug.LogWarning("AudioDirector：_playerTransform 未设置，心跳功能将跳过。", this);
            }

            if (_oneShotSource == null)
            {
                Debug.LogWarning("AudioDirector：_oneShotSource 未设置，氛围音效将跳过。", this);
            }

            if (_heartbeatFarDistance <= _heartbeatNearDistance)
            {
                Debug.LogWarning(
                    "AudioDirector：_heartbeatFarDistance 应大于 _heartbeatNearDistance，否则距离映射会退化。",
                    this);
            }

            _heartbeatAvailable = _heartbeatSource != null
                && _ghostTransform != null
                && _playerTransform != null;
            _oneShotAvailable = _oneShotSource != null;
        }

        private void SampleHeartbeatTargets()
        {
            float distance = Vector3.Distance(_playerTransform.position, _ghostTransform.position);
            float distanceRatio = Mathf.InverseLerp(
                _heartbeatNearDistance,
                _heartbeatFarDistance,
                distance);

            _targetHeartbeatVolume = Mathf.Lerp(
                _heartbeatMaxVolume,
                _heartbeatMinVolume,
                distanceRatio);
            _targetHeartbeatPitch = Mathf.Lerp(
                _heartbeatMaxPitch,
                _heartbeatMinPitch,
                distanceRatio);
        }

        private void EnsureHeartbeatPlaying()
        {
            if (_heartbeatSource.isPlaying)
            {
                return;
            }

            _heartbeatSource.clip = _heartbeatClip;
            _heartbeatSource.loop = true;
            _heartbeatSource.Play();
        }

        private void SmoothHeartbeat(bool heartbeatRequested)
        {
            float lerpAmount = _heartbeatLerpSpeed * Time.deltaTime;
            _heartbeatSource.volume = Mathf.Lerp(
                _heartbeatSource.volume,
                _targetHeartbeatVolume,
                lerpAmount);
            _heartbeatSource.pitch = Mathf.Lerp(
                _heartbeatSource.pitch,
                _targetHeartbeatPitch,
                lerpAmount);

            bool shouldStop = (!heartbeatRequested
                    || Mathf.Approximately(_targetHeartbeatVolume, 0f))
                && Mathf.Approximately(_heartbeatSource.volume, 0f);
            if (shouldStop && _heartbeatSource.isPlaying)
            {
                _heartbeatSource.volume = 0f;
                _heartbeatSource.Stop();
            }
        }

        private void ResetHeartbeatState()
        {
            _isHunting = false;
            _manifestHeartbeatRemaining = 0f;
            _heartbeatSampleTimer = 0f;
            _targetHeartbeatVolume = 0f;
            _targetHeartbeatPitch = _heartbeatMinPitch;

            if (_heartbeatSource == null)
            {
                return;
            }

            _heartbeatSource.Stop();
            _heartbeatSource.volume = 0f;
            _heartbeatSource.pitch = _heartbeatMinPitch;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (!_oneShotAvailable || clip == null)
            {
                return;
            }

            _oneShotSource.PlayOneShot(clip, volume);
        }

        private void UpdateBreathState()
        {
            if (!_isRoundActive)
            {
                return;
            }

            if (_postManifestBreathPending)
            {
                _postManifestBreathRemaining = Mathf.Max(
                    0f,
                    _postManifestBreathRemaining - Time.deltaTime);
                if (_postManifestBreathRemaining <= 0f)
                {
                    _postManifestBreathPending = false;
                    _isManifesting = false;
                    TryPlayBreath(_postManifestBreathChance);
                }
            }

            if (_isHunting || _isManifesting)
            {
                return;
            }

            _ambientBreathTimer -= Time.deltaTime;
            if (_ambientBreathTimer > 0f)
            {
                return;
            }

            _ambientBreathTimer = _ambientBreathCheckInterval;
            TryPlayBreath(_ambientBreathChance);
        }

        private void TryPlayBreath(float chance)
        {
            if (chance > 0f && (chance >= 1f || Random.value < chance))
            {
                PlayOneShot(_ghostManifestClip, _ghostManifestVolume);
            }
        }

        private void ResetBreathState(bool isRoundActive)
        {
            _isRoundActive = isRoundActive;
            _isManifesting = false;
            _postManifestBreathPending = false;
            _ambientBreathTimer = _ambientBreathCheckInterval;
            _postManifestBreathRemaining = 0f;
        }

        private void HandleRoundStart()
        {
            ResetHeartbeatState();
            ResetBreathState(true);
        }

        private void HandleRoundEnd(RoundResult roundResult)
        {
            ResetHeartbeatState();
            ResetBreathState(false);
        }

        private void HandleGhostEvent(Vector3 position)
        {
            _manifestHeartbeatRemaining = _manifestHeartbeatDuration;
            _heartbeatSampleTimer = 0f;
            _isManifesting = true;
            _postManifestBreathPending = true;
            _postManifestBreathRemaining = _manifestDuration;
            _ambientBreathTimer = _ambientBreathCheckInterval;
        }

        private void HandleHuntStart(float duration)
        {
            _isHunting = true;
            _heartbeatSampleTimer = 0f;
            PlayOneShot(_huntStartClip, _huntStartVolume);
        }

        private void HandleHuntEnd()
        {
            _isHunting = false;
            PlayOneShot(_huntEndClip, _huntEndVolume);
        }

        private void HandleBlackoutChanged(bool isBlackout)
        {
            if (isBlackout)
            {
                PlayOneShot(_blackoutClip, _blackoutVolume);
            }
        }

        private void HandleEvidenceFound(EvidenceType evidenceType)
        {
            PlayOneShot(_evidenceFoundClip, _evidenceFoundVolume);
        }

        private void HandlePlayerCaught()
        {
            PlayOneShot(_playerCaughtClip, _playerCaughtVolume);
        }

        private void HandleSanityCritical()
        {
            PlayOneShot(_sanityCriticalClip, _sanityCriticalVolume);
        }

        private void HandleStaminaDepleted()
        {
            PlayOneShot(_staminaDepletedClip, _staminaDepletedVolume);
        }
    }
}
