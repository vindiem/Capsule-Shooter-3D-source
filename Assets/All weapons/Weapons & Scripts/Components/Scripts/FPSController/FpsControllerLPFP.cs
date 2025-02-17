using System;
using System.Linq;
using UnityEngine;

namespace FPSControllerLPFP
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(AudioSource))]

    public class FpsControllerLPFP : MonoBehaviour
    {
        // --- Arms Settings ---
        [Header("Arms Settings")]
        [SerializeField] private Transform arms;
        [SerializeField] private Vector3 armPosition;

        // --- Audio Clips ---
        [Header("Audio Settings")]
        [SerializeField] private AudioClip walkingSound;
        [SerializeField] private AudioClip runningSound;

        // --- Movement Settings ---
        [Header("Movement Settings")]
        [SerializeField] private float walkingSpeed = 5f;
        [SerializeField] private float runningSpeed = 9f;
        [SerializeField] private float movementSmoothness = 0.125f;
        [SerializeField] private float jumpForce = 35f;

        // --- Look Settings ---
        [Header("Look Settings")]
        [SerializeField] private float mouseSensitivity = 7f;
        [SerializeField] private float rotationSmoothness = 0.05f;
        [SerializeField] private float minVerticalAngle = -90f;
        [SerializeField] private float maxVerticalAngle = 90f;

        // --- Input Settings ---
        [Header("Input Settings")]
        [SerializeField] private FpsInput input;

        // --- Private Variables ---
        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;
        private AudioSource _audioSource;
        private SmoothRotation _rotationX;
        private SmoothRotation _rotationY;
        private SmoothVelocity _velocityX;
        private SmoothVelocity _velocityZ;
        private bool _isGrounded;

        private readonly RaycastHit[] _groundCastResults = new RaycastHit[8];
        private readonly RaycastHit[] _wallCastResults = new RaycastHit[8];

        private void Start()
        {
            // Initialize components
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            _collider = GetComponent<CapsuleCollider>();
            _audioSource = GetComponent<AudioSource>();

            // Assign arms to character and set initial settings
            arms = AssignCharacterCamera();
            _audioSource.clip = walkingSound;
            _audioSource.loop = true;

            // Initialize helpers for smoothing
            _rotationX = new SmoothRotation(RotationXRaw);
            _rotationY = new SmoothRotation(RotationYRaw);
            _velocityX = new SmoothVelocity();
            _velocityZ = new SmoothVelocity();

            Cursor.lockState = CursorLockMode.Locked;
            ValidateRotationRestrictions();
        }

        private Transform AssignCharacterCamera()
        {
            arms.SetPositionAndRotation(transform.position, transform.rotation);
            return arms;
        }

        private void ValidateRotationRestrictions()
        {
            minVerticalAngle = Mathf.Clamp(minVerticalAngle, -90, 90);
            maxVerticalAngle = Mathf.Clamp(maxVerticalAngle, -90, 90);
            if (minVerticalAngle > maxVerticalAngle)
            {
                Debug.LogWarning("Max vertical angle must be greater than min vertical angle. Values swapped.");
                (minVerticalAngle, maxVerticalAngle) = (maxVerticalAngle, minVerticalAngle);
            }
        }

        private void FixedUpdate()
        {
            RotateCameraAndCharacter();
            MoveCharacter();
            _isGrounded = false;
        }

        private void Update()
        {
            arms.position = transform.position + transform.TransformVector(armPosition);
            Jump();
            PlayFootstepSounds();
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

        private void MoveCharacter()
        {
            Vector3 direction = new Vector3(input.Move, 0f, input.Strafe).normalized;
            Vector3 worldDirection = transform.TransformDirection(direction);
            Vector3 velocity = worldDirection * (input.Run ? runningSpeed : walkingSpeed);

            if (CheckCollisionsWithWalls(velocity)) return;

            float smoothX = _velocityX.Update(velocity.x, movementSmoothness);
            float smoothZ = _velocityZ.Update(velocity.z, movementSmoothness);

            Vector3 force = new Vector3(smoothX - _rigidbody.velocity.x, 0f, smoothZ - _rigidbody.velocity.z);
            _rigidbody.AddForce(force, ForceMode.VelocityChange);
        }

        private bool CheckCollisionsWithWalls(Vector3 velocity)
        {
            if (_isGrounded) return false;

            Physics.CapsuleCastNonAlloc(_collider.bounds.center, _collider.bounds.center, _collider.radius, velocity.normalized,
                _wallCastResults, _collider.radius * 0.04f);
            return _wallCastResults.Any(hit => hit.collider != null && hit.collider != _collider);
        }

        private void Jump()
        {
            if (_isGrounded && input.Jump)
            {
                _isGrounded = false;
                _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
            [SerializeField] private string rotateX = "Mouse X";
            [SerializeField] private string rotateY = "Mouse Y";
            [SerializeField] private string move = "Horizontal";
            [SerializeField] private string strafe = "Vertical";
            [SerializeField] private string run = "Fire3";
            [SerializeField] private string jump = "Jump";

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

            public SmoothRotation(float startAngle)
            {
                _current = startAngle;
            }

            public float Update(float target, float smoothTime)
            {
                return _current = Mathf.SmoothDampAngle(_current, target, ref _currentVelocity, smoothTime);
            }

            public float Current { set => _current = value; }
        }

        private class SmoothVelocity
        {
            private float _current;
            private float _currentVelocity;

            public float Update(float target, float smoothTime)
            {
                return _current = Mathf.SmoothDamp(_current, target, ref _currentVelocity, smoothTime);
            }

            public float Current { set => _current = value; }
        }
    }
}
