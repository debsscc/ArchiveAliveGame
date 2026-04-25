using UnityEngine;
using UnityEngine.UI;

public class InteractionButtonManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private IArtworkDisplay artworkDisplay;
    [SerializeField] private Buttons orchestrator;

    [Header("Buttons")]
    [SerializeField] private Button[] interactionButtons;

    private int _lastDisplayedIndex = -1;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        InitializeButtons();
        UpdateButtonVisibility();
    }

    private void Update()
    {
        if (_lastDisplayedIndex != ((artworkDisplay as MonoBehaviour)?.GetComponent<ArtworkDisplay>()?.CurrentArtworkIndex ?? -1))
        {
            UpdateButtonVisibility();
        }
    }

    private void InitializeButtons()
    {
        if (interactionButtons == null || interactionButtons.Length == 0)
        {
            return;
        }

        for (int i = 0; i < interactionButtons.Length; i++)
        {
            if (interactionButtons[i] != null)
            {
                int index = i;
                interactionButtons[i].onClick.AddListener(() => OnButtonClicked(index));
            }
        }
    }

    private void UpdateButtonVisibility()
    {
        if (interactionButtons == null || interactionButtons.Length == 0)
        {
            return;
        }

        ArtworkDisplay artworkDisplayComponent = (artworkDisplay as MonoBehaviour)?.GetComponent<ArtworkDisplay>();
        if (artworkDisplayComponent == null)
        {
            return;
        }

        int currentIndex = artworkDisplayComponent.CurrentArtworkIndex;
        _lastDisplayedIndex = currentIndex;

        for (int i = 0; i < interactionButtons.Length; i++)
        {
            if (interactionButtons[i] != null)
            {
                bool isActive = (i == currentIndex);
                interactionButtons[i].gameObject.SetActive(isActive);
            }
        }
    }

    private void OnButtonClicked(int buttonIndex)
    {
        if (orchestrator != null)
        {
            orchestrator.OnArtworkClicked();
        }
    }

    private void ValidateReferences()
    {
        if (artworkDisplay == null || orchestrator == null)
        {
            Debug.LogError("InteractionButtonManager: Assign ArtworkDisplay (via artworkDisplayBehaviour) and Buttons orchestrator.", this);
            enabled = false;
        }
    }
}
