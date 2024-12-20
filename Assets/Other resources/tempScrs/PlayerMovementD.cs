using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerMovementD : MonoBehaviour
{
    public CharacterController controller;
    public Transform GroundCheck;
    public LayerMask GroundMask;

    private PhotonView photonView;

    private float gravity = -20f;
    private float GroundDistance = 0.4f;
    private float moveSpeed = 7.0f;  // Ground move speed
    private float runAcceleration = 14f;   // Ground accel
    private float runDeacceleration = 10f; // Deacceleration on ground
    private float airAcceleration = 2.0f;  // Air accel
    private float airDeacceleration = 2.0f; // Deacceleration in air
    private float jumpSpeed = 8.0f;
    private float friction = 6f;

    private Vector3 playerVelocity;
    private Vector3 moveDirectionNorm;
    private bool JumpQueue = false;
    private bool wishJump = false;

    private float currentspeed;
    private float addspeed;
    private float accelspeed;
    private float control;
    private float drop;
    private Vector3 wishdir;

    [SerializeField] private bool IsGrounded;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        //IsGrounded = Physics.CheckSphere(GroundCheck.position, GroundDistance, GroundMask);

        QueueJump();

        if (controller.isGrounded)
            GroundMove();
        else
            AirMove();

        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void QueueJump()
    {
        if (Input.GetButtonDown("Jump") && IsGrounded)
        {
            wishJump = true;
        }

        if (!IsGrounded && Input.GetButtonDown("Jump"))
        {
            JumpQueue = true;
        }
        if (IsGrounded && JumpQueue)
        {
            wishJump = true;
            JumpQueue = false;
        }
    }

    public void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        currentspeed = Vector3.Dot(playerVelocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;
        accelspeed = accel * Time.deltaTime * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        playerVelocity.x += accelspeed * wishdir.x;
        playerVelocity.z += accelspeed * wishdir.z;
    }

    private void AirMove()
    {
        SetMovementDir();

        wishdir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        wishdir = transform.TransformDirection(wishdir);
        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        float wishspeed = wishdir.magnitude * moveSpeed;

        if (Vector3.Dot(playerVelocity, wishdir) < 0)
            Accelerate(wishdir, wishspeed, airDeacceleration);
        else
            Accelerate(wishdir, wishspeed, airAcceleration);

        playerVelocity.y += gravity * Time.deltaTime;
    }

    private void GroundMove()
    {
        if (!wishJump)
            ApplyFriction(1.0f);
        else
            ApplyFriction(0);

        SetMovementDir();

        wishdir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        wishdir = transform.TransformDirection(wishdir);
        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        float wishspeed = wishdir.magnitude * moveSpeed;

        Accelerate(wishdir, wishspeed, runAcceleration);

        playerVelocity.y = 0;

        if (wishJump)
        {
            playerVelocity.y = jumpSpeed;
            wishJump = false;
        }
    }

    private void ApplyFriction(float t)
    {
        Vector3 vec = playerVelocity;
        vec.y = 0f;
        float speed = vec.magnitude;
        drop = 0f;

        if (controller.isGrounded)
        {
            control = speed < runDeacceleration ? runDeacceleration : speed;
            drop = control * friction * Time.deltaTime * t;
        }

        float newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }

    private void SetMovementDir()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        wishdir = new Vector3(x, 0, z);
    }

    public void SetGroundedState(bool _grounded)
    {
        IsGrounded = _grounded;
    }

}
