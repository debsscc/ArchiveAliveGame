using System.Collections;
using UnityEngine;

public class SynesthesiaAudioController : MonoBehaviour, IAudioController
{
    [System.Serializable]
    public class ArtworkAudioPreset
    {
        public AudioClip clickSfx;
        public AudioClip musicClip;
    }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Fallback Clips")]
    [SerializeField] private AudioClip defaultClickSfx;
    [SerializeField] private AudioClip[] musicPerArtwork;

    [Header("Per Artwork Overrides")]
    [SerializeField] private ArtworkAudioPreset[] audioPresetsPerArtwork;

    [Header("Timing")]
    [SerializeField] private float musicFadeDuration = 0.35f;

    private Coroutine _crossFadeRoutine;

    public void PlayClick(int artworkIndex)
    {
        AudioClip clickClip = GetClickClipForArtwork(artworkIndex);

        if (sfxSource != null && clickClip != null)
        {
            sfxSource.PlayOneShot(clickClip);
        }
    }

    public void PlayMusicForArtwork(int artworkIndex, bool immediate)
    {
        if (musicSource == null)
        {
            return;
        }

        AudioClip targetClip = GetMusicClipForArtwork(artworkIndex);
        if (targetClip == null || musicSource.clip == targetClip)
        {
            return;
        }

        if (_crossFadeRoutine != null)
        {
            StopCoroutine(_crossFadeRoutine);
            _crossFadeRoutine = null;
        }

        if (immediate)
        {
            musicSource.clip = targetClip;
            musicSource.volume = 1f;
            musicSource.Play();
            return;
        }

        _crossFadeRoutine = StartCoroutine(CrossFadeMusic(targetClip));
    }

    private AudioClip GetClickClipForArtwork(int artworkIndex)
    {
        if (TryGetPreset(artworkIndex, out ArtworkAudioPreset preset) && preset.clickSfx != null)
        {
            return preset.clickSfx;
        }

        return defaultClickSfx;
    }

    private AudioClip GetMusicClipForArtwork(int artworkIndex)
    {
        if (TryGetPreset(artworkIndex, out ArtworkAudioPreset preset) && preset.musicClip != null)
        {
            return preset.musicClip;
        }

        if (musicPerArtwork == null || musicPerArtwork.Length == 0)
        {
            return null;
        }

        return musicPerArtwork[Mathf.Abs(artworkIndex) % musicPerArtwork.Length];
    }

    private bool TryGetPreset(int artworkIndex, out ArtworkAudioPreset preset)
    {
        preset = null;

        if (audioPresetsPerArtwork == null || audioPresetsPerArtwork.Length == 0)
        {
            return false;
        }

        int index = Mathf.Abs(artworkIndex) % audioPresetsPerArtwork.Length;
        preset = audioPresetsPerArtwork[index];
        return preset != null;
    }

    private IEnumerator CrossFadeMusic(AudioClip nextClip)
    {
        float startVolume = musicSource.volume;

        if (musicSource.isPlaying)
        {
            float fadeOutTime = 0f;
            while (fadeOutTime < musicFadeDuration)
            {
                fadeOutTime += Time.deltaTime;
                float normalized = Mathf.Clamp01(fadeOutTime / musicFadeDuration);
                musicSource.volume = Mathf.Lerp(startVolume, 0f, normalized);
                yield return null;
            }
        }

        musicSource.clip = nextClip;
        musicSource.Play();

        float fadeInTime = 0f;
        while (fadeInTime < musicFadeDuration)
        {
            fadeInTime += Time.deltaTime;
            float normalized = Mathf.Clamp01(fadeInTime / musicFadeDuration);
            musicSource.volume = Mathf.Lerp(0f, 1f, normalized);
            yield return null;
        }

        musicSource.volume = 1f;
        _crossFadeRoutine = null;
    }
}
