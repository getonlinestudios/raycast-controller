using UnityEngine;

namespace RaycastControllerCore
{
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class RaycastObject : MonoBehaviour
    {
        [SerializeField] protected LayerMask collisionMask;
        
        protected int HorizontalRayCount;
        protected int VerticalRayCount;

        protected const float SkinWidth = 0.015f;
        protected const float DistanceBetweenRays = 0.25f;

        protected float HorizontalRaySpacing;
        protected float VerticalRaySpacing;
        
        private BoxCollider2D _collider2D;
        protected RaycastOrigins RaycastOrigins;
        
        private void CalculateRaySpacing()
        {
            var bounds = _collider2D.bounds;
            bounds.Expand(SkinWidth * -2);

            var boundsWidth = bounds.size.x;
            var boundsHeight = bounds.size.y;

            HorizontalRayCount = Mathf.RoundToInt(boundsHeight / DistanceBetweenRays);
            VerticalRayCount = Mathf.RoundToInt(boundsWidth / DistanceBetweenRays);

            HorizontalRaySpacing = bounds.size.y / (HorizontalRayCount - 1);
            VerticalRaySpacing = bounds.size.x / (VerticalRayCount - 1);
        }

        protected void UpdateRaycastOrigins()
        {
            var bounds = _collider2D.bounds;
            bounds.Expand(SkinWidth * -2);

            RaycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
            RaycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
            RaycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
            RaycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
        }
        
        protected virtual void Start()
        {
            if (!Physics2D.autoSyncTransforms)
            {
                Physics2D.autoSyncTransforms = true;
            }
            _collider2D = GetComponent<BoxCollider2D>();
            CalculateRaySpacing();
        }
    }
}