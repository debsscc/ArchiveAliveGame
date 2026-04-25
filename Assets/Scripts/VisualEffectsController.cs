using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VisualEffectsController : MonoBehaviour, IVisualEffectsController
{
    [System.Serializable]
    public class ArtworkVisualPreset
    {
        public float flashFrom = 0.75f;
        public float flashTo = 0f;
        public float flashDuration = 0.22f;
        public Gradient colorPulse;
        public float colorPulseDuration = 0.9f;
        public ParticleSystem[] particlesToBurst;
    }

    [Header("Targets")]
    [SerializeField] private CanvasGroup flashOverlay;
    [SerializeField] private Image artworkImage;

    [Header("Fallback FX")]
    [SerializeField] private float defaultFlashFrom = 0.75f;
    [SerializeField] private float defaultFlashTo = 0f;
    [SerializeField] private float defaultFlashDuration = 0.22f;
    [SerializeField] private Gradient defaultColorPulse;
    [SerializeField] private float defaultColorPulseDuration = 0.9f;
    [SerializeField] private ParticleSystem[] defaultParticlesToBurst;

    [Header("Per Artwork Overrides")]
    [SerializeField] private ArtworkVisualPreset[] visualPresetsPerArtwork;

    private Color _initialColor = Color.white;

    private void Awake()
    {
        if (artworkImage != null)
        {
            _initialColor = artworkImage.color;
        }

        if (flashOverlay != null)
        {
            flashOverlay.alpha = 0f;
        }
    }

    public IEnumerator PlayEffectsForArtwork(int artworkIndex)
    {
        ArtworkVisualPreset preset = GetPreset(artworkIndex);

        TriggerParticles(preset != null ? preset.particlesToBurst : defaultParticlesToBurst);
        yield return StartCoroutine(PlayFlashInternal(
            preset != null ? preset.flashFrom : defaultFlashFrom,
            preset != null ? preset.flashTo : defaultFlashTo,
            preset != null ? preset.flashDuration : defaultFlashDuration));
        yield return StartCoroutine(PlayColorPulseInternal(
            preset != null ? preset.colorPulse : defaultColorPulse,
            preset != null ? preset.colorPulseDuration : defaultColorPulseDuration));
    }

    private void TriggerParticles(ParticleSystem[] particlesToPlay)
    {
        if (particlesToPlay == null)
        {
            return;
        }

        for (int i = 0; i < particlesToPlay.Length; i++)
        {
            if (particlesToPlay[i] != null)
            {
                particlesToPlay[i].Play();
            }
        }
    }

    private IEnumerator PlayFlashInternal(float flashFrom, float flashTo, float flashDuration)
    {
        if (flashOverlay == null)
        {
            yield break;
        }

        if (flashDuration <= 0f)
        {
            flashOverlay.alpha = flashTo;
            yield break;
        }

        flashOverlay.alpha = flashFrom;

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / flashDuration);
            flashOverlay.alpha = Mathf.Lerp(flashFrom, flashTo, normalized);
            yield return null;
        }

        flashOverlay.alpha = flashTo;
    }

    private IEnumerator PlayColorPulseInternal(Gradient colorPulse, float colorPulseDuration)
    {
        if (artworkImage == null || colorPulse == null)
        {
            yield break;
        }

        if (colorPulseDuration <= 0f)
        {
            artworkImage.color = colorPulse.Evaluate(1f);
            yield break;
        }

        float t = 0f;
        while (t < colorPulseDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / colorPulseDuration);
            artworkImage.color = colorPulse.Evaluate(normalized);
            yield return null;
        }

        artworkImage.color = _initialColor;
    }

    private ArtworkVisualPreset GetPreset(int artworkIndex)
    {
        if (visualPresetsPerArtwork == null || visualPresetsPerArtwork.Length == 0)
        {
            return null;
        }

        int index = Mathf.Abs(artworkIndex) % visualPresetsPerArtwork.Length;
        return visualPresetsPerArtwork[index];
    }
}
