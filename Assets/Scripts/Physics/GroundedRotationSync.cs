using Celestial;
using UnityEngine;

namespace Physics
{
    /**
     * Component that synchronizes grounded objects with rotating planet surfaces.
     * Handles position, velocity, and centripetal force to keep objects attached to rotating surfaces.
     */
    public class GroundedRotationSync
    {
        private readonly Rigidbody rigidbody;
        private readonly Transform transform;
        
        private CelestialBody currentGroundedBody;
        private Vector3 lastLocalPosition;
        private Quaternion lastBodyRotation;
        private bool wasGroundedLastFrame;

        public GroundedRotationSync(Rigidbody rigidbody, Transform transform)
        {
            this.rigidbody = rigidbody;
            this.transform = transform;
            this.wasGroundedLastFrame = false;
        }

        /**
         * Updates the grounded object's position and velocity to match the rotating surface.
         * Should be called in FixedUpdate after gravity is applied and grounded status is determined.
         */
        public void SyncWithRotatingSurface(bool isGrounded, CelestialBody groundedBody)
        {
            if (!isGrounded || groundedBody == null)
            {
                wasGroundedLastFrame = false;
                currentGroundedBody = null;
                return;
            }

            // Check if the body is actually rotating
            if (Mathf.Approximately(groundedBody.angularVelocity, 0f))
            {
                wasGroundedLastFrame = isGrounded;
                currentGroundedBody = groundedBody;
                return;
            }

            // If we just became grounded or changed bodies, initialize tracking
            if (!wasGroundedLastFrame || currentGroundedBody != groundedBody)
            {
                InitializeGroundedTracking(groundedBody);
            }
            else
            {
                // Sync position and velocity with the rotating surface
                SyncRotation(groundedBody);
            }

            wasGroundedLastFrame = isGrounded;
            currentGroundedBody = groundedBody;
        }

        private void InitializeGroundedTracking(CelestialBody groundedBody)
        {
            // Store the local position relative to the planet
            lastLocalPosition = groundedBody.transform.InverseTransformPoint(transform.position);
            lastBodyRotation = groundedBody.transform.rotation;
        }

        private void SyncRotation(CelestialBody groundedBody)
        {
            // Calculate how much the body has rotated since last frame
            Quaternion rotationDelta = groundedBody.transform.rotation * Quaternion.Inverse(lastBodyRotation);
            
            // Transform the local position by the rotation delta to get new world position
            Vector3 newWorldPosition = groundedBody.transform.TransformPoint(lastLocalPosition);
            
            // Calculate the velocity change needed to move to the new position
            Vector3 positionDelta = newWorldPosition - transform.position;
            Vector3 velocityCorrection = positionDelta / Time.fixedDeltaTime;
            
            // Apply velocity correction to maintain position on rotating surface
            rigidbody.velocity += velocityCorrection;
            
            // Apply centripetal force to keep object rooted during rotation
            ApplyCentripetalForce(groundedBody);
            
            // Update tracking for next frame
            lastLocalPosition = groundedBody.transform.InverseTransformPoint(transform.position);
            lastBodyRotation = groundedBody.transform.rotation;
        }

        private void ApplyCentripetalForce(CelestialBody groundedBody)
        {
            // Calculate distance from rotation axis
            Vector3 bodyToObject = transform.position - groundedBody.transform.position;
            Vector3 rotationAxisNormalized = groundedBody.rotationAxis.normalized;
            
            // Project the vector onto the plane perpendicular to the rotation axis
            Vector3 radialVector = bodyToObject - Vector3.Project(bodyToObject, rotationAxisNormalized);
            float radius = radialVector.magnitude;
            
            if (radius < 0.01f)
            {
                // Too close to the axis, no centripetal force needed
                return;
            }
            
            // Calculate angular velocity in radians per second
            float angularVelocityRad = groundedBody.angularVelocity * Mathf.Deg2Rad;
            
            // Centripetal acceleration = ω² * r, directed toward the axis
            float centripetalAcceleration = angularVelocityRad * angularVelocityRad * radius;
            
            // Direction is from object toward the rotation axis (inward)
            Vector3 centripetalDirection = -radialVector.normalized;
            
            // Apply centripetal force (AddForce already handles time scaling in FixedUpdate)
            Vector3 centripetalForce = centripetalDirection * centripetalAcceleration * rigidbody.mass;
            rigidbody.AddForce(centripetalForce);
        }
    }
}
