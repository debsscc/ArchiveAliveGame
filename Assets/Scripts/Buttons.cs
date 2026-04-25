using System.Collections;
using UnityEngine;

public class Buttons : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private MonoBehaviour artworkDisplayBehaviour;
    [SerializeField] private MonoBehaviour visualEffectsBehaviour;
    [SerializeField] private MonoBehaviour audioControllerBehaviour;

    [Header("Timing")]
    [SerializeField] private float holdDuration = 0.6f;

    private IArtworkDisplay _artworkDisplay;
    private IVisualEffectsController _visualEffects;
    private IAudioController _audioController;
    private bool _isTransitionRunning;

    private void Awake()
    {
        _artworkDisplay = artworkDisplayBehaviour as IArtworkDisplay;
        _visualEffects = visualEffectsBehaviour as IVisualEffectsController;
        _audioController = audioControllerBehaviour as IAudioController;

        if (_artworkDisplay == null || _visualEffects == null || _audioController == null)
        {
            Debug.LogError("Buttons: Assign components that implement IArtworkDisplay, IVisualEffectsController and IAudioController.", this);
            enabled = false;
            return;
        }

        _audioController.PlayMusicForArtwork(_artworkDisplay.CurrentArtworkIndex, immediate: true);
    }

    public void OnArtworkClicked()
    {
        if (!_isTransitionRunning)
        {
            StartCoroutine(PlaySynestheticTransition());
        }
    }

    private IEnumerator PlaySynestheticTransition()
    {
        _isTransitionRunning = true;

        int currentArtworkIndex = _artworkDisplay.CurrentArtworkIndex;
        _audioController.PlayClick(currentArtworkIndex);

        yield return StartCoroutine(_artworkDisplay.ZoomIn());

        _artworkDisplay.ShowNextArtwork();
        int nextArtworkIndex = _artworkDisplay.CurrentArtworkIndex;
        _audioController.PlayMusicForArtwork(nextArtworkIndex, immediate: false);

        yield return StartCoroutine(_visualEffects.PlayEffectsForArtwork(nextArtworkIndex));

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        yield return StartCoroutine(_artworkDisplay.ZoomOut());
        _isTransitionRunning = false;
    }
}
