using System.Collections.Generic;
using UnityEngine;

public class V2BookScroller : MonoBehaviour
{
    private List<V2BookInteraction> _books;
    private int _index = -1;   // -1 = keyboard navigation inactive

    void OnEnable()  => V2BookInteraction.OnMouseEnterBook += HandleMouseEnter;
    void OnDisable() => V2BookInteraction.OnMouseEnterBook -= HandleMouseEnter;

    void Update()
    {
        if (V2PlayerController.currentState != V2PlayerController.PlayerState.BROWSING) return;

        bool left  = Input.GetKeyDown(KeyCode.LeftArrow);
        bool right = Input.GetKeyDown(KeyCode.RightArrow);

        if (left || right)
        {
            EnsureBooks();
            if (_books.Count == 0) return;

            if (_index >= 0 && _index < _books.Count)
                _books[_index].Dim();

            if (_index < 0)
                _index = right ? 0 : _books.Count - 1;
            else
                _index = right
                    ? (_index + 1) % _books.Count
                    : (_index - 1 + _books.Count) % _books.Count;

            _books[_index].LightUp();
        }

        if (_index >= 0 && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            EnsureBooks();
            if (_index >= 0 && _index < _books.Count)
                _books[_index].PlayBookAnimation();
        }
    }

    private void HandleMouseEnter()
    {
        if (_index >= 0 && _books != null && _index < _books.Count)
            _books[_index].Dim();
        _index = -1;
    }

    private void EnsureBooks()
    {
        if (_books == null || _books.Count == 0)
            _books = new List<V2BookInteraction>(FindObjectsByType<V2BookInteraction>(FindObjectsInactive.Exclude));
    }
}
