using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class BossArenaTrigger : MonoBehaviour
{
    [Header("The Reveal")]
    [SerializeField] private GameObject bossHealthBarUI;
    [SerializeField] private AudioSource bossMusic;

    [Header("Cinemachine Setup")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform bossFocusTarget;
    [SerializeField] private float zoomedOutSize = 8f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Timing")]
    [SerializeField] private float cinematicPauseDuration = 2f;

    private bool hasTriggered = false;
    private float originalZoomSize;
    private Coroutine activeCameraRoutine;

    private void Start()
    {
        if (virtualCamera != null)
        {
            originalZoomSize = virtualCamera.Lens.OrthographicSize;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;

            PlayerController player = collision.GetComponent<PlayerController>();

            if (bossHealthBarUI != null) bossHealthBarUI.SetActive(true);
            if (bossMusic != null) bossMusic.Play();

            if (activeCameraRoutine != null) StopCoroutine(activeCameraRoutine);
            activeCameraRoutine = StartCoroutine(CinematicReveal(player));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && hasTriggered)
        {
            hasTriggered = false;

            if (bossHealthBarUI != null) bossHealthBarUI.SetActive(false);
            if (bossMusic != null) bossMusic.Stop();

            if (activeCameraRoutine != null) StopCoroutine(activeCameraRoutine);
            activeCameraRoutine = StartCoroutine(ResetCameraZoom());
        }
    }

    private IEnumerator ResetCameraZoom()
    {
        if (virtualCamera != null)
        {
            LensSettings currentLens = virtualCamera.Lens;

            while (Mathf.Abs(currentLens.OrthographicSize - originalZoomSize) > 0.01f)
            {
                currentLens.OrthographicSize = Mathf.Lerp(currentLens.OrthographicSize, originalZoomSize, Time.deltaTime * zoomSpeed);
                virtualCamera.Lens = currentLens;
                yield return null;
            }

            currentLens.OrthographicSize = originalZoomSize;
            virtualCamera.Lens = currentLens;
        }
    }

    private IEnumerator CinematicReveal(PlayerController player)
    {
        if (player != null)
        {
            player.LockMovementForAttack(true);
        }

        Transform originalTarget = virtualCamera != null ? virtualCamera.Target.TrackingTarget : null;

        if (virtualCamera != null)
        {
            if (bossFocusTarget != null)
            {
                virtualCamera.Target.TrackingTarget = bossFocusTarget; 
            }

            float targetSize = zoomedOutSize;
            LensSettings currentLens = virtualCamera.Lens;

            while (Mathf.Abs(currentLens.OrthographicSize - targetSize) > 0.01f)
            {
                currentLens.OrthographicSize = Mathf.Lerp(currentLens.OrthographicSize, targetSize, Time.deltaTime * zoomSpeed);
                virtualCamera.Lens = currentLens;
                yield return null;
            }

            currentLens.OrthographicSize = targetSize;
            virtualCamera.Lens = currentLens;
        }

        yield return new WaitForSeconds(cinematicPauseDuration);

        if (virtualCamera != null && originalTarget != null)
        {
            virtualCamera.Target.TrackingTarget = originalTarget;
        }

        if (player != null)
        {
            player.LockMovementForAttack(false);
        }
    }
}