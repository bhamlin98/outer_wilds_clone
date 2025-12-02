using Celestial;
using UnityEngine;

namespace Physics
{
    /// <summary>
    /// Handles coupling objects to the rotation of nearby planets.
    /// For airborne objects near rotating planets, applies a smooth velocity adjustment
    /// to help them feel the effects of planetary rotation.
    /// </summary>
    public class PlanetaryRotationCoupler
    {
        private readonly Rigidbody rigidbody;
        private bool hasInitializedVelocity = false;

        public PlanetaryRotationCoupler(Rigidbody rigidbody)
        {
            this.rigidbody = rigidbody;
        }

        /// <summary>
        /// Applies rotation coupling for an object near a rotating planet.
        /// Should be called from FixedUpdate.
        /// </summary>
        /// <param name="dominantCelestialBody">The planet with the strongest gravitational influence</param>
        /// <param name="isGrounded">Whether the object is grounded (handled separately by grounding logic)</param>
        public void ApplyRotationCoupling(CelestialBody dominantCelestialBody, bool isGrounded)
        {
            if (dominantCelestialBody == null)
            {
                return;
            }

            // Initialize velocity on first call to prevent spawning issues
            if (!hasInitializedVelocity)
            {
                InitializeVelocityForRotation(dominantCelestialBody);
                hasInitializedVelocity = true;
            }

            // Skip if not rotating
            if (Mathf.Approximately(dominantCelestialBody.angularSpeedDegrees, 0f))
            {
                return;
            }

            // Skip if grounded - grounding logic handles this case
            if (isGrounded)
            {
                return;
            }

            // Only apply to objects near the planet (within 2x radius)
            float distanceFromCenter = (rigidbody.position - dominantCelestialBody.rigidbody.position).magnitude;
            float maxCouplingDistance = dominantCelestialBody.radius * 2f;
            
            if (distanceFromCenter > maxCouplingDistance)
            {
                return;
            }

            // Calculate target tangential velocity
            Vector3 targetTangentialVelocity = dominantCelestialBody.GetTangentialVelocityAtPosition(rigidbody.position);

            // Get tangential direction
            Vector3 omega = dominantCelestialBody.GetAngularVelocity();
            Vector3 radialDirection = (rigidbody.position - dominantCelestialBody.rigidbody.position).normalized;
            Vector3 tangentialDirection = Vector3.Cross(omega.normalized, radialDirection).normalized;

            // Calculate how much our tangential velocity differs from target
            float currentTangentialSpeed = Vector3.Dot(rigidbody.velocity, tangentialDirection);
            float targetTangentialSpeed = Vector3.Dot(targetTangentialVelocity, tangentialDirection);
            float speedDifference = targetTangentialSpeed - currentTangentialSpeed;

            // Apply a distance-based coupling strength
            // Stronger near surface (1.0), weaker far away (0.0)
            // Guard against division by zero
            float couplingStrength = 0f;
            if (dominantCelestialBody.radius > 0f)
            {
                float distanceRatio = (distanceFromCenter - dominantCelestialBody.radius) / dominantCelestialBody.radius;
                couplingStrength = Mathf.Clamp01(1f - distanceRatio);
            }

            // For airborne objects, use a moderate coupling factor (0.1 = 10% per frame)
            // This is gentle enough to allow orbital mechanics but strong enough to feel rotation
            float airborneCouplingFactor = 0.1f;
            
            // Apply velocity adjustment
            Vector3 velocityAdjustment = tangentialDirection * speedDifference * couplingStrength * airborneCouplingFactor;
            rigidbody.velocity += velocityAdjustment;
        }

        /// <summary>
        /// Initializes the object's velocity to match the tangential velocity at spawn position.
        /// This prevents objects from being thrown away when they spawn on or near rotating planets.
        /// Only adds tangential velocity if the object doesn't already have it.
        /// </summary>
        private void InitializeVelocityForRotation(CelestialBody celestialBody)
        {
            if (Mathf.Approximately(celestialBody.angularSpeedDegrees, 0f))
            {
                return;
            }

            float distanceFromCenter = (rigidbody.position - celestialBody.rigidbody.position).magnitude;
            float maxCouplingDistance = celestialBody.radius * 2f;
            
            if (distanceFromCenter > maxCouplingDistance)
            {
                return;
            }

            // Calculate target tangential velocity
            Vector3 targetTangentialVelocity = celestialBody.GetTangentialVelocityAtPosition(rigidbody.position);
            
            // Get current tangential component
            Vector3 omega = celestialBody.GetAngularVelocity();
            Vector3 radialDirection = (rigidbody.position - celestialBody.rigidbody.position).normalized;
            Vector3 tangentialDirection = Vector3.Cross(omega.normalized, radialDirection).normalized;
            float currentTangentialSpeed = Vector3.Dot(rigidbody.velocity, tangentialDirection);
            float targetTangentialSpeed = Vector3.Dot(targetTangentialVelocity, tangentialDirection);
            
            // Only add if significantly different (more than 10% off)
            float speedDifference = Mathf.Abs(targetTangentialSpeed - currentTangentialSpeed);
            if (speedDifference > Mathf.Abs(targetTangentialSpeed) * 0.1f)
            {
                rigidbody.velocity += targetTangentialVelocity;
            }
        }
    }
}
