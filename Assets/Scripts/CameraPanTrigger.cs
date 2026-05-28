using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CameraPanTrigger : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _triggerSound;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera _virtualCamera;
    [SerializeField] private float _panRightOffset = 2f;
    [SerializeField] private float _panDownOffset = -3f;
    [SerializeField] private float _zoomOutOffset = 2f;
    [SerializeField] private float _panSpeed = 2f;
    [SerializeField] private float _pauseAtBottomDuration = 1.5f; 

    private CinemachinePositionComposer _positionComposer;
    private Coroutine _panCoroutine;
    private PlayerController _player;
    private float _originalXOffset;
    private float _originalYOffset;
    private float _originalOrthoSize;

    private bool _isPlayerInside = false;
    private bool _hasTriggeredOnce = false;

    private void Awake()
    {
        if (_virtualCamera != null)
        {
            _originalOrthoSize = _virtualCamera.Lens.OrthographicSize;

            _positionComposer = _virtualCamera.GetComponent<CinemachinePositionComposer>();
            if (_positionComposer != null)
            {
                _originalXOffset = _positionComposer.TargetOffset.x;
                _originalYOffset = _positionComposer.TargetOffset.y;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !_isPlayerInside && !_hasTriggeredOnce)
        {
            _isPlayerInside = true;
            _hasTriggeredOnce = true;

            _player = collision.GetComponent<PlayerController>();
            if (_player != null)
            {
                _player.LockMovementForAttack(true);
            }

            if (_audioSource != null && _triggerSound != null)
            {
                _audioSource.PlayOneShot(_triggerSound);
            }

            float actualPanRight = _panRightOffset;
            if (_player != null && _player.IsFacingRight)
            {
                actualPanRight = -_panRightOffset;
            }

            StartPanRoutine(_originalXOffset + actualPanRight, _originalYOffset + _panDownOffset, _originalOrthoSize + _zoomOutOffset);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && _isPlayerInside)
        {
            _isPlayerInside = false;
        }
    }

    private void StartPanRoutine(float targetXOffset, float targetYOffset, float targetZoom)
    {
        if (_positionComposer == null || _virtualCamera == null) return;

        if (_panCoroutine != null)
        {
            StopCoroutine(_panCoroutine);
        }

        _panCoroutine = StartCoroutine(CameraPanSequence(targetXOffset, targetYOffset, targetZoom));
    }

    private IEnumerator CameraPanSequence(float targetX, float targetY, float targetZoom)
    {
        yield return StartCoroutine(LerpCamera(targetX, targetY, targetZoom));

        yield return new WaitForSeconds(_pauseAtBottomDuration);

        yield return StartCoroutine(LerpCamera(_originalXOffset, _originalYOffset, _originalOrthoSize));

        if (_player != null)
        {
            _player.LockMovementForAttack(false);
        }
    }

    private IEnumerator LerpCamera(float targetX, float targetY, float targetZoom)
    {
        float currentX = _positionComposer.TargetOffset.x;
        float currentY = _positionComposer.TargetOffset.y;
        LensSettings currentLens = _virtualCamera.Lens;

        while (Mathf.Abs(currentX - targetX) > 0.01f || Mathf.Abs(currentY - targetY) > 0.01f || Mathf.Abs(currentLens.OrthographicSize - targetZoom) > 0.01f)
        {
            currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * _panSpeed);
            currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * _panSpeed);
            currentLens.OrthographicSize = Mathf.Lerp(currentLens.OrthographicSize, targetZoom, Time.deltaTime * _panSpeed);

            Vector3 currentOffset = _positionComposer.TargetOffset;
            currentOffset.x = currentX;
            currentOffset.y = currentY;
            _positionComposer.TargetOffset = currentOffset;
            _virtualCamera.Lens = currentLens;

            yield return null;
        }

        Vector3 finalOffset = _positionComposer.TargetOffset;
        finalOffset.x = targetX;
        finalOffset.y = targetY;
        _positionComposer.TargetOffset = finalOffset;

        currentLens.OrthographicSize = targetZoom;
        _virtualCamera.Lens = currentLens;
    }
}