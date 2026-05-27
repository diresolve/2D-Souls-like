using System.Collections;
using UnityEngine;

public class MusicController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource bossMusic;

    [Header("Target Volumes")]
    [SerializeField, Range(0f, 1f)] private float backgroundVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float bossVolume = 0.7f;

    [Header("Crossfade")]
    [SerializeField] private float crossfadeDuration = 1.5f;

    private Coroutine activeFade;

    private void Start()
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.loop = true;
            backgroundMusic.volume = backgroundVolume;
            backgroundMusic.Play();
        }

        if (bossMusic != null)
        {
            bossMusic.loop = true;
            bossMusic.volume = 0f;
            bossMusic.Stop();
        }
    }

    public void PlayBossMusic()
    {
        StartFade(backgroundMusic, 0f, bossMusic, bossVolume);
    }

    public void PlayBackgroundMusic()
    {
        StartFade(bossMusic, 0f, backgroundMusic, backgroundVolume);
    }

    private void StartFade(AudioSource fadeOut, float fadeOutTo, AudioSource fadeIn, float fadeInTo)
    {
        if (activeFade != null) StopCoroutine(activeFade);
        activeFade = StartCoroutine(Crossfade(fadeOut, fadeOutTo, fadeIn, fadeInTo));
    }

    private IEnumerator Crossfade(AudioSource fadeOut, float fadeOutTo, AudioSource fadeIn, float fadeInTo)
    {
        if (fadeIn != null && !fadeIn.isPlaying)
        {
            fadeIn.volume = 0f;
            fadeIn.Play();
        }

        float startOut = fadeOut != null ? fadeOut.volume : 0f;
        float startIn = fadeIn != null ? fadeIn.volume : 0f;

        float t = 0f;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / crossfadeDuration);

            if (fadeOut != null) fadeOut.volume = Mathf.Lerp(startOut, fadeOutTo, p);
            if (fadeIn != null) fadeIn.volume = Mathf.Lerp(startIn, fadeInTo, p);

            yield return null;
        }

        if (fadeOut != null)
        {
            fadeOut.volume = fadeOutTo;
            if (fadeOutTo <= 0f) fadeOut.Stop();
        }

        if (fadeIn != null) fadeIn.volume = fadeInTo;

        activeFade = null;
    }
}
