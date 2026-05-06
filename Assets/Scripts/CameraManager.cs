using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [SerializeField] private CinemachineCamera[] _allVirtualCameras;

    [Header("Controls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallYPanTime = 0.35f;
    [SerializeField] private float _fallSpeedYDampingChangeThreshold = -6f;

    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }

    private Coroutine lerpYPanCoroutine;
    private CinemachinePositionComposer _positionComposer;
    private CinemachineCamera _currentCamera;

    private float _normYPanAmount;

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
}