using Celestial;
using UnityEngine;

namespace PlayerLogic
{
    /**
     * Class that handles grounding logic
     */
    public class Groundable
    {
        private const float DistanceFromBodyCenterToGround = 1.1f;

        private readonly Player player;
        private readonly LayerMask groundCheckLayerMask;
        private CelestialBody groundedBody;

        public Groundable(Player player)
        {
            this.player = player;
            groundCheckLayerMask = LayerMask.GetMask("Planets", "Objects");
        }

        public bool IsGrounded()
        {
            Transform cachedPlayerTransform = player.transform;
            RaycastHit hit;
            bool isGrounded = UnityEngine.Physics.Raycast(cachedPlayerTransform.position, -cachedPlayerTransform.up, out hit, DistanceFromBodyCenterToGround, groundCheckLayerMask);
            
            if (isGrounded)
            {
                // Try to get the CelestialBody component from the hit object or its parents
                groundedBody = hit.collider.GetComponentInParent<CelestialBody>();
            }
            else
            {
                groundedBody = null;
            }
            
            return isGrounded;
        }

        public CelestialBody GetGroundedBody()
        {
            return groundedBody;
        }
    }
}
