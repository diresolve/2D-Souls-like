using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [SerializeField] private CinemachineCamera[] _allVirtualCameras;

    [Header("Controls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallYPanTime = 0.35f;
    [SerializeField] private float _fallSpeedYDampingChangeThreshold = -6f;

    [Header("Playtest Debug Controls")]
    [SerializeField] private bool enablePlaytestDebugControls = true;
    [SerializeField] private float playtestZoomOrthographicSize = 4f;
    [SerializeField] private float playtestSlowMotionScale = 0.25f;

    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }

    private Coroutine lerpYPanCoroutine;
    private CinemachinePositionComposer _positionComposer;
    private CinemachineCamera _currentCamera;

    private float _normYPanAmount;
    private float _normalOrthographicSize;
    private float _normalFixedDeltaTime;
    private bool _hasCapturedPlaytestDefaults;
    private bool _isPlaytestZoomed;
    private bool _isPlaytestSlowMotion;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        for (int i = 0; i < _allVirtualCameras.Length; i++)
        {
            if (_allVirtualCameras[i] != null && _allVirtualCameras[i].enabled)
            {
                _currentCamera = _allVirtualCameras[i];
                _positionComposer = _currentCamera.GetComponent<CinemachinePositionComposer>();
                break;
            }
        }

        if (_positionComposer != null)
        {
            _normYPanAmount = _positionComposer.Damping.y;
        }

        CapturePlaytestDefaults();
    }

    private void Update()
    {
        HandlePlaytestDebugControls();
    }

    private void OnDisable()
    {
        ResetPlaytestDebugState();
    }

    private void OnDestroy()
    {
        ResetPlaytestDebugState();
    }

    public void LerpYDamping(bool isPlayerFalling)
    {
        if (_positionComposer == null)
        {
            return;
        }

        if (lerpYPanCoroutine != null)
        {
            StopCoroutine(lerpYPanCoroutine);
        }

        lerpYPanCoroutine = StartCoroutine(LerpYDampingAction(isPlayerFalling));
    }

    private IEnumerator LerpYDampingAction(bool isPlayerFalling)
    {
        IsLerpingYDamping = true;

        float startDampAmount = _positionComposer.Damping.y;
        float endDampAmount = 0f;

        if (isPlayerFalling)
        {
            endDampAmount = _fallPanAmount;
            LerpedFromPlayerFalling = true;
        }
        else
        {
            endDampAmount = _normYPanAmount;
            LerpedFromPlayerFalling = false;
        }

        float elapsedTime = 0f;
        Vector3 currentDamping = _positionComposer.Damping;

        while (elapsedTime < _fallYPanTime)
        {
            elapsedTime += Time.deltaTime;

            float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, elapsedTime / _fallYPanTime);
            _positionComposer.Damping = new Vector3(currentDamping.x, lerpedPanAmount, currentDamping.z);

            yield return null;
        }

        _positionComposer.Damping = new Vector3(currentDamping.x, endDampAmount, currentDamping.z);
        IsLerpingYDamping = false;
    }

    public float FallSpeedYDampingChangeThreshold
    {
        get { return _fallSpeedYDampingChangeThreshold; }
    }

    private void HandlePlaytestDebugControls()
    {
        if (!enablePlaytestDebugControls || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            TogglePlaytestZoom();
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TogglePlaytestSlowMotion();
        }

        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            ResetPlaytestDebugState();
        }
    }

    private void CapturePlaytestDefaults()
    {
        if (_currentCamera == null)
        {
            return;
        }

        _normalOrthographicSize = _currentCamera.Lens.OrthographicSize;
        _normalFixedDeltaTime = Time.fixedDeltaTime;
        _hasCapturedPlaytestDefaults = true;
    }

    private void TogglePlaytestZoom()
    {
        if (_currentCamera == null)
        {
            return;
        }

        if (!_hasCapturedPlaytestDefaults)
        {
            CapturePlaytestDefaults();
        }

        _isPlaytestZoomed = !_isPlaytestZoomed;
        _currentCamera.Lens.OrthographicSize = _isPlaytestZoomed
            ? playtestZoomOrthographicSize
            : _normalOrthographicSize;
    }

    private Coroutine timedSlowMotionRoutine;

    public void TriggerSlowMotionFor(float realSeconds, float scale = -1f)
    {
        if (!_hasCapturedPlaytestDefaults)
        {
            CapturePlaytestDefaults();
        }

        if (timedSlowMotionRoutine != null) StopCoroutine(timedSlowMotionRoutine);
        timedSlowMotionRoutine = StartCoroutine(TimedSlowMotionRoutine(realSeconds, scale));
    }

    private IEnumerator TimedSlowMotionRoutine(float realSeconds, float scale)
    {
        float targetScale = scale > 0f ? scale : playtestSlowMotionScale;

        Time.timeScale = targetScale;
        Time.fixedDeltaTime = _normalFixedDeltaTime * targetScale;

        yield return new WaitForSecondsRealtime(realSeconds);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = _normalFixedDeltaTime;
        _isPlaytestSlowMotion = false;
        timedSlowMotionRoutine = null;
    }

    private void TogglePlaytestSlowMotion()
    {
        if (!_hasCapturedPlaytestDefaults)
        {
            CapturePlaytestDefaults();
        }

        _isPlaytestSlowMotion = !_isPlaytestSlowMotion;

        float targetTimeScale = _isPlaytestSlowMotion ? playtestSlowMotionScale : 1f;
        Time.timeScale = targetTimeScale;
        Time.fixedDeltaTime = _normalFixedDeltaTime * targetTimeScale;
    }

    private void ResetPlaytestDebugState()
    {
        if (!_hasCapturedPlaytestDefaults)
        {
            return;
        }

        if (_currentCamera != null)
        {
            _currentCamera.Lens.OrthographicSize = _normalOrthographicSize;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = _normalFixedDeltaTime;
        _isPlaytestZoomed = false;
        _isPlaytestSlowMotion = false;
    }
}
