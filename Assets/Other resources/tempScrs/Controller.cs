using System;
using System.Linq;
using UnityEngine;

namespace FPSController
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(AudioSource))]

    public class Controller : MonoBehaviour
    {
        // --- Arms and Audio ---
        [Header("Arms Settings")]
        [SerializeField] private Transform arms;
        [SerializeField] private Vector3 armPosition;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip walkingSound;
        [SerializeField] private AudioClip runningSound;

        // --- Movement and Jump ---
        [Header("Movement Settings")]
        [SerializeField] private float walkingSpeed = 5f;
        [SerializeField] private float runningSpeed = 9f;
        [SerializeField] private float jumpForce = 8.0f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float airAcceleration = 2.0f;
        [SerializeField] private float sideStrafeSpeed = 1f;

        [Header("Ground Settings")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundDistance = 0.4f;

        // --- Look Settings ---
        [Header("Look Settings")]
        [SerializeField] private float mouseSensitivity = 7f;
        [SerializeField] private float rotationSmoothness = 0.05f;
        [SerializeField] private float minVerticalAngle = -90f;
        [SerializeField] private float maxVerticalAngle = 90f;

        // --- Input ---
        [Header("Input Settings")]
        [SerializeField] private FpsInput input;

        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;
        private AudioSource _audioSource;
        private SmoothRotation _rotationX;
        private SmoothRotation _rotationY;

        private Vector3 _playerVelocity;
        private bool _isGrounded;
        private bool _wishJump;
        private bool _jumpQueue;

        private void Start()
        {
            // Initialize components
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            _collider = GetComponent<CapsuleCollider>();
            _audioSource = GetComponent<AudioSource>();

            arms = AssignCharacterCamera();
            _audioSource.clip = walkingSound;
            _audioSource.loop = true;

            _rotationX = new SmoothRotation(RotationXRaw);
            _rotationY = new SmoothRotation(RotationYRaw);

            Cursor.lockState = CursorLockMode.Locked;
        }

        private Transform AssignCharacterCamera()
        {
            arms.SetPositionAndRotation(transform.position, transform.rotation);
            return arms;
        }

        private void Update()
        {
            arms.position = transform.position + transform.TransformVector(armPosition);
            RotateCameraAndCharacter();
            JumpLogic();
            PlayFootstepSounds();
        }

        private void FixedUpdate()
        {
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            if (_isGrounded)
            {
                GroundMove();
            }
            else
            {
                AirMove();
            }

            // Apply gravity
            _playerVelocity.y += gravity * Time.fixedDeltaTime;
            _rigidbody.velocity = _playerVelocity;
        }

        private void RotateCameraAndCharacter()
        {
            float rotationX = _rotationX.Update(RotationXRaw, rotationSmoothness);
            float rotationY = _rotationY.Update(RotationYRaw, rotationSmoothness);
            float clampedY = Mathf.Clamp(rotationY, minVerticalAngle, maxVerticalAngle);

            _rotationY.Current = clampedY;

            Quaternion rotation = Quaternion.Euler(0, rotationX, 0);
            transform.eulerAngles = new Vector3(0, rotation.eulerAngles.y, 0);
            arms.rotation = Quaternion.Euler(clampedY, rotation.eulerAngles.y, 0);
        }

        private float RotationXRaw => input.RotateX * mouseSensitivity;
        private float RotationYRaw => input.RotateY * mouseSensitivity;

        private void GroundMove()
        {
            Vector3 inputDirection = new Vector3(input.Move, 0, input.Strafe).normalized;
            Vector3 moveDirection = transform.TransformDirection(inputDirection) * walkingSpeed;

            _playerVelocity.x = moveDirection.x;
            _playerVelocity.z = moveDirection.z;

            if (_wishJump)
            {
                _playerVelocity.y = jumpForce;
                _wishJump = false;
            }
        }

        private void AirMove()
        {
            Vector3 inputDirection = new Vector3(input.Move, 0, input.Strafe);
            Vector3 moveDirection = transform.TransformDirection(inputDirection);

            float wishSpeed = moveDirection.magnitude * sideStrafeSpeed;
            Accelerate(moveDirection.normalized, wishSpeed, airAcceleration);
        }

        private void Accelerate(Vector3 direction, float targetSpeed, float acceleration)
        {
            float currentSpeed = Vector3.Dot(_playerVelocity, direction);
            float addSpeed = targetSpeed - currentSpeed;

            if (addSpeed <= 0) return;

            float accelSpeed = acceleration * Time.fixedDeltaTime * targetSpeed;
            accelSpeed = Mathf.Min(accelSpeed, addSpeed);

            _playerVelocity.x += accelSpeed * direction.x;
            _playerVelocity.z += accelSpeed * direction.z;
        }

        private void JumpLogic()
        {
            if (_isGrounded && input.Jump)
            {
                _wishJump = true;
            }

            if (!_isGrounded && input.Jump)
            {
                _jumpQueue = true;
            }

            if (_isGrounded && _jumpQueue)
            {
                _wishJump = true;
                _jumpQueue = false;
            }
        }

        private void PlayFootstepSounds()
        {
            if (_isGrounded && _rigidbody.velocity.sqrMagnitude > 0.1f)
            {
                _audioSource.clip = input.Run ? runningSound : walkingSound;
                if (!_audioSource.isPlaying) _audioSource.Play();
            }
            else if (_audioSource.isPlaying)
            {
                _audioSource.Pause();
            }
        }

        [Serializable]
        private class FpsInput
        {
            public string rotateX = "Mouse X";
            public string rotateY = "Mouse Y";
            public string move = "Horizontal";
            public string strafe = "Vertical";
            public string run = "Fire3";
            public string jump = "Jump";

            public float RotateX => Input.GetAxisRaw(rotateX);
            public float RotateY => Input.GetAxisRaw(rotateY);
            public float Move => Input.GetAxisRaw(move);
            public float Strafe => Input.GetAxisRaw(strafe);
            public bool Run => Input.GetButton(run);
            public bool Jump => Input.GetButtonDown(jump);
        }

        private class SmoothRotation
        {
            private float _current;
            private float _currentVelocity;

            public SmoothRotation(float startAngle) => _current = startAngle;

            public float Update(float target, float smoothTime)
            {
                return _current = Mathf.SmoothDampAngle(_current, target, ref _currentVelocity, smoothTime);
            }

            public float Current { set => _current = value; }
        }
    }
}
