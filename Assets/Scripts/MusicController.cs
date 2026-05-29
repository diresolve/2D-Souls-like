using System.Collections;
using UnityEngine;

public class MusicController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource bossMusic;
    [SerializeField] private AudioSource bonfireMusic;

    [Header("Target Volumes")]
    [SerializeField, Range(0f, 1f)] private float backgroundVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float bossVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float bonfireVolume = 0.6f;

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

        if (bonfireMusic != null)
        {
            bonfireMusic.loop = true;
            bonfireMusic.volume = 0f;
            bonfireMusic.Stop();
        }
    }

    public void PlayBossMusic()
    {
        StartFade(new[] { backgroundMusic, bonfireMusic }, bossMusic, bossVolume);
    }

    public void PlayBackgroundMusic()
    {
        StartFade(new[] { bossMusic, bonfireMusic }, backgroundMusic, backgroundVolume);
    }

    public void PlayBonfireMusic()
    {
        StartFade(new[] { backgroundMusic, bossMusic }, bonfireMusic, bonfireVolume);
    }

    private void StartFade(AudioSource[] fadeOuts, AudioSource fadeIn, float fadeInTo)
    {
        if (activeFade != null) StopCoroutine(activeFade);
        activeFade = StartCoroutine(Crossfade(fadeOuts, fadeIn, fadeInTo));
    }

    private IEnumerator Crossfade(AudioSource[] fadeOuts, AudioSource fadeIn, float fadeInTo)
    {
        if (fadeIn != null && !fadeIn.isPlaying)
        {
            fadeIn.volume = 0f;
            fadeIn.Play();
        }

        float[] startOuts = new float[fadeOuts != null ? fadeOuts.Length : 0];
        for (int i = 0; i < startOuts.Length; i++)
        {
            startOuts[i] = fadeOuts[i] != null ? fadeOuts[i].volume : 0f;
        }
        float startIn = fadeIn != null ? fadeIn.volume : 0f;

        float t = 0f;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / crossfadeDuration);

            for (int i = 0; i < startOuts.Length; i++)
            {
                if (fadeOuts[i] != null) fadeOuts[i].volume = Mathf.Lerp(startOuts[i], 0f, p);
            }
            if (fadeIn != null) fadeIn.volume = Mathf.Lerp(startIn, fadeInTo, p);

            yield return null;
        }

        for (int i = 0; i < startOuts.Length; i++)
        {
            if (fadeOuts[i] != null)
            {
                fadeOuts[i].volume = 0f;
                fadeOuts[i].Stop();
            }
        }

        if (fadeIn != null) fadeIn.volume = fadeInTo;

        activeFade = null;
    }
}
