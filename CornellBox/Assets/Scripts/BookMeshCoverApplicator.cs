using UnityEngine;

/// <summary>
/// Applies a book cover texture fetched by BookCoverFetcher to a 3D mesh.
/// Uses BookCoverTextureComposer to composite the fetched image into the
/// front cover region of the UV atlas, so it wraps correctly onto the book.
///
/// Attach to any GameObject; wire BookCoverFetcher, MeshRenderer, and
/// MeshFilter in the Inspector.
/// </summary>
public class BookMeshCoverApplicator : MonoBehaviour
{
    [SerializeField] private BookCoverFetcher fetcher;
    [SerializeField] private MeshRenderer bookMesh;
    [SerializeField] private MeshFilter bookMeshFilter;

    private void Start()
    {
        fetcher.OnCoverFetched += ApplyCoverTexture;
    }

    private void OnDestroy()
    {
        fetcher.OnCoverFetched -= ApplyCoverTexture;
    }

    private void ApplyCoverTexture(Texture2D texture)
    {
        Mesh mesh = bookMeshFilter.sharedMesh;
        Texture2D atlas = BookCoverTextureComposer.BuildAtlas(texture, mesh);
        bookMesh.material.mainTexture = atlas;
    }
}
