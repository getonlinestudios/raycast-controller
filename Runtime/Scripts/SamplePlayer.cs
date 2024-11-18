using UnityEngine;
using UnityEngine.Serialization;

namespace RaycastControllerCore
{
    public class SamplePlayer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float maxJumpHeight = 4f;
        [SerializeField] private float minJumpHeight = 1f;
        [SerializeField] private float timeToJumpApex = 0.4f;
        [SerializeField] private float accelerationTimeAirborne = .2f;
        [SerializeField] private float accelerationTimeGrounded = .1f;
        
        private float _maxJumpVelocity;
        private float _minJumpVelocity;
        private float _gravity;
        private Vector2 _velocity;
        private float _velocityXSmoothing;
    
        private Controller2D _controller2D;

        private void Start()
        {
            _controller2D = GetComponent<Controller2D>();

            if (_controller2D == null)
            {
                Debug.LogError($"No controller was found on {name}. Ensure one is attached to this game object.");
            }

            _gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
            _maxJumpVelocity = Mathf.Abs(_gravity) * timeToJumpApex;
            _minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(_gravity) * minJumpHeight);
            print($"Gravity: {_gravity}, Jump Velocity: {_maxJumpVelocity}.");
        }

        private void Update()
        {
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (Input.GetKeyDown(KeyCode.Space) && _controller2D.CollisionInfo.Below)
            {
                _velocity.y = _maxJumpVelocity;
            }

            if (Input.GetKeyUp(KeyCode.Space) && _velocity.y > _minJumpVelocity)
            {
                _velocity.y = _minJumpVelocity;
            }

            var targetVelocityX = input.x * moveSpeed;
            _velocity.x = Mathf.SmoothDamp(_velocity.x, targetVelocityX, ref _velocityXSmoothing, 
                _controller2D.CollisionInfo.Below ? accelerationTimeGrounded : accelerationTimeAirborne);
            _velocity.x = input.x * moveSpeed;
            _velocity.y += _gravity * Time.deltaTime;
            _controller2D.Move(_velocity * Time.deltaTime, input);
            
            // See this part of the video: https://youtu.be/rVfR14UNNDo?t=745
            if (_controller2D.CollisionInfo.Above || _controller2D.CollisionInfo.Below)
            {
                _velocity.y = 0;
            }
        }
    }
}