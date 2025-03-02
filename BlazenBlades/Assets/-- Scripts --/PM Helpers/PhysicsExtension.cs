using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhysicsExtensions
{
    public static class PhysicsExtension
    {
        public static Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
        {
            float gravity = Physics.gravity.y;
            float displacementY = endPoint.y - startPoint.y;
            Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);
            
            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
            
            //if velovityY.y is 0, don't modify by grav, otherwise factor in gravity
            Vector3 velocityXZ = velocityY.y == 0 ? displacementXZ :
                displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) +
                                  Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

            return velocityXZ + velocityY;
        }
        public static Vector3 CalculateJumpVelocityWithTime(Vector3 startPoint, Vector3 endPoint, float timeToReach)
        {
            // Calculate the displacement in each axis
            Vector3 displacement = endPoint - startPoint;
            
            // Horizontal displacement (XZ plane)
            Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);
            
            // Time to reach horizontal target (horizontal velocity is constant)
            float horizontalSpeed = displacementXZ.magnitude / timeToReach;
            
            // Vertical displacement
            float displacementY = displacement.y;
            
            // Vertical velocity calculation using kinematic equations
            // Equation: y = v0 * t + 0.5 * g * t^2, solving for v0 (initial velocity)
            float verticalSpeed = (displacementY - 0.5f * Physics.gravity.y * timeToReach * timeToReach) / timeToReach;

            // Combine the horizontal and vertical velocities
            Vector3 velocity = displacementXZ.normalized * horizontalSpeed; // Horizontal velocity vector
            velocity.y = verticalSpeed; // Vertical velocity component

            return velocity;
        }
    }
}