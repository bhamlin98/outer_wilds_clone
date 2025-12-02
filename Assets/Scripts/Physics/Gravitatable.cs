using Celestial;
using UI.Debug;
using UnityEngine;

namespace Physics
{
    /**
     * Component that handles gravitation to all existing CelestialBodies
     */
    public class Gravitatable
    {
        private readonly Rigidbody rigidbody;
        private readonly bool isCelestialBody;
        private readonly bool applyRotationCoupling;

        private readonly CelestialBody[] celestialBodies;
        private MaxGravitatableInfo maxGravitatableInfo;
        private bool hasInitializedRotationVelocity = false;

        public Gravitatable(Rigidbody rigidbody, CelestialBody[] celestialBodies, bool isCelestialBody = false, bool applyRotationCoupling = true)
        {
            this.rigidbody = rigidbody;
            this.isCelestialBody = isCelestialBody;
            this.celestialBodies = celestialBodies;
            this.applyRotationCoupling = applyRotationCoupling;
        }

        public MaxGravitatableInfo ApplyGravity()
        {
            maxGravitatableInfo = new MaxGravitatableInfo();

            foreach (CelestialBody celestialBody in celestialBodies)
            {
                // This difference is needed because celestial body gravity is a little bit to harsh for the player
                Vector3 gravityForce = isCelestialBody ? Gravitation.ComputeCelestialBodyForce(rigidbody, celestialBody.rigidbody) : Gravitation.ComputeNonCelestialBodyForce(rigidbody, celestialBody);
                gravityForce *= Time.deltaTime;
                rigidbody.AddForce(gravityForce);

                if (gravityForce.magnitude > maxGravitatableInfo.MaxGravityForce.magnitude)
                {
                    maxGravitatableInfo.Update(gravityForce, celestialBody);
                }

                CornerDebug.AddGravityDebug(celestialBody.name, $"'{celestialBody.name}' gravity magnitude: {gravityForce.magnitude}");
            }
            
            // Initialize rotation velocity on first update if we're near a rotating planet
            if (applyRotationCoupling && !hasInitializedRotationVelocity && maxGravitatableInfo.CelestialBody != null)
            {
                InitializeRotationVelocity(maxGravitatableInfo.CelestialBody);
                hasInitializedRotationVelocity = true;
            }
            
            // Apply rotation coupling if enabled and we have a dominant celestial body
            if (applyRotationCoupling && maxGravitatableInfo.CelestialBody != null)
            {
                ApplyRotationCoupling(maxGravitatableInfo.CelestialBody);
            }

            return maxGravitatableInfo;
        }
        
        /// <summary>
        /// Initializes the object's velocity to match the tangential velocity of a rotating planet.
        /// This is called once on the first physics update to prevent objects from being thrown away
        /// when they spawn near a rotating planet.
        /// </summary>
        private void InitializeRotationVelocity(CelestialBody celestialBody)
        {
            // Only initialize if the planet is actually rotating
            if (Mathf.Approximately(celestialBody.angularSpeedDegrees, 0f))
            {
                return;
            }
            
            // Only apply if object is near the planet surface
            Vector3 toBody = celestialBody.rigidbody.position - rigidbody.position;
            float distanceFromCenter = toBody.magnitude;
            
            // Only initialize within reasonable range (2x radius)
            float couplingRadius = celestialBody.radius * 2f;
            if (distanceFromCenter > couplingRadius)
            {
                return;
            }
            
            // Calculate the tangential velocity at the object's spawn position
            Vector3 tangentialVelocity = celestialBody.GetTangentialVelocityAtPosition(rigidbody.position);
            
            // Add the tangential velocity to the object's current velocity
            // This ensures objects spawn with the correct velocity for their position
            rigidbody.velocity += tangentialVelocity;
            
            CornerDebug.AddDebug($"Initialized velocity with tangential: {tangentialVelocity.magnitude:F2} m/s");
        }
        
        /// <summary>
        /// Applies rotation coupling to make objects co-rotate with nearby rotating planets.
        /// This adjusts the object's velocity to include the tangential velocity component
        /// from the planet's rotation, implementing the physics: v_tangent = ω × r
        /// </summary>
        private void ApplyRotationCoupling(CelestialBody celestialBody)
        {
            // Only apply rotation coupling if the planet is actually rotating
            if (Mathf.Approximately(celestialBody.angularSpeedDegrees, 0f))
            {
                return;
            }
            
            // Only apply strong coupling if object is near the planet surface
            Vector3 toBody = celestialBody.rigidbody.position - rigidbody.position;
            float distanceFromCenter = toBody.magnitude;
            
            // Define a coupling strength that falls off with distance
            // Strong coupling within 2x radius, weak beyond that
            float couplingRadius = celestialBody.radius * 2f;
            if (distanceFromCenter > couplingRadius)
            {
                return;
            }
            
            // Calculate the tangential velocity the object should have at this position
            Vector3 targetTangentialVelocity = celestialBody.GetTangentialVelocityAtPosition(rigidbody.position);
            
            // Calculate current velocity component in the tangential direction
            Vector3 omega = celestialBody.GetAngularVelocity();
            Vector3 radialDirection = (rigidbody.position - celestialBody.rigidbody.position).normalized;
            Vector3 tangentialDirection = Vector3.Cross(omega.normalized, radialDirection).normalized;
            
            float currentTangentialSpeed = Vector3.Dot(rigidbody.velocity, tangentialDirection);
            float targetTangentialSpeed = Vector3.Dot(targetTangentialVelocity, tangentialDirection);
            
            // Apply a force to gradually match the planet's rotation
            // Use a smooth interpolation factor based on distance
            float couplingStrength = Mathf.Clamp01(1f - (distanceFromCenter - celestialBody.radius) / celestialBody.radius);
            float speedDifference = targetTangentialSpeed - currentTangentialSpeed;
            
            // Apply acceleration to match rotation (stronger when closer to surface)
            // AddForce in FixedUpdate already accounts for timestep, so no Time.deltaTime needed
            Vector3 couplingForce = tangentialDirection * speedDifference * couplingStrength * rigidbody.mass;
            rigidbody.AddForce(couplingForce, ForceMode.Force);
        }
    }
}
