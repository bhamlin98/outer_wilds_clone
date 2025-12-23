using Celestial;
using UnityEngine;

namespace Physics
{
    /**
     * Component that synchronizes grounded objects with rotating planet surfaces.
     * Rotates the object's position and velocity around the planet's center to maintain
     * attachment to the rotating surface.
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
            // Calculate the rotation delta between last frame and current frame
            Quaternion rotationDelta = groundedBody.transform.rotation * Quaternion.Inverse(lastBodyRotation);
            
            // Rotate the player around the planet's center by the same amount the planet rotated
            Vector3 toPlayer = transform.position - groundedBody.transform.position;
            Vector3 rotatedOffset = rotationDelta * toPlayer;
            Vector3 newPosition = groundedBody.transform.position + rotatedOffset;
            
            // Move the player to the new position
            rigidbody.MovePosition(newPosition);
            
            // Rotate the player's velocity by the same rotation
            rigidbody.velocity = rotationDelta * rigidbody.velocity;
            
            // Update tracking for next frame using the actual transform position after MovePosition
            lastLocalPosition = groundedBody.transform.InverseTransformPoint(rigidbody.position);
            lastBodyRotation = groundedBody.transform.rotation;
        }


    }
}
