using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Dave MovementLab - BoostPad
///
// Content:
/// - boosting the PlayerParent in a specific direction
/// 
// Note:
/// This code is not fully optimized, I'll continue to work on it in the future


public class BoostPad_MLab : MonoBehaviour
{
    [Header("Boosting")]
    public bool normalBoosting = true; // when active, PlayerParent gets boosted into a specific direction
    public Vector3 boostDirection;
    public float boostForce;

    public bool localBoosting = false; // when active, PlayerParent gets boosted relative to where he is looking
    public float boostLocalForwardForce;
    public float boostLocalUpwardForce;

    public float boostDuration = 1f;

    private PlayerMovement pm = null;

    /// this function is called if your boost pad has a collider set to "trigger" 
    /// and an object (for example the PlayerParent) moves inside this trigger
    private void OnTriggerEnter(Collider other)
    {
        AddForce(other);
    }

    /// this function is called if your boost pad has a normal collider
    /// and an object (for example the PlayerParent) touches this collider
    private void OnCollisionEnter(Collision collision)
    {
        AddForce(collision.collider);
    }

    private void AddForce(Collider other)
    {
        // first check if the other object is the PlayerParent
        if (other.GetComponentInParent<PlayerMovement>() != null)
        {
            // get a reference to the PlayerMovement script
            pm = other.GetComponentInParent<PlayerMovement>();

            // this causes the PlayerMovement script to enter MovementMode.unlimited -> speed will no longer be limited
            pm.UnlimitedSpeed = true;

            // get the rigidbody component of the PlayerParent
            Rigidbody rb = pm.GetComponent<Rigidbody>();

            // boost the PlayerParent into the boostDirection
            if (normalBoosting)
                rb.AddForce(boostDirection.normalized * boostForce, ForceMode.Impulse);

            // boost the PlayerParent relative to where he is looking
            if (localBoosting)
            {
                // calculate the direction the PlayerParent is looking multiplied with the boostLocalForward force
                Vector3 localBoostedDirection = pm.Orientation.forward * boostLocalForwardForce + pm.Orientation.up * boostLocalUpwardForce;
                rb.AddForce(localBoostedDirection, ForceMode.Impulse);
            }

            // deactivate the boost once the boostDuration is over
            Invoke(nameof(DeactivateBoost), boostDuration);
        }
    }

    private void DeactivateBoost()
    {
        // turn off the unlimited speed of the PlayerParent
        pm.UnlimitedSpeed = false;
    }


    /// visualize the boost direction of the boost pad
    #region Gizmo Visualisations

    private void OnDrawGizmosSelected()
    {
        if (!normalBoosting) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + boostDirection);
    }

    #endregion
}
