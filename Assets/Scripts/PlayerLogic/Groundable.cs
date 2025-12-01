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
        private RaycastHit lastGroundHit;
        private bool isCurrentlyGrounded;

        public Groundable(Player player)
        {
            this.player = player;
            groundCheckLayerMask = LayerMask.GetMask("Planets", "Objects");
        }

        public bool IsGrounded()
        {
            Transform cachedPlayerTransform = player.transform;
            isCurrentlyGrounded = UnityEngine.Physics.Raycast(cachedPlayerTransform.position, -cachedPlayerTransform.up, out lastGroundHit, DistanceFromBodyCenterToGround, groundCheckLayerMask);
            return isCurrentlyGrounded;
        }
        
        /// <summary>
        /// Gets the ground collider that the player is standing on, or null if not grounded.
        /// Uses cached result from last IsGrounded() call to avoid duplicate raycasts.
        /// </summary>
        public Collider GetGroundCollider()
        {
            return isCurrentlyGrounded ? lastGroundHit.collider : null;
        }
    }
}
