using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PhotoMemorySequenceController : MonoBehaviour
{
    public enum OverlayEffectType
    {
        None,
        Pulse,
        Sway,
        Flicker,
        Fog
    }

    [System.Serializable]
    public class MemoryButtonEntry
    {
        [Header("Binding")]
        public string id;
        public Button triggerButton;
        public RectTransform sourcePhoto;
        public AudioSource buttonAudioSource;

        [Header("Memory Overlay")]
        public Sprite overlaySprite;
        [Min(0.1f)] public float overlayOpenScale = 1f;
        public OverlayEffectType overlayEffect = OverlayEffectType.None;
        public float overlayEffectSpeed = 1f;
        public float overlayEffectIntensity = 1f;

        [Header("Button VFX")]
        public GameObject buttonVisualEffectPrefab;
        public Vector3 buttonVfxLocalOffset = Vector3.zero;
        public Vector3 buttonVfxLocalScale = Vector3.one;

        [Header("Open Transition")]
        public float zoomScale = 2.2f;
        public float zoomDuration = 0.45f;

        [HideInInspector] public Vector2 initialAnchoredPosition;
        [HideInInspector] public Vector3 initialScale;
        [HideInInspector] public int initialSiblingIndex;
        [HideInInspector] public float initialAudioVolume;
    }

    [Header("Entries")]
    [SerializeField] private MemoryButtonEntry[] memoryButtons;

    [Header("Overlays")]
    [SerializeField] private Image memoryOverlayImage;
    [SerializeField] private CanvasGroup whiteFlashOverlay;
    [SerializeField] private CanvasGroup blackFlashOverlay;

    [Header("Back Button")]
    [SerializeField] private Button backButton;
    [SerializeField] private CanvasGroup backButtonCanvasGroup;

    [Header("Timings")]
    [SerializeField] private float openHoldAfterFlash = 0.15f;
    [SerializeField] private float overlayFadeDuration = 0.3f;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private float whiteFlashPeak = 1f;
    [SerializeField] private float blackFlashPeak = 1f;
    [SerializeField] private float closeZoomDuration = 0.45f;

    [Header("Audio")]
    [SerializeField] private float audioFadeInDuration = 0.25f;
    [SerializeField] private float audioFadeOutDuration = 0.25f;

    [Header("Behavior")]
    [SerializeField] private bool disableAllButtonsWhileOpen = true;

    private MemoryButtonEntry _activeEntry;
    private bool _isOpen;
    private bool _isTransitionRunning;
    private PhotoMemoryOverlayController _overlayController;
    private PhotoMemoryBackButtonPresenter _backButtonPresenter;
    private Coroutine _audioFadeRoutine;
    private GameObject _activeButtonVfxInstance;
    private Canvas _activeTransitionCanvas;
    private bool _activeTransitionCanvasWasCreated;
    private bool _activeTransitionCanvasOriginalOverrideSorting;
    private int _activeTransitionCanvasOriginalSortingOrder;

    private void Awake()
    {
        _overlayController = new PhotoMemoryOverlayController(this, memoryOverlayImage);
        _backButtonPresenter = new PhotoMemoryBackButtonPresenter(this, backButton, backButtonCanvasGroup);

        CacheInitialTransforms();
        BindButtonCallbacks();
        ConfigureInitialUiState();
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonPressed);
        }

        ReleaseActivePhotoTransitionLayer();
        StopActiveButtonVfx();
    }

    public void OpenMemoryByIndex(int index)
    {
        if (index < 0 || memoryButtons == null || index >= memoryButtons.Length)
        {
            return;
        }

        TryOpenMemory(memoryButtons[index]);
    }

    public void TryOpenMemory(MemoryButtonEntry entry)
    {
        if (entry == null || _isTransitionRunning || _isOpen)
        {
            return;
        }

        if (entry.sourcePhoto == null)
        {
            Debug.LogWarning("PhotoMemorySequenceController: entry sourcePhoto is missing.", this);
            return;
        }

        if (entry.overlaySprite == null)
        {
            Debug.LogWarning("PhotoMemorySequenceController: entry overlaySprite is missing.", this);
        }

        StartCoroutine(OpenSequence(entry));
    }

    public void OnBackButtonPressed()
    {
        if (_isTransitionRunning || !_isOpen || _activeEntry == null)
        {
            return;
        }

        StartCoroutine(CloseSequence());
    }

    private void CacheInitialTransforms()
    {
        if (memoryButtons == null)
        {
            return;
        }

        for (int i = 0; i < memoryButtons.Length; i++)
        {
            MemoryButtonEntry entry = memoryButtons[i];
            if (entry == null || entry.sourcePhoto == null)
            {
                continue;
            }

            entry.initialAnchoredPosition = entry.sourcePhoto.anchoredPosition;
            entry.initialScale = entry.sourcePhoto.localScale;
            entry.initialSiblingIndex = entry.sourcePhoto.GetSiblingIndex();
            entry.initialAudioVolume = entry.buttonAudioSource != null ? Mathf.Clamp01(entry.buttonAudioSource.volume) : 1f;
        }
    }

    private void BindButtonCallbacks()
    {
        if (memoryButtons != null)
        {
            for (int i = 0; i < memoryButtons.Length; i++)
            {
                MemoryButtonEntry localEntry = memoryButtons[i];
                if (localEntry != null && localEntry.triggerButton != null)
                {
                    localEntry.triggerButton.onClick.AddListener(() => TryOpenMemory(localEntry));
                }
            }
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonPressed);
        }
    }

    private void ConfigureInitialUiState()
    {
        PhotoMemoryCanvasFx.SetCanvasGroupState(whiteFlashOverlay, 0f, false, false);
        PhotoMemoryCanvasFx.SetCanvasGroupState(blackFlashOverlay, 0f, false, false);
        _overlayController?.ConfigureInitialState();
        _backButtonPresenter?.SetVisible(false, instant: true);
    }

    private IEnumerator OpenSequence(MemoryButtonEntry entry)
    {
        _isTransitionRunning = true;
        _activeEntry = entry;
        StartButtonVfx(entry);

        if (disableAllButtonsWhileOpen)
        {
            SetEntryButtonsInteractable(false);
        }
        else if (entry.triggerButton != null)
        {
            entry.triggerButton.interactable = false;
        }

        StartEntryAudioFadeIn(entry);

        entry.sourcePhoto.SetAsLastSibling();
        ApplyTransitionLayer(entry.sourcePhoto);

        yield return PhotoMemoryPhotoAnimator.AnimateToCenter(
            entry.sourcePhoto,
            entry.initialScale,
            entry.zoomScale,
            Mathf.Max(0f, entry.zoomDuration));
        yield return PhotoMemoryCanvasFx.PlayFlash(whiteFlashOverlay, whiteFlashPeak, flashDuration);

        if (openHoldAfterFlash > 0f)
        {
            yield return new WaitForSeconds(openHoldAfterFlash);
        }

        ReleaseActivePhotoTransitionLayer();

        entry.sourcePhoto.SetAsLastSibling();

        _isOpen = true;
        yield return _overlayController.ShowOverlay(entry.overlaySprite, overlayFadeDuration, entry.overlayOpenScale);
        _overlayController.StartEffect(entry, () => _isOpen && _activeEntry == entry);
        _backButtonPresenter?.SetVisible(true, instant: false);

        _isTransitionRunning = false;
    }

    private IEnumerator CloseSequence()
    {
        _isTransitionRunning = true;
        StartActiveEntryAudioFadeOut();
        StopActiveButtonVfx();
        ReleaseActivePhotoTransitionLayer();

        _backButtonPresenter?.SetVisible(false, instant: false);
        _overlayController.StopEffect();

        yield return PhotoMemoryCanvasFx.PlayFlash(blackFlashOverlay, blackFlashPeak, flashDuration);
        yield return _overlayController.HideOverlay(overlayFadeDuration);

        if (_activeEntry != null && _activeEntry.sourcePhoto != null)
        {
            yield return PhotoMemoryPhotoAnimator.AnimateBack(
                _activeEntry.sourcePhoto,
                _activeEntry.initialAnchoredPosition,
                _activeEntry.initialScale,
                Mathf.Max(0f, closeZoomDuration));

            _activeEntry.sourcePhoto.SetSiblingIndex(_activeEntry.initialSiblingIndex);

            if (!disableAllButtonsWhileOpen && _activeEntry.triggerButton != null)
            {
                _activeEntry.triggerButton.interactable = true;
            }
        }

        if (disableAllButtonsWhileOpen)
        {
            SetEntryButtonsInteractable(true);
        }

        _activeEntry = null;
        _isOpen = false;
        _overlayController.StopEffect();
        _isTransitionRunning = false;
    }

    private void StartButtonVfx(MemoryButtonEntry entry)
    {
        StopActiveButtonVfx();

        if (entry == null || entry.buttonVisualEffectPrefab == null || entry.sourcePhoto == null)
        {
            return;
        }

        _activeButtonVfxInstance = Instantiate(entry.buttonVisualEffectPrefab, entry.sourcePhoto, false);
        Transform fxTransform = _activeButtonVfxInstance.transform;
        fxTransform.localPosition = entry.buttonVfxLocalOffset;
        fxTransform.localRotation = Quaternion.identity;
        fxTransform.localScale = entry.buttonVfxLocalScale;
    }

    private void StopActiveButtonVfx()
    {
        if (_activeButtonVfxInstance != null)
        {
            Destroy(_activeButtonVfxInstance);
            _activeButtonVfxInstance = null;
        }
    }

    private void StartEntryAudioFadeIn(MemoryButtonEntry entry)
    {
        if (entry?.buttonAudioSource == null)
        {
            return;
        }

        if (_audioFadeRoutine != null)
        {
            StopCoroutine(_audioFadeRoutine);
            _audioFadeRoutine = null;
        }

        AudioSource source = entry.buttonAudioSource;
        float targetVolume = Mathf.Clamp01(entry.initialAudioVolume > 0f ? entry.initialAudioVolume : 1f);

        source.volume = 0f;
        if (!source.isPlaying)
        {
            source.Play();
        }

        _audioFadeRoutine = StartCoroutine(FadeAudioVolume(source, 0f, targetVolume, audioFadeInDuration));
    }

    private void StartActiveEntryAudioFadeOut()
    {
        if (_activeEntry?.buttonAudioSource == null)
        {
            return;
        }

        if (_audioFadeRoutine != null)
        {
            StopCoroutine(_audioFadeRoutine);
            _audioFadeRoutine = null;
        }

        AudioSource source = _activeEntry.buttonAudioSource;
        float restoreVolume = Mathf.Clamp01(_activeEntry.initialAudioVolume > 0f ? _activeEntry.initialAudioVolume : 1f);
        _audioFadeRoutine = StartCoroutine(FadeOutAndStopAudio(source, restoreVolume, audioFadeOutDuration));
    }

    private IEnumerator FadeOutAndStopAudio(AudioSource source, float restoreVolume, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        yield return FadeAudioVolume(source, source.volume, 0f, duration);

        source.Stop();
        source.volume = restoreVolume;
        _audioFadeRoutine = null;
    }

    private IEnumerator FadeAudioVolume(AudioSource source, float from, float to, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        float safeDuration = Mathf.Max(0f, duration);
        if (safeDuration <= 0f)
        {
            source.volume = to;
            _audioFadeRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < safeDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / safeDuration);
            source.volume = Mathf.Lerp(from, to, n);
            yield return null;
        }

        source.volume = to;
        _audioFadeRoutine = null;
    }

    private void ApplyTransitionLayer(RectTransform photo)
    {
        ReleaseActivePhotoTransitionLayer();

        if (photo == null)
        {
            return;
        }

        Canvas transitionCanvas = photo.GetComponent<Canvas>();
        _activeTransitionCanvasWasCreated = transitionCanvas == null;

        if (transitionCanvas == null)
        {
            transitionCanvas = photo.gameObject.AddComponent<Canvas>();
        }

        _activeTransitionCanvasOriginalOverrideSorting = transitionCanvas.overrideSorting;
        _activeTransitionCanvasOriginalSortingOrder = transitionCanvas.sortingOrder;

        transitionCanvas.overrideSorting = true;
        transitionCanvas.sortingOrder = short.MaxValue - 10;

        _activeTransitionCanvas = transitionCanvas;
    }

    private void ReleaseActivePhotoTransitionLayer()
    {
        if (_activeTransitionCanvas == null)
        {
            return;
        }

        if (_activeTransitionCanvasWasCreated)
        {
            Destroy(_activeTransitionCanvas);
        }
        else
        {
            _activeTransitionCanvas.overrideSorting = _activeTransitionCanvasOriginalOverrideSorting;
            _activeTransitionCanvas.sortingOrder = _activeTransitionCanvasOriginalSortingOrder;
        }

        _activeTransitionCanvas = null;
        _activeTransitionCanvasWasCreated = false;
    }

    private void SetEntryButtonsInteractable(bool interactable)
    {
        if (memoryButtons == null)
        {
            return;
        }

        for (int i = 0; i < memoryButtons.Length; i++)
        {
            MemoryButtonEntry entry = memoryButtons[i];
            if (entry != null && entry.triggerButton != null)
            {
                entry.triggerButton.interactable = interactable;
            }
        }
    }
}
