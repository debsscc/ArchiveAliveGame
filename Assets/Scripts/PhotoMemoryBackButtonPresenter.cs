using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class PhotoMemoryBackButtonPresenter
{
    private readonly MonoBehaviour owner;
    private readonly Button backButton;
    private readonly CanvasGroup buttonCanvasGroup;
    private Coroutine fadeRoutine;

    public PhotoMemoryBackButtonPresenter(MonoBehaviour owner, Button backButton, CanvasGroup buttonCanvasGroup)
    {
        this.owner = owner;
        this.backButton = backButton;
        this.buttonCanvasGroup = buttonCanvasGroup;
    }

    public void SetVisible(bool visible, bool instant)
    {
        if (backButton == null)
        {
            return;
        }

        if (buttonCanvasGroup == null)
        {
            backButton.gameObject.SetActive(visible);
            return;
        }

        backButton.gameObject.SetActive(true);

        if (instant)
        {
            PhotoMemoryCanvasFx.SetCanvasGroupState(buttonCanvasGroup, visible ? 1f : 0f, visible, visible);
            if (!visible)
            {
                backButton.gameObject.SetActive(false);
            }

            return;
        }

        if (fadeRoutine != null && owner != null)
        {
            owner.StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (owner != null)
        {
            fadeRoutine = owner.StartCoroutine(FadeVisibility(visible));
        }
    }

    private IEnumerator FadeVisibility(bool visible)
    {
        if (buttonCanvasGroup == null)
        {
            yield break;
        }

        float from = buttonCanvasGroup.alpha;
        float to = visible ? 1f : 0f;
        float duration = 0.2f;

        if (visible)
        {
            backButton.gameObject.SetActive(true);
            buttonCanvasGroup.interactable = true;
            buttonCanvasGroup.blocksRaycasts = true;
        }

        yield return PhotoMemoryCanvasFx.FadeCanvasGroup(buttonCanvasGroup, from, to, duration);

        buttonCanvasGroup.interactable = visible;
        buttonCanvasGroup.blocksRaycasts = visible;

        if (!visible)
        {
            backButton.gameObject.SetActive(false);
        }

        fadeRoutine = null;
    }
}
