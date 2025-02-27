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
    }
}