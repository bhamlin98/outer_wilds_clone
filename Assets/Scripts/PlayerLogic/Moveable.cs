using Celestial;
using UI.Debug;
using UnityEngine;

namespace PlayerLogic
{
    /**
     * Class that handles player moving logic
     */
    public class Moveable
    {
        private readonly Player player;
        private readonly Groundable groundable;
        private readonly Jumpable jumpable;

        private const float LegsMoveSpeed = 7f;

        public Moveable(Player player)
        {
            this.player = player;
            groundable = new Groundable(player);
            jumpable = new Jumpable(player);
        }

        public bool IsGrounded()
        {
            return groundable.IsGrounded();
        }

        public void Move(PlayerControllable playerControllable)
        {
            Vector3 playerVerticalMotion = player.transform.up * playerControllable.movement.y;
            Vector3 playerHorizontalMotion = player.transform.forward * playerControllable.movement.x +
                                             player.transform.right * playerControllable.movement.z;

            if (groundable.IsGrounded())
            {
                // Apply rotation coupling when grounded on a rotating planet
                ApplyGroundedRotationCoupling();
                
                WalkByFoot(playerHorizontalMotion);
                HandleJumpLogic(playerControllable);
            }
            else
            {
                FireHorizontalThrusters(playerHorizontalMotion);
            }

            if (DoPlayerWantsToFlyVertically(playerControllable))
            {
                FireVerticalThrusters(playerControllable, playerVerticalMotion);
            }

            CornerDebug.AddDebug("IsOnTheGround = " + groundable.IsGrounded());
        }
        
        /// <summary>
        /// Applies rotation coupling when player is grounded on a rotating planet.
        /// This directly sets the player's velocity to match the planet's surface rotation,
        /// ensuring the player moves with the planet as it rotates.
        /// Physics: v_tangent = ω × r, where ω is angular velocity and r is position from center
        /// </summary>
        private void ApplyGroundedRotationCoupling()
        {
            Collider groundCollider = groundable.GetGroundCollider();
            if (groundCollider == null)
            {
                return;
            }
            
            // Check if the ground is a celestial body (planet)
            CelestialBody celestialBody = groundCollider.GetComponentInParent<CelestialBody>();
            if (celestialBody == null)
            {
                return;
            }
            
            // Only apply if the planet is actually rotating
            if (Mathf.Approximately(celestialBody.angularSpeedDegrees, 0f))
            {
                return;
            }
            
            // Calculate what the player's tangential velocity should be at this position
            // This is the velocity needed to co-rotate with the planet's surface
            Vector3 targetTangentialVelocity = celestialBody.GetTangentialVelocityAtPosition(player.rigidbody.position);
            
            // Get the tangential direction (perpendicular to both radius and rotation axis)
            Vector3 omega = celestialBody.GetAngularVelocity();
            Vector3 radialDirection = (player.rigidbody.position - celestialBody.rigidbody.position).normalized;
            Vector3 tangentialDirection = Vector3.Cross(omega.normalized, radialDirection).normalized;
            
            // Decompose current velocity into tangential and non-tangential components
            float currentTangentialSpeed = Vector3.Dot(player.rigidbody.velocity, tangentialDirection);
            Vector3 currentTangentialVelocity = tangentialDirection * currentTangentialSpeed;
            Vector3 nonTangentialVelocity = player.rigidbody.velocity - currentTangentialVelocity;
            
            // When grounded, we want to completely replace the tangential component with the planet's
            // This simulates friction locking the player to the rotating surface
            // The non-tangential components (radial, walking movement) are preserved
            player.rigidbody.velocity = nonTangentialVelocity + targetTangentialVelocity;
        }

        private void WalkByFoot(Vector3 playerHorizontalMotion)
        {
            // Movement by foot with AddForce is buggy, so for now MovePosition will work.
            // 03 November 2020 Update: Should've used AddForce :D

            Vector3 playerPositionAddition = playerHorizontalMotion;
            playerPositionAddition *= LegsMoveSpeed;
            playerPositionAddition *= Time.deltaTime;

            player.rigidbody.MovePosition(player.rigidbody.position + playerPositionAddition);
        }

        private void FireHorizontalThrusters(Vector3 playerHorizontalMotion)
        {
            Vector3 horizontalThrustersForce = playerHorizontalMotion;
            horizontalThrustersForce *= player.spaceSuit.FireHorizontalThrusters();
            horizontalThrustersForce *= Time.deltaTime;

            player.rigidbody.AddForce(horizontalThrustersForce);
        }

        private void FireVerticalThrusters(PlayerControllable playerControllable, Vector3 playerVerticalMotion)
        {
            bool useSuperFuel = playerControllable.movement.y > 0f && playerControllable.jump;

            Vector3 verticalThrustersForce = playerVerticalMotion;
            verticalThrustersForce *= player.spaceSuit.FireVerticalThrusters(useSuperFuel);
            verticalThrustersForce *= Time.deltaTime;

            player.rigidbody.AddForce(verticalThrustersForce);
        }

        private void HandleJumpLogic(PlayerControllable playerControllable)
        {
            if (playerControllable.jump)
            {
                jumpable.AccumulateJumpPower();
            }
            else if (!playerControllable.jump && jumpable.ReadyToJump)
            {
                Vector3 jumpMotion = player.transform.up;
                jumpMotion *= jumpable.Jump();
                player.rigidbody.AddForce(jumpMotion); // There's no Time.deltaTime, because it's a single force push
            }
        }

        private static bool DoPlayerWantsToFlyVertically(PlayerControllable playerControllable)
        {
            return !Mathf.Approximately(playerControllable.movement.y, 0f);
        }
    }
}
