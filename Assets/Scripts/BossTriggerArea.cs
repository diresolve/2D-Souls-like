using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class BossArenaTrigger : MonoBehaviour
{
    [Header("The Reveal")]
    [SerializeField] private GameObject bossHealthBarUI;
    [SerializeField] private MusicController musicController;
    [SerializeField] private BossController bossController;

    [Header("Cinemachine Setup")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform bossFocusTarget;
    [SerializeField] private float zoomedOutSize = 8f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Loot")]
    [SerializeField] private Transform soulDropPoint;
    public Transform SoulDropPoint { get { return soulDropPoint; } }

    [Header("Timing")]
    [SerializeField] private float cinematicPauseDuration = 2f;

    private bool hasTriggered = false;
    private bool defeated = false;
    private float originalZoomSize;
    private bool hasOriginalZoomSize = false;
    private Coroutine activeCameraRoutine;
    private MonoBehaviour activeCameraRoutineRunner;

    private bool introPlayed = false;

    public bool HasTriggered { get { return hasTriggered; } }
    public bool IsDefeated { get { return defeated; } }

    private void Awake()
    {
        CacheOriginalZoomSize();
    }

    private void Start()
    {
        CacheOriginalZoomSize();
    }

    private void CacheOriginalZoomSize()
    {
        if (virtualCamera != null)
        {
            originalZoomSize = virtualCamera.Lens.OrthographicSize;
            hasOriginalZoomSize = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryTriggerArena(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryTriggerArena(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && hasTriggered)
        {
            ResetArea();
        }
    }

    public void ResetArea()
    {
        if (defeated) return;

        hasTriggered = false;

        if (bossHealthBarUI != null) bossHealthBarUI.SetActive(false);
        if (musicController != null) musicController.PlayBackgroundMusic();

        StartCameraRoutine(ResetCameraZoom());
    }

    public void OnBossDefeated()
    {
        defeated = true;
        hasTriggered = false;

        if (bossHealthBarUI != null) bossHealthBarUI.SetActive(false);
        if (musicController != null) musicController.PlayBackgroundMusic();

        StartCameraRoutine(ResetCameraZoom());
    }

    private void TryTriggerArena(Collider2D collision)
    {
        if (defeated || !collision.CompareTag("Player") || hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        PlayerController player = collision.GetComponent<PlayerController>();

        if (bossHealthBarUI != null) bossHealthBarUI.SetActive(true);
        if (musicController != null) musicController.PlayBossMusic();

        ActivateBoss();

        if (!introPlayed)
        {
            introPlayed = true;
            StartCameraRoutine(CinematicReveal(player));
        }
        else
        {
            if (player != null)
            {
                player.LockMovementForAttack(false);
            }
        }
    }

    private void StartCameraRoutine(IEnumerator routine)
    {
        StopActiveCameraRoutine();

        if (!gameObject.activeInHierarchy || !enabled)
        {
            ResetCameraZoomInstantly();
            return;
        }

        activeCameraRoutineRunner = this;
        activeCameraRoutine = StartCoroutine(routine);
    }

    private void StopActiveCameraRoutine()
    {
        if (activeCameraRoutine != null)
        {
            if (activeCameraRoutineRunner != null)
            {
                activeCameraRoutineRunner.StopCoroutine(activeCameraRoutine);
            }

            activeCameraRoutine = null;
            activeCameraRoutineRunner = null;
        }
    }

    private void ActivateBoss()
    {
        BossController bossToActivate = bossController;
        if (bossToActivate == null && bossFocusTarget != null)
        {
            bossToActivate = bossFocusTarget.GetComponent<BossController>();
        }

        if (bossToActivate != null)
        {
            bossToActivate.ActivateBoss();
        }
    }

    private void ResetCameraZoomInstantly()
    {
        if (virtualCamera == null || !hasOriginalZoomSize)
        {
            return;
        }

        LensSettings currentLens = virtualCamera.Lens;
        currentLens.OrthographicSize = originalZoomSize;
        virtualCamera.Lens = currentLens;
    }

    private IEnumerator ResetCameraZoom()
    {
        if (virtualCamera != null && hasOriginalZoomSize)
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

        ActivateBoss();
    }
}
