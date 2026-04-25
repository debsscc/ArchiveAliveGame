using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ArtworkDisplay : MonoBehaviour, IArtworkDisplay
{
    [Header("Artwork")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private Sprite[] artworks;

    [Header("Zoom")]
    [SerializeField] private float zoomScale = 3f;
    [SerializeField] private float zoomDuration = 0.5f;

    private RectTransform _artworkRect;
    private Vector3 _initialScale;

    public int CurrentArtworkIndex { get; private set; }
    public Image ArtworkImage => artworkImage;

    private void Awake()
    {
        if (artworkImage == null)
        {
            Debug.LogError("ArtworkDisplay: Artwork Image is not assigned.", this);
            enabled = false;
            return;
        }

        _artworkRect = artworkImage.rectTransform;
        _initialScale = _artworkRect.localScale;

        if (artworks != null && artworks.Length > 0)
        {
            CurrentArtworkIndex = Mathf.Clamp(CurrentArtworkIndex, 0, artworks.Length - 1);
            artworkImage.sprite = artworks[CurrentArtworkIndex];
        }
    }

    public IEnumerator ZoomIn()
    {
        yield return AnimateZoom(_initialScale, _initialScale * zoomScale, zoomDuration);
    }

    public IEnumerator ZoomOut()
    {
        yield return AnimateZoom(_artworkRect.localScale, _initialScale, zoomDuration);
    }

    public void ShowNextArtwork()
    {
        if (artworks == null || artworks.Length == 0)
        {
            return;
        }

        CurrentArtworkIndex = (CurrentArtworkIndex + 1) % artworks.Length;
        artworkImage.sprite = artworks[CurrentArtworkIndex];
    }

    private IEnumerator AnimateZoom(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            _artworkRect.localScale = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            _artworkRect.localScale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        _artworkRect.localScale = to;
    }
}
