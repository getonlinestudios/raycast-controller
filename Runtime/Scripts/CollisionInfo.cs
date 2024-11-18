using UnityEngine;

namespace RaycastControllerCore
{
    public struct CollisionInfo
    {
        public bool Above;
        public bool Below;
        public bool Left;
        public bool Right;
        public bool ClimbingSlope;
        public bool DescendingSlope;
        public float SlopeAngle;
        public float SlopeAngleOld;
        public Vector2 VelocityOld;
        public int FacingDirection;
        public bool FallingThroughPlatform;

        public void Reset()
        {
            Above = false;
            Below = false;
            Right = false;
            Left = false;
            ClimbingSlope = false;
            DescendingSlope = false;
            SlopeAngleOld = SlopeAngle;
            SlopeAngle = 0f;
        }
    }
}