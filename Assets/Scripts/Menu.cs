using UnityEngine;

public class Menu : MonoBehaviour
{
	[Header("Scene Navigation")]
	[SerializeField] private string gameSceneName = "Game";
	[SerializeField] private AudioSource audioSource;

	private void Awake()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}

		SceneTransitionFader.Instance.RegisterAudioSource(audioSource);
	}

	public void OnGameButtonPressed()
	{
		if (string.IsNullOrWhiteSpace(gameSceneName))
		{
			Debug.LogWarning("Game scene name is empty.", this);
			return;
		}

		SceneTransitionFader.Instance.LoadScene(gameSceneName);
	}

	public void OnExitButtonPressed()
	{
		Application.Quit();

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}
