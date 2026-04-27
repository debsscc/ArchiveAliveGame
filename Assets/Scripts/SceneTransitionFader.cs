using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionFader : MonoBehaviour
{
	[Header("Fade")]
	[SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
	[SerializeField, Min(0f)] private float fadeInDuration = 6f;
	[SerializeField] private Color fadeColor = Color.black;
	[SerializeField] private bool fadeInOnStartup = true;

	private static SceneTransitionFader _instance;

	private Canvas _canvas;
	private CanvasGroup _canvasGroup;
	private Image _fadeImage;
	private Coroutine _transitionRoutine;
	private Coroutine _audioFadeRoutine;
	private bool _isTransitioning;
	private bool _hasPlayedStartupFade;
	private bool _pendingSceneFadeIn;
	private bool _isSceneLoadedSubscribed;
	private AudioSource _registeredAudioSource;
	private float _audioSourceOriginalVolume;

	public static SceneTransitionFader Instance
	{
		get
		{
			EnsureInstance();
			return _instance;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		EnsureInstance();
	}

	public void RegisterAudioSource(AudioSource audioSource)
	{
		_registeredAudioSource = audioSource;
		if (_registeredAudioSource != null)
		{
			_audioSourceOriginalVolume = _registeredAudioSource.volume;
		}
	}

	public void LoadScene(string sceneName)
	{
		if (string.IsNullOrWhiteSpace(sceneName))
		{
			Debug.LogWarning("SceneTransitionFader: scene name is empty.", this);
			return;
		}

		if (_isTransitioning)
		{
			return;
		}

		_transitionRoutine = StartCoroutine(LoadSceneRoutine(sceneName));
	}

	private static void EnsureInstance()
	{
		if (_instance != null)
		{
			return;
		}

		SceneTransitionFader existingInstance = FindAnyObjectByType<SceneTransitionFader>();
		if (existingInstance != null)
		{
			existingInstance.InitializeSingleton();
			return;
		}

		GameObject root = new GameObject(nameof(SceneTransitionFader));
		_instance = root.AddComponent<SceneTransitionFader>();
		_instance.InitializeSingleton();
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
			return;
		}

		InitializeSingleton();
	}

	private void Start()
	{
		if (_hasPlayedStartupFade || !fadeInOnStartup)
		{
			SetOverlayAlpha(0f);
			return;
		}

		_hasPlayedStartupFade = true;
		_transitionRoutine = StartCoroutine(FadeRoutine(1f, 0f, fadeInDuration));
	}

	private void InitializeSingleton()
	{
		if (_instance == null)
		{
			_instance = this;
		}

		if (!_isSceneLoadedSubscribed)
		{
			SceneManager.sceneLoaded += HandleSceneLoaded;
			_isSceneLoadedSubscribed = true;
		}

		DontDestroyOnLoad(gameObject);
		EnsureOverlay();
	}

	private void OnDestroy()
	{
		if (_isSceneLoadedSubscribed)
		{
			SceneManager.sceneLoaded -= HandleSceneLoaded;
			_isSceneLoadedSubscribed = false;
		}
	}

	private void EnsureOverlay()
	{
		if (_canvas == null)
		{
			_canvas = gameObject.GetComponent<Canvas>();
			if (_canvas == null)
			{
				_canvas = gameObject.AddComponent<Canvas>();
			}

			_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			_canvas.sortingOrder = short.MaxValue;
		}

		if (gameObject.GetComponent<CanvasScaler>() == null)
		{
			gameObject.AddComponent<CanvasScaler>();
		}

		if (gameObject.GetComponent<GraphicRaycaster>() == null)
		{
			gameObject.AddComponent<GraphicRaycaster>();
		}

		if (_canvasGroup == null)
		{
			_canvasGroup = gameObject.GetComponent<CanvasGroup>();
			if (_canvasGroup == null)
			{
				_canvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
		}

		if (_fadeImage == null)
		{
			Transform imageTransform = transform.Find("FadeImage");
			if (imageTransform == null)
			{
				GameObject imageObject = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
				imageTransform = imageObject.transform;
				imageTransform.SetParent(transform, false);
			}

			_fadeImage = imageTransform.GetComponent<Image>();
		}

		RectTransform imageRect = _fadeImage.rectTransform;
		imageRect.anchorMin = Vector2.zero;
		imageRect.anchorMax = Vector2.one;
		imageRect.offsetMin = Vector2.zero;
		imageRect.offsetMax = Vector2.zero;

		_fadeImage.color = fadeColor;
		_fadeImage.raycastTarget = true;

		if (!_hasPlayedStartupFade && fadeInOnStartup)
		{
			SetOverlayAlpha(1f);
		}
		else if (!_isTransitioning)
		{
			SetOverlayAlpha(0f);
		}
	}

	private IEnumerator LoadSceneRoutine(string sceneName)
	{
		_isTransitioning = true;

		if (_audioFadeRoutine != null)
		{
			StopCoroutine(_audioFadeRoutine);
		}

		_audioFadeRoutine = StartCoroutine(FadeAudioVolume(_registeredAudioSource, _audioSourceOriginalVolume, 0f, fadeOutDuration));

		yield return FadeRoutine(_canvasGroup.alpha, 1f, fadeOutDuration);
		yield return new WaitForSecondsRealtime(0.05f);

		_pendingSceneFadeIn = true;
		AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
		if (loadOperation == null)
		{
			_pendingSceneFadeIn = false;
			_isTransitioning = false;
			yield return FadeRoutine(_canvasGroup.alpha, 0f, fadeInDuration);
			_transitionRoutine = null;
			yield break;
		}

		while (!loadOperation.isDone)
		{
			yield return null;
		}

		_transitionRoutine = null;
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
	{
		EnsureOverlay();

		if (!_pendingSceneFadeIn)
		{
			if (!_isTransitioning)
			{
				SetOverlayAlpha(0f);
			}

			return;
		}

		_pendingSceneFadeIn = false;

		if (_transitionRoutine != null)
		{
			StopCoroutine(_transitionRoutine);
		}

		_transitionRoutine = StartCoroutine(FadeInAfterSceneLoad());
	}

	private IEnumerator FadeInAfterSceneLoad()
	{
		yield return null;

		if (_audioFadeRoutine != null)
		{
			StopCoroutine(_audioFadeRoutine);
		}

		_audioFadeRoutine = StartCoroutine(FadeAudioVolume(_registeredAudioSource, 0f, _audioSourceOriginalVolume, fadeInDuration));

		yield return FadeRoutine(_canvasGroup.alpha, 0f, fadeInDuration);
		SetOverlayAlpha(0f);
		_isTransitioning = false;
		_transitionRoutine = null;
	}

	private IEnumerator FadeRoutine(float from, float to, float duration)
	{
		SetOverlayAlpha(from);

		if (duration <= 0f)
		{
			SetOverlayAlpha(to);
			yield break;
		}

		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float normalized = Mathf.Clamp01(elapsed / duration);
			SetOverlayAlpha(Mathf.Lerp(from, to, normalized));
			yield return null;
		}

		SetOverlayAlpha(to);
	}

	private IEnumerator FadeAudioVolume(AudioSource audioSource, float from, float to, float duration)
	{
		if (audioSource == null)
		{
			yield break;
		}

		if (duration <= 0f)
		{
			audioSource.volume = to;
			_audioFadeRoutine = null;
			yield break;
		}

		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float normalized = Mathf.Clamp01(elapsed / duration);
			audioSource.volume = Mathf.Lerp(from, to, normalized);
			yield return null;
		}

		audioSource.volume = to;
		_audioFadeRoutine = null;
	}

	private void SetOverlayAlpha(float alpha)
	{
		if (_canvasGroup == null)
		{
			return;
		}

		float clampedAlpha = Mathf.Clamp01(alpha);
		_canvasGroup.alpha = clampedAlpha;
		_canvasGroup.blocksRaycasts = clampedAlpha > 0.001f;
		_canvasGroup.interactable = false;
	}

	private void OnValidate()
	{
		if (_fadeImage != null)
		{
			_fadeImage.color = fadeColor;
		}
	}
}