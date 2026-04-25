using System.Collections;

public interface IArtworkDisplay
{
    int CurrentArtworkIndex { get; }
    IEnumerator ZoomIn();
    IEnumerator ZoomOut();
    void ShowNextArtwork();
}
