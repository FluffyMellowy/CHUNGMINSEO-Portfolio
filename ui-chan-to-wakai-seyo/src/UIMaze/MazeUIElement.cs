using UnityEngine;

namespace UIMaze
{
    public abstract class MazeUIElement : MonoBehaviour
    {
        protected RectTransform _rect;
        public RectTransform Rect => _rect;

        protected virtual void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        protected bool RectOverlaps(RectTransform a, RectTransform b)
        {
            Rect rectA = GetWorldRect(a);
            Rect rectB = GetWorldRect(b);
            return rectA.Overlaps(rectB);
        }

        private Rect GetWorldRect(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return new Rect(
                corners[0].x,
                corners[0].y,
                corners[2].x - corners[0].x,
                corners[2].y - corners[0].y
            );
        }
    }
}