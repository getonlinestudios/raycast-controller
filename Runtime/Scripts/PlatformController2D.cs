using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaycastControllerCore
{
    internal struct PassengerMoveInfo
    {
        public Transform Transform;
        public Vector2 RequiredVelocity;
        public bool IsStandingOnPlatform;
        public bool RequiresMovementBeforePlatform;

        public PassengerMoveInfo(
            Transform transform, 
            Vector2 requiredVelocity, 
            bool isStandingOnPlatform,
            bool requiresMovementBeforePlatform)
        {
            Transform = transform;
            RequiredVelocity = requiredVelocity;
            IsStandingOnPlatform = isStandingOnPlatform;
            RequiresMovementBeforePlatform = requiresMovementBeforePlatform;
        }
    }
    
    public class PlatformController2D : RaycastObject
    {
        [SerializeField] private LayerMask passengerMask;
        [SerializeField] private Vector2[] localWayPoints;
        [SerializeField] private float platformSpeed = 5f;
        [SerializeField] private float waitTime = 1.5f;
        [SerializeField] [Range(0f, 2f)] private float easeAmount = 2.1f;
        [SerializeField] private bool isCyclic;
        
        
        private List<PassengerMoveInfo> _passengers;
        private readonly Dictionary<Transform, Controller2D> _passengerCache = new();
        private Vector2[] _globalWayPoints;
        private int _previousWayPointIndex;
        private float _percentBetweenWayPoints;
        private float _nextMoveTime;

        private void MovePassengers(bool beforeMovePlatform)
        {
            foreach (var passenger in _passengers)
            {
                if (!_passengerCache.ContainsKey(passenger.Transform))
                {
                   _passengerCache.Add(passenger.Transform, passenger.Transform.GetComponent<Controller2D>()); 
                }
                
                if (passenger.RequiresMovementBeforePlatform == beforeMovePlatform)
                {
                    _passengerCache[passenger.Transform]
                        .Move(passenger.RequiredVelocity, isStandingOnPlatform: passenger.IsStandingOnPlatform);
                }
            }
        }

        private float Ease(float x)
        {
            var a = 1f + easeAmount;
            return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
        }

        private Vector2 CalculatePlatformMovement()
        {
            if (Time.time < _nextMoveTime)
            {
                return Vector2.zero;
            }
            
            _previousWayPointIndex %= _globalWayPoints.Length;
            var nextWayPointIndex = (_previousWayPointIndex + 1) % _globalWayPoints.Length;
            var distanceBetweenWayPoints = Vector2.Distance(
                _globalWayPoints[_previousWayPointIndex],
                _globalWayPoints[nextWayPointIndex]);
            _percentBetweenWayPoints += Time.deltaTime * platformSpeed / distanceBetweenWayPoints;
            _percentBetweenWayPoints = Mathf.Clamp01(_percentBetweenWayPoints);

            var easedPercentBetweenWayPoints = Ease(_percentBetweenWayPoints);

            var nextPosition = Vector2.Lerp(
                _globalWayPoints[_previousWayPointIndex],
                _globalWayPoints[nextWayPointIndex],
                easedPercentBetweenWayPoints);

            if (_percentBetweenWayPoints >= 1)
            {
                _percentBetweenWayPoints = 0;
                _previousWayPointIndex++;
                if (!isCyclic)
                {
                    if (_previousWayPointIndex >= _globalWayPoints.Length - 1)
                    {
                        _previousWayPointIndex = 0;
                        Array.Reverse(_globalWayPoints);
                    }
                }

                _nextMoveTime = Time.time + waitTime;
            }

            return nextPosition - (Vector2) transform.position;
        }

        /// <summary>
        /// Moves the passengers along with the moving platform. This object will
        /// only move objects that are <see cref="RaycastObject"/>.
        /// </summary>
        /// <param name="targetPassengerVelocity">How fast the platform must move its passengers.</param>
        private void CalculatePassengerMovement(Vector2 targetPassengerVelocity)
        {
            var movedPassengers = new HashSet<Transform>();
            _passengers = new List<PassengerMoveInfo>();
            
            var directionX = (int)Mathf.Sign(targetPassengerVelocity.x);
            var directionY = (int)Mathf.Sign(targetPassengerVelocity.y);
            
            // Vertically moving platform
            if (targetPassengerVelocity.y != 0f)
            {
                var rayLength = Mathf.Abs(targetPassengerVelocity.y) + SkinWidth;
                for (var i = 0; i < VerticalRayCount; i++)
                {
                    var rayOrigin = (directionY == -1) ? RaycastOrigins.bottomLeft : RaycastOrigins.topLeft;
                    rayOrigin += Vector2.right * (VerticalRaySpacing * i);

                    var hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);
                    if (hit && hit.distance != 0)
                    {
                        if (!movedPassengers.Contains(hit.transform))
                        {
                            movedPassengers.Add(hit.transform);
                            
                            var pushX = directionY == 1 ? targetPassengerVelocity.x : 0;
                            var pushY = targetPassengerVelocity.y - (hit.distance - SkinWidth) * directionY;

                            _passengers.Add(new PassengerMoveInfo(
                                hit.transform,
                                new Vector2(pushX, pushY),
                                directionY == 1,
                                true));
                        }
                    }
                }
            }
            
            // Horizontally moving platform
            if (targetPassengerVelocity.x != 0f)
            {
                var rayLength = Mathf.Abs(targetPassengerVelocity.x) + SkinWidth;

                for (var i = 0; i < HorizontalRayCount; i++)
                {
                    var rayOrigin = (directionX == -1) ? RaycastOrigins.bottomLeft : RaycastOrigins.bottomRight;
                    rayOrigin += Vector2.up * (HorizontalRaySpacing * i);

                    var hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);
                    if (hit && hit.distance != 0)
                    {
                        if (!movedPassengers.Contains(hit.transform))
                        {
                            movedPassengers.Add(hit.transform);
                            
                            var pushX = targetPassengerVelocity.x - (hit.distance - SkinWidth) * directionX;
                            const float pushY = -SkinWidth;

                            _passengers.Add(new PassengerMoveInfo(
                                hit.transform,
                                new Vector2(pushX, pushY),
                                false,
                                true));
                        }
                    }
                }
            }
            
            // Passenger on top of platform
            if (directionY == -1 || targetPassengerVelocity.y == 0 && targetPassengerVelocity.x != 0)
            {
                var rayLength = SkinWidth * 2;
                for (var i = 0; i < VerticalRayCount; i++)
                {
                    var rayOrigin = RaycastOrigins.topLeft + Vector2.right * (VerticalRaySpacing * i);

                    var hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);
                    if (hit && hit.distance != 0)
                    {
                        if (!movedPassengers.Contains(hit.transform))
                        {
                            movedPassengers.Add(hit.transform);

                            var pushX = targetPassengerVelocity.x;
                            var pushY = targetPassengerVelocity.y;

                            _passengers.Add(new PassengerMoveInfo(
                                hit.transform,
                                new Vector2(pushX, pushY),
                                true,
                                false));
                        }
                    }
                }
            }
        }

        protected override void Start()
        {
            base.Start();

            _globalWayPoints = new Vector2[localWayPoints.Length];
            for (var i = 0; i < localWayPoints.Length; i++)
            {
                _globalWayPoints[i] = localWayPoints[i] + (Vector2) transform.position;
            }
        }

        private void Update()
        {
            UpdateRaycastOrigins();
            
            var velocity = CalculatePlatformMovement();
            CalculatePassengerMovement(velocity);

            MovePassengers(true);
            transform.Translate(velocity);
            MovePassengers(false);
        }

        private void OnDrawGizmos()
        {
            if (localWayPoints == null)
            {
                return;
            }
            
            Gizmos.color = Color.red;
            const float size = 0.3f;
            for (var i = 0; i < localWayPoints.Length; i++)
            {
                var point = localWayPoints[i];
                var globalWayPointPosition = Application.isPlaying 
                    ? _globalWayPoints[i]  
                    : localWayPoints[i] + (Vector2)transform.position;
                
                Gizmos.DrawLine(globalWayPointPosition - Vector2.up * size, globalWayPointPosition + Vector2.up * size);
                Gizmos.DrawLine(globalWayPointPosition - Vector2.left * size,
                    globalWayPointPosition + Vector2.left * size);
            }
        }
    }
}