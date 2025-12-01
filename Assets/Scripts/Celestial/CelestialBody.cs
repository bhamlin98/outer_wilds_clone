using System.Linq;
using Physics;
using UnityEngine;

namespace Celestial
{
    /**
     * Class for a celestial body which has rigidbody, gravitates to other celestial bodies and has an orbit trail
     */
    [RequireComponent(typeof(Rigidbody))]
    public class CelestialBody : AcceleratedMonoBehaviour
    {
        public new Rigidbody rigidbody;

        public new string name;
        public float radius; // Is needed to compute size of a planet. I can somehow get this data from renderers, but for now this will do
        public bool isStationary; // I want the Sun to always be at 0, 0, 0. I can do it with moving sun, but it will ease the numbers
        public float gravityScale;
        
        // Axial rotation properties
        [Tooltip("Rotation axis in world space (will be normalized). Default is up (0,1,0)")]
        public Vector3 rotationAxis = Vector3.up;
        [Tooltip("Angular speed in degrees per second")]
        public float angularSpeedDegrees = 0f;

        private Gravitatable gravitatable;

        // Nested objects
        private Orbit orbit;

        private new void Awake()
        {
            base.Awake();

            rigidbody = GetComponent<Rigidbody>();

            gravitatable = new Gravitatable(rigidbody, FindObjectsOfType<CelestialBody>().Where(body => body != this).ToArray(), true);
            orbit = new Orbit(rigidbody.position, Color.white);
        }

        private void FixedUpdate()
        {
            if (isStationary)
            {
                return;
            }

            gravitatable.ApplyGravity();
            DrawOrbit();
            UpdateRotation();
        }
        
        /// <summary>
        /// Updates the planet's rotation based on its angular velocity.
        /// This implements axial rotation for the celestial body.
        /// </summary>
        private void UpdateRotation()
        {
            if (Mathf.Approximately(angularSpeedDegrees, 0f))
            {
                return;
            }
            
            // Convert angular speed from degrees/sec to radians for this frame
            float rotationAngleDegrees = angularSpeedDegrees * Time.deltaTime;
            
            // Rotate the planet around its axis
            transform.Rotate(rotationAxis.normalized, rotationAngleDegrees, Space.World);
        }
        
        /// <summary>
        /// Gets the angular velocity vector (in radians per second) for this celestial body.
        /// This represents ω in the equation v_tangent = ω × r
        /// </summary>
        public Vector3 GetAngularVelocity()
        {
            // Convert degrees/sec to radians/sec and multiply by normalized axis
            float angularSpeedRadians = angularSpeedDegrees * Mathf.Deg2Rad;
            return rotationAxis.normalized * angularSpeedRadians;
        }
        
        /// <summary>
        /// Calculates the tangential velocity at a given world position due to this planet's rotation.
        /// Uses the formula: v_tangent = ω × r
        /// where ω is angular velocity vector and r is position relative to planet center
        /// </summary>
        public Vector3 GetTangentialVelocityAtPosition(Vector3 worldPosition)
        {
            Vector3 omega = GetAngularVelocity();
            Vector3 r = worldPosition - rigidbody.position;
            
            // Cross product: ω × r gives tangential velocity
            return Vector3.Cross(omega, r);
        }

        private void DrawOrbit()
        {
            if (isStationary)
            {
                return;
            }

            orbit.Draw();
            orbit.Update(this);
        }
    }
}
