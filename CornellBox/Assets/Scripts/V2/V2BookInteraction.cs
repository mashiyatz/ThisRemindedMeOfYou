using UnityEngine;

public class V2BookInteraction : MonoBehaviour
{
    public static event System.Action OnMouseEnterBook;

    private Renderer _renderer;
    private bool _outlineShowing;

    public string bookTitle;
    public string bookAuthor;
    public Sprite bookResponseImageSprite;
    public AudioClip bookResponseAudio;
    public string bookResponseText;
    public string responseImageUrl;
    public string responseAudioUrl;

    [Header("Outline")]
    public Material outlineMaterial;

    // Drag state
    private const float DragThresholdPx = 8f;
    private bool      _isDragging;
    private Vector2   _mouseDownScreenPos;
    private Plane     _dragPlane;
    private Vector3   _dragOffset;

    // Pre-allocated buffer for table raycast checks (shared across all book instances)
    private static readonly RaycastHit[] _raycastBuffer = new RaycastHit[8];

    // How much to shrink the table radius before blocking (0–1). Tweak if books
    // stop too early or hang over the edge.
    private const float TableRadiusFactor = 0.92f;

    // Returns true if pos is over the round wooden table.
    // Raycasts downward to find the table collider, then applies a circular
    // boundary using the collider's XZ half-extent as the table radius — this
    // correctly ignores the square corners of the BoxCollider.
    private static bool IsOverTable(Vector3 pos)
    {
        Vector3 origin = new Vector3(pos.x, pos.y + 2f, pos.z);
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, _raycastBuffer, 4f);
        for (int i = 0; i < count; i++)
        {
            Collider col = _raycastBuffer[i].collider;
            if (col.gameObject.name != "round_wooden_table_01") continue;

            // Circular boundary: distance from table centre in XZ must be within radius
            Vector3 centre = col.bounds.center;
            float radius = Mathf.Min(col.bounds.extents.x, col.bounds.extents.z) * TableRadiusFactor;
            float dx = pos.x - centre.x;
            float dz = pos.z - centre.z;
            return (dx * dx + dz * dz) <= (radius * radius);
        }
        return false;
    }

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
    }

    public void LightUp()
    {
        if (V2PlayerController.currentState == V2PlayerController.PlayerState.BROWSING)
        {
            _renderer.material.SetColor("_EmissionColor", new Color(0.6f, 0.6f, 0.6f, 1));
            ShowOutline();
            V2BookDisplay.OnBookHighlight?.Invoke(bookTitle);
        }
    }

    public void Dim()
    {
        if (V2PlayerController.currentState == V2PlayerController.PlayerState.BROWSING)
        {
            _renderer.material.SetColor("_EmissionColor", Color.black);
            HideOutline();
            V2BookDisplay.OnBookHighlight?.Invoke("");
        }
    }

    public void PlayBookAnimation()
    {
        if (V2PlayerController.currentState == V2PlayerController.PlayerState.BROWSING)
        {
            HideOutline();
            V2PlayerController.timeOfTransition = Time.time;
            _renderer.material.SetColor("_EmissionColor", Color.black);
            V2BookDisplay.OnBookInteract?.Invoke(this);
            V2PlayerController.currentState = V2PlayerController.PlayerState.IN;
        }
    }

    private void ShowOutline()
    {
        if (outlineMaterial == null || _outlineShowing) return;
        var current = _renderer.materials;
        var mats = new Material[current.Length + 1];
        current.CopyTo(mats, 0);
        mats[mats.Length - 1] = outlineMaterial;
        _renderer.materials = mats;
        _outlineShowing = true;
    }

    private void HideOutline()
    {
        if (!_outlineShowing) return;
        var current = _renderer.materials;
        var mats = new Material[current.Length - 1];
        System.Array.Copy(current, mats, mats.Length);
        _renderer.materials = mats;
        _outlineShowing = false;
    }

    private void OnMouseEnter() { OnMouseEnterBook?.Invoke(); LightUp(); }

    private void OnMouseExit()
    {
        // Don't dim while dragging — cursor can leave the collider bounds mid-drag
        if (!_isDragging) Dim();
    }

    private void OnMouseDown()
    {
        _mouseDownScreenPos = Input.mousePosition;
        _isDragging = false;
    }

    private void OnMouseDrag()
    {
        if (V2PlayerController.currentState != V2PlayerController.PlayerState.BROWSING) return;

        if (!_isDragging)
        {
            float dist = Vector2.Distance(Input.mousePosition, _mouseDownScreenPos);
            if (dist < DragThresholdPx) return;

            // Distance threshold crossed — enter drag mode
            _isDragging = true;

            // Build a horizontal plane at the book's current height and compute
            // the grab offset so the book doesn't snap its centre to the cursor
            _dragPlane = new Plane(Vector3.up, transform.position);
            Ray startRay = Camera.main.ScreenPointToRay(_mouseDownScreenPos);
            _dragOffset = _dragPlane.Raycast(startRay, out float startDist)
                ? transform.position - startRay.GetPoint(startDist)
                : Vector3.zero;
        }

        // Move the book along the drag plane, only if the table is beneath the target position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (_dragPlane.Raycast(ray, out float d))
        {
            Vector3 target = ray.GetPoint(d) + _dragOffset;
            if (IsOverTable(target))
                transform.position = target;
        }
    }

    private void OnMouseUp()
    {
        // If no drag happened, treat the press+release as a click
        if (!_isDragging)
            StartCoroutine(DeferredPlay());
        _isDragging = false;
    }

    // Deferred one frame so any UI button's OpenPanel() call (which sets state to
    // SUBMITTING) is processed by EventSystem before PlayBookAnimation() checks it.
    private System.Collections.IEnumerator DeferredPlay()
    {
        yield return null;
        PlayBookAnimation();
    }
}
