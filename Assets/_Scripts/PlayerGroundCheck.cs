using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundCheck : MonoBehaviour
{
    private PlayerMovementD playerMovementD;

    private void Awake()
    {
        playerMovementD = GetComponentInParent<PlayerMovementD>();
    }

    private void OnTriggerEnter(Collider other)
    {
        playerMovementD.SetGroundedState(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject == playerMovementD.gameObject)
        {
            return;
        }
        playerMovementD.SetGroundedState(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject == playerMovementD.gameObject)
        {
            return;
        }
        playerMovementD.SetGroundedState(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        playerMovementD.SetGroundedState(true);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == playerMovementD.gameObject)
        {
            return;
        }
        playerMovementD.SetGroundedState(false);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject == playerMovementD.gameObject)
        {
            return;
        }
        playerMovementD.SetGroundedState(true);
    }
}
