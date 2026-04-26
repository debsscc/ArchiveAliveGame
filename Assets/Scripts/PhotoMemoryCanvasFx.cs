using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class PhotoMemoryCanvasFx
{
    public static void SetCanvasGroupState(CanvasGroup group, float alpha, bool blocksRaycasts, bool interactable)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.blocksRaycasts = blocksRaycasts;
        group.interactable = interactable;
    }

    public static IEnumerator PlayFlash(CanvasGroup flashGroup, float peakAlpha, float duration)
    {
        if (flashGroup == null)
        {
            yield break;
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        float halfDuration = safeDuration * 0.5f;

        flashGroup.blocksRaycasts = false;
        flashGroup.interactable = false;

        yield return FadeCanvasGroup(flashGroup, 0f, peakAlpha, halfDuration);
        yield return FadeCanvasGroup(flashGroup, peakAlpha, 0f, halfDuration);
    }

    public static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(from, to, n);
            yield return null;
        }

        group.alpha = to;
    }

    public static IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        if (image == null)
        {
            yield break;
        }

        Color color = image.color;

        if (duration <= 0f)
        {
            color.a = to;
            image.color = color;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            color.a = Mathf.Lerp(from, to, n);
            image.color = color;
            yield return null;
        }

        color.a = to;
        image.color = color;
    }
}
