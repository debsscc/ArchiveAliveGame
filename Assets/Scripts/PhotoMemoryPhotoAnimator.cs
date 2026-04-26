using System.Collections;
using UnityEngine;

public static class PhotoMemoryPhotoAnimator
{
    public static IEnumerator AnimateToCenter(
        RectTransform photo,
        Vector3 initialScale,
        float zoomScale,
        float duration)
    {
        if (photo == null)
        {
            yield break;
        }

        Vector2 fromPos = photo.anchoredPosition;
        Vector3 fromScale = photo.localScale;
        Vector2 toPos = Vector2.zero;
        Vector3 toScale = initialScale * Mathf.Max(0.01f, zoomScale);

        if (duration <= 0f)
        {
            photo.anchoredPosition = toPos;
            photo.localScale = toScale;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float eased = EaseOutCubic(n);
            photo.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, eased);
            photo.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            yield return null;
        }

        photo.anchoredPosition = toPos;
        photo.localScale = toScale;
    }

    public static IEnumerator AnimateBack(
        RectTransform photo,
        Vector2 targetPosition,
        Vector3 targetScale,
        float duration)
    {
        if (photo == null)
        {
            yield break;
        }

        Vector2 fromPos = photo.anchoredPosition;
        Vector3 fromScale = photo.localScale;

        if (duration <= 0f)
        {
            photo.anchoredPosition = targetPosition;
            photo.localScale = targetScale;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float eased = EaseInOutSine(n);
            photo.anchoredPosition = Vector2.LerpUnclamped(fromPos, targetPosition, eased);
            photo.localScale = Vector3.LerpUnclamped(fromScale, targetScale, eased);
            yield return null;
        }

        photo.anchoredPosition = targetPosition;
        photo.localScale = targetScale;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinus = 1f - t;
        return 1f - oneMinus * oneMinus * oneMinus;
    }

    private static float EaseInOutSine(float t)
    {
        t = Mathf.Clamp01(t);
        return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
    }
}
