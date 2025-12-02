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

        // Axial rotation parameters
        public Vector3 rotationAxis = Vector3.up; // Default rotation around Y-axis
        public float angularVelocity = 0f; // Degrees per second

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
                ApplyRotation();
                return;
            }

            gravitatable.ApplyGravity();
            ApplyRotation();
            DrawOrbit();
        }

        private void ApplyRotation()
        {
            if (Mathf.Approximately(angularVelocity, 0f))
            {
                return;
            }

            // Calculate rotation angle for this frame
            float rotationAngle = angularVelocity * Time.deltaTime;
            
            // Apply rotation around the specified axis
            Quaternion deltaRotation = Quaternion.AngleAxis(rotationAngle, rotationAxis.normalized);
            transform.rotation = deltaRotation * transform.rotation;
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
