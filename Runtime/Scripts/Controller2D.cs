using UnityEngine;

namespace RaycastControllerCore
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Controller2D : RaycastObject
    {
        [SerializeField] private int startingFacingDirection;
        
        private const float MaxClimbingAngle = 80f;
        private const float MaxDescendingAngle = 75f;
        private const string ThroughTag = "Through";

        private CollisionInfo _collisionInfo;
        private Vector2 _playerInput;
        public CollisionInfo CollisionInfo => _collisionInfo;

        public void Move(Vector2 deltaMove, bool isStandingOnPlatform = false) =>
            Move(deltaMove, Vector2.zero, isStandingOnPlatform);

        public void Move(Vector2 deltaMove, Vector2 input, bool isStandingOnPlatform = false)
        {
            UpdateRaycastOrigins();
            _collisionInfo.Reset();
            _collisionInfo.VelocityOld = deltaMove;
            _playerInput = input;

            if (deltaMove.x != 0)
            {
                _collisionInfo.FacingDirection = (int)Mathf.Sign(deltaMove.x);
            }

            if (deltaMove.y < 0)
            {
               DescendSlope(ref deltaMove); 
            }

            HandleHorizontalCollisions(ref deltaMove);

            if (deltaMove.y != 0)
            {
                HandleVerticalCollisions(ref deltaMove);
            }

            transform.Translate(deltaMove);

            if (isStandingOnPlatform)
            {
                _collisionInfo.Below = true;
            }
        }

        private void HandleHorizontalCollisions(ref Vector2 targetVelocity)
        {
            var directionX = _collisionInfo.FacingDirection;
            var rayLength = Mathf.Abs(targetVelocity.x) + SkinWidth;

            if (Mathf.Abs(targetVelocity.x) < SkinWidth)
            {
                rayLength = 2 * SkinWidth;
            }
            
            for (var i = 0; i < HorizontalRayCount; i++)
            { 
                var rayOrigin = (directionX == -1) ? RaycastOrigins.bottomLeft : RaycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (HorizontalRaySpacing * i);

                var hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
                Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red); 
                
                if (hit)
                {
                    if (hit.distance == 0)
                    {
                        continue;
                    }
                    
                    var slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                    
                    if (i == 0 && slopeAngle <= MaxClimbingAngle)
                    {
                        if (_collisionInfo.DescendingSlope)
                        {
                            _collisionInfo.DescendingSlope = false;
                            targetVelocity = _collisionInfo.VelocityOld;
                        }
                        var distanceToSlopStart = 0f;
                        if (slopeAngle != _collisionInfo.SlopeAngleOld)
                        {
                            distanceToSlopStart = hit.distance - SkinWidth;
                            targetVelocity.x -= distanceToSlopStart * directionX;
                        }
                        ClimbSlope(ref targetVelocity, slopeAngle);
                        targetVelocity.x += distanceToSlopStart * directionX;
                    }

                    if (!_collisionInfo.ClimbingSlope || slopeAngle > MaxClimbingAngle)
                    {
                        targetVelocity.x = (hit.distance - SkinWidth) * directionX;
                        rayLength = hit.distance;

                        if (_collisionInfo.ClimbingSlope)
                        {
                            targetVelocity.y = Mathf.Tan(_collisionInfo.SlopeAngle * Mathf.Deg2Rad) *
                                               Mathf.Abs(targetVelocity.x);
                        }

                        _collisionInfo.Left = directionX == -1;
                        _collisionInfo.Right = directionX == 1;
                    }
                }
            }
        }

        private void HandleVerticalCollisions(ref Vector2 targetVelocity)
        {
            var directionY = (int) Mathf.Sign(targetVelocity.y);
            var rayLength = Mathf.Abs(targetVelocity.y) + SkinWidth;

            for (var i = 0; i < VerticalRayCount; i++)
            {
                var rayOrigin = (directionY == -1) ? RaycastOrigins.bottomLeft : RaycastOrigins.topLeft;
                rayOrigin += Vector2.right * (VerticalRaySpacing * i + targetVelocity.x);

                var hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
                Debug.DrawRay(rayOrigin, Vector2.up * (directionY * rayLength), Color.red);

                if (hit)
                {
                    if (ThroughTag.Equals(hit.collider.tag))
                    {
                        if (directionY == 1 || hit.distance == 0f || _collisionInfo.FallingThroughPlatform)
                        {
                            continue;
                        }

                        if (_playerInput.y == -1)
                        {
                            _collisionInfo.FallingThroughPlatform = true;
                            Invoke(nameof(ResetFallingThroughPlatform), 0.5f);
                            continue;
                        }
                    }
                    targetVelocity.y = (hit.distance - SkinWidth) * directionY;
                    rayLength = hit.distance;

                    if (_collisionInfo.ClimbingSlope)
                    {
                        targetVelocity.x = targetVelocity.y / Mathf.Tan(_collisionInfo.SlopeAngle * Mathf.Deg2Rad) *
                                           Mathf.Sign(targetVelocity.x);
                    }

                    _collisionInfo.Below = directionY == -1;
                    _collisionInfo.Above = directionY == 1;
                }
            }

            if (_collisionInfo.ClimbingSlope)
            {
                var directionX = (int) Mathf.Sign(targetVelocity.x);
                rayLength = Mathf.Abs(targetVelocity.x) + SkinWidth;
                var rayOrigin = directionX == -1 ? RaycastOrigins.bottomLeft : RaycastOrigins.bottomRight + Vector2.up * targetVelocity.y;
                var hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

                if (hit)
                {
                    var slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                    if (slopeAngle != _collisionInfo.SlopeAngle)
                    {
                        targetVelocity.x = (hit.distance - SkinWidth) * directionX;
                        _collisionInfo.SlopeAngle = slopeAngle;
                    }
                }
            }
        }

        private void ClimbSlope(ref Vector2 targetVelocity, float slopeAngle)
        {
            var moveDistance = Mathf.Abs(targetVelocity.x);
            var climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
            if (targetVelocity.y <= climbVelocityY)
            {
                targetVelocity.y = climbVelocityY;
                targetVelocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(targetVelocity.x);
                _collisionInfo.Below = true;
                _collisionInfo.ClimbingSlope = true;
                _collisionInfo.SlopeAngle = slopeAngle;
            }
        }

        private void DescendSlope(ref Vector2 targetVelocity)
        {
            var directionX = (int) Mathf.Sign(targetVelocity.x);
            var rayOrigin = directionX == -1 ? RaycastOrigins.bottomRight : RaycastOrigins.bottomLeft;
            var hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, collisionMask);
            if (hit)
            {
                var slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= MaxDescendingAngle)
                {
                    if ((int)Mathf.Sign(hit.normal.x) == directionX)
                    {
                        if (hit.distance - SkinWidth <=
                            Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(targetVelocity.x))
                        {
                            var moveDistance = Mathf.Abs(targetVelocity.x);
                            var descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            targetVelocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(targetVelocity.x);
                            targetVelocity.y -= descendVelocityY;

                            _collisionInfo.SlopeAngle = slopeAngle;
                            _collisionInfo.DescendingSlope = true;
                            _collisionInfo.Below = true;
                        }
                    }
                }
            }
        }

        private void ResetFallingThroughPlatform() => _collisionInfo.FallingThroughPlatform = false;

        protected override void Start()
        {
            base.Start();
            _collisionInfo.FacingDirection = startingFacingDirection;
        }
    }
}