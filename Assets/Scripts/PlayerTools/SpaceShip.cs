using System.Linq;
using Celestial;
using Physics;
using PlayerTools.SpaceShipParts;
using UnityEngine;

namespace PlayerTools
{
    /**
     * Main Class for the SpaceShip. It gravitates towards CelestialBodies and knows about all it's children parts
     */
    [RequireComponent(typeof(Rigidbody))]
    public class SpaceShip : AcceleratedMonoBehaviour
    {
        // Internal components
        public new Rigidbody rigidbody;

        // External components
        public SpaceShipThrusters thrusters;
        public SpaceShipAccelerationShowcase accelerationShowcase;
        public SpaceShipFlashlight flashlight;

        // Humble object components
        private Gravitatable gravitatable;
        private TowardsCelestialBodyRotatable towardsCelestialBodyRotatable;
        private PlanetaryRotationCoupler planetaryRotationCoupler;

        private new void Awake()
        {
            base.Awake();

            rigidbody = GetComponent<Rigidbody>();

            gravitatable = new Gravitatable(rigidbody, FindObjectsOfType<CelestialBody>().ToArray());
            towardsCelestialBodyRotatable = new TowardsCelestialBodyRotatable(rigidbody);
            planetaryRotationCoupler = new PlanetaryRotationCoupler(rigidbody);
        }

        private void FixedUpdate()
        {
            MaxGravitatableInfo maxGravitatableInfo = gravitatable.ApplyGravity();
            towardsCelestialBodyRotatable.RotateIfNeeded(maxGravitatableInfo);
            
            // Apply rotation coupling for ship near rotating planets
            // Ships are never "grounded" in the traditional sense, so always pass false
            planetaryRotationCoupler.ApplyRotationCoupling(maxGravitatableInfo.CelestialBody, false);
        }
    }
}
