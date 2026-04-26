using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class PhotoMemoryOverlayController
{
    private const string BackdropName = "MemoryOverlayBackdrop";
    private const string FogLayerName = "MemoryOverlayFog";

    private readonly MonoBehaviour owner;
    private readonly Image overlayImage;
    private readonly Image backdropImage;
    private readonly Image fogLayerImage;
    private Coroutine effectRoutine;
    private float overlayBaseScale = 1f;

    public PhotoMemoryOverlayController(MonoBehaviour owner, Image overlayImage)
    {
        this.owner = owner;
        this.overlayImage = overlayImage;
        backdropImage = EnsureBackdropImage();
        fogLayerImage = EnsureFogLayerImage();
    }

    public void ConfigureInitialState()
    {
        if (overlayImage == null)
        {
            return;
        }

        SetImageAlpha(backdropImage, 0f);
        if (backdropImage != null)
        {
            backdropImage.gameObject.SetActive(false);
        }

        SetImageAlpha(fogLayerImage, 0f);
        if (fogLayerImage != null)
        {
            fogLayerImage.rectTransform.localPosition = Vector3.zero;
            fogLayerImage.rectTransform.localScale = Vector3.one;
            fogLayerImage.gameObject.SetActive(false);
        }

        overlayImage.color = new Color(1f, 1f, 1f, 0f);
        overlayImage.rectTransform.localPosition = Vector3.zero;
        overlayImage.rectTransform.localScale = Vector3.one;
        overlayImage.gameObject.SetActive(false);
        overlayBaseScale = 1f;
    }

    public IEnumerator ShowOverlay(Sprite sprite, float fadeDuration, float openScale)
    {
        if (overlayImage == null)
        {
            yield break;
        }

        overlayBaseScale = Mathf.Max(0.1f, openScale);

        if (backdropImage != null)
        {
            backdropImage.gameObject.SetActive(true);
        }

        if (fogLayerImage != null)
        {
            fogLayerImage.gameObject.SetActive(false);
            SetImageAlpha(fogLayerImage, 0f);
        }

        overlayImage.sprite = sprite;
        overlayImage.gameObject.SetActive(true);
        overlayImage.rectTransform.localPosition = Vector3.zero;
        overlayImage.rectTransform.localScale = Vector3.one * overlayBaseScale;

        yield return FadeOverlayAndBackdrop(0f, 1f, 0f, 1f, fadeDuration);
    }

    public IEnumerator HideOverlay(float fadeDuration)
    {
        if (overlayImage == null)
        {
            yield break;
        }

        float fromAlpha = overlayImage.color.a;
        float fromBackdropAlpha = backdropImage != null ? backdropImage.color.a : 0f;

        if (backdropImage != null && !backdropImage.gameObject.activeSelf)
        {
            backdropImage.gameObject.SetActive(true);
        }

        yield return FadeOverlayAndBackdrop(fromAlpha, 0f, fromBackdropAlpha, 0f, fadeDuration);

        overlayImage.rectTransform.localPosition = Vector3.zero;
        overlayImage.rectTransform.localScale = Vector3.one;
        overlayImage.gameObject.SetActive(false);
        overlayBaseScale = 1f;

        if (backdropImage != null)
        {
            backdropImage.gameObject.SetActive(false);
        }

        if (fogLayerImage != null)
        {
            fogLayerImage.rectTransform.localPosition = Vector3.zero;
            fogLayerImage.rectTransform.localScale = Vector3.one;
            fogLayerImage.gameObject.SetActive(false);
            SetImageAlpha(fogLayerImage, 0f);
        }
    }

    public void StartEffect(PhotoMemorySequenceController.MemoryButtonEntry entry, Func<bool> keepRunning)
    {
        StopEffect();

        if (owner == null || overlayImage == null || entry == null)
        {
            return;
        }

        if (entry.overlayEffect == PhotoMemorySequenceController.OverlayEffectType.None)
        {
            if (fogLayerImage != null)
            {
                fogLayerImage.gameObject.SetActive(false);
                SetImageAlpha(fogLayerImage, 0f);
            }

            return;
        }

        effectRoutine = owner.StartCoroutine(RunEffect(entry, keepRunning));
    }

    public void StopEffect()
    {
        if (owner != null && effectRoutine != null)
        {
            owner.StopCoroutine(effectRoutine);
            effectRoutine = null;
        }

        if (overlayImage != null)
        {
            overlayImage.rectTransform.localPosition = Vector3.zero;
            overlayImage.rectTransform.localScale = Vector3.one * overlayBaseScale;
            overlayImage.color = new Color(1f, 1f, 1f, overlayImage.color.a);
        }

        if (fogLayerImage != null)
        {
            fogLayerImage.rectTransform.localPosition = Vector3.zero;
            fogLayerImage.rectTransform.localScale = Vector3.one;
            fogLayerImage.gameObject.SetActive(false);
            SetImageAlpha(fogLayerImage, 0f);
        }
    }

    private IEnumerator RunEffect(PhotoMemorySequenceController.MemoryButtonEntry entry, Func<bool> keepRunning)
    {
        RectTransform rect = overlayImage.rectTransform;
        Color baseColor = overlayImage.color;

        while (keepRunning == null || keepRunning())
        {
            float speed = Mathf.Max(0.01f, entry.overlayEffectSpeed);
            float intensity = Mathf.Max(0f, entry.overlayEffectIntensity);
            float time = Time.time * speed;

            switch (entry.overlayEffect)
            {
                case PhotoMemorySequenceController.OverlayEffectType.Pulse:
                {
                    float pulse = 1f + Mathf.Sin(time * 6f) * 0.03f * intensity;
                    float targetScale = overlayBaseScale * pulse;
                    rect.localScale = new Vector3(targetScale, targetScale, 1f);
                    break;
                }
                case PhotoMemorySequenceController.OverlayEffectType.Sway:
                {
                    float x = Mathf.Sin(time * 2.7f) * 8f * intensity;
                    float y = Mathf.Cos(time * 2.1f) * 5f * intensity;
                    rect.localPosition = new Vector3(x, y, 0f);
                    break;
                }
                case PhotoMemorySequenceController.OverlayEffectType.Flicker:
                {
                    float flicker = 0.82f + Mathf.PerlinNoise(time * 4f, 0f) * 0.18f * intensity;
                    Color c = baseColor;
                    c.a = Mathf.Clamp01(flicker);
                    overlayImage.color = c;
                    break;
                }
                case PhotoMemorySequenceController.OverlayEffectType.Fog:
                {
                    if (fogLayerImage == null)
                    {
                        break;
                    }

                    fogLayerImage.gameObject.SetActive(true);

                    RectTransform fogRect = fogLayerImage.rectTransform;
                    float drift = 18f * intensity;
                    float x = Mathf.Sin(time * 0.9f) * drift;
                    float y = Mathf.Cos(time * 0.7f) * drift * 0.6f;
                    fogRect.localPosition = new Vector3(x, y, 0f);

                    float scale = 1.08f + Mathf.Sin(time * 0.5f) * 0.06f * intensity;
                    fogRect.localScale = new Vector3(scale, scale, 1f);

                    float breathing = (Mathf.Sin(time * 1.35f) + 1f) * 0.5f;
                    float fogAlpha = Mathf.Lerp(0.06f, 0.22f, breathing) * Mathf.Clamp(intensity, 0f, 2f);
                    SetImageAlpha(fogLayerImage, fogAlpha);
                    break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator FadeOverlayAndBackdrop(
        float overlayFrom,
        float overlayTo,
        float backdropFrom,
        float backdropTo,
        float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float t = 0f;

        while (t < safeDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / safeDuration);
            SetImageAlpha(overlayImage, Mathf.Lerp(overlayFrom, overlayTo, n));
            SetImageAlpha(backdropImage, Mathf.Lerp(backdropFrom, backdropTo, n));
            yield return null;
        }

        SetImageAlpha(overlayImage, overlayTo);
        SetImageAlpha(backdropImage, backdropTo);
    }

    private Image EnsureBackdropImage()
    {
        if (overlayImage == null)
        {
            return null;
        }

        RectTransform overlayRect = overlayImage.rectTransform;
        RectTransform parent = overlayRect.parent as RectTransform;
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(BackdropName);
        Image backdrop = existing != null ? existing.GetComponent<Image>() : null;

        if (backdrop == null)
        {
            GameObject backdropObject = new GameObject(BackdropName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
            backdropRect.SetParent(parent, false);
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.anchoredPosition = Vector2.zero;
            backdropRect.sizeDelta = Vector2.zero;
            backdropRect.localScale = Vector3.one;

            backdrop = backdropObject.GetComponent<Image>();
        }

        RectTransform rect = backdrop.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetSiblingIndex(overlayRect.GetSiblingIndex());

        backdrop.color = new Color(0f, 0f, 0f, 0f);
        backdrop.raycastTarget = false;
        backdrop.sprite = null;
        backdrop.type = Image.Type.Simple;
        backdrop.preserveAspect = false;

        return backdrop;
    }

    private Image EnsureFogLayerImage()
    {
        if (overlayImage == null)
        {
            return null;
        }

        RectTransform overlayRect = overlayImage.rectTransform;
        RectTransform parent = overlayRect.parent as RectTransform;
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(FogLayerName);
        Image fog = existing != null ? existing.GetComponent<Image>() : null;

        if (fog == null)
        {
            GameObject fogObject = new GameObject(FogLayerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform fogRect = fogObject.GetComponent<RectTransform>();
            fogRect.SetParent(parent, false);
            fogRect.anchorMin = Vector2.zero;
            fogRect.anchorMax = Vector2.one;
            fogRect.anchoredPosition = Vector2.zero;
            fogRect.sizeDelta = Vector2.zero;
            fogRect.localScale = Vector3.one;

            fog = fogObject.GetComponent<Image>();
        }

        RectTransform rect = fog.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetSiblingIndex(Mathf.Clamp(overlayRect.GetSiblingIndex(), 0, int.MaxValue));

        if (overlayRect.GetSiblingIndex() > 0)
        {
            rect.SetSiblingIndex(overlayRect.GetSiblingIndex() - 1);
        }

        fog.color = new Color(0.82f, 0.84f, 0.88f, 0f);
        fog.raycastTarget = false;
        fog.sprite = null;
        fog.type = Image.Type.Simple;
        fog.preserveAspect = false;
        fog.gameObject.SetActive(false);

        return fog;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }
}
