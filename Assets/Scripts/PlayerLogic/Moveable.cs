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
            
            // Add rotation coupling debug info
            if (groundable.IsGrounded())
            {
                Collider groundCollider = groundable.GetGroundCollider();
                if (groundCollider != null)
                {
                    CelestialBody celestialBody = groundCollider.GetComponentInParent<CelestialBody>();
                    if (celestialBody != null && !Mathf.Approximately(celestialBody.angularSpeedDegrees, 0f))
                    {
                        Vector3 tangentialVel = celestialBody.GetTangentialVelocityAtPosition(player.rigidbody.position);
                        CornerDebug.AddDebug($"Rotating Planet: {celestialBody.name} ({celestialBody.angularSpeedDegrees:F1}°/s)");
                        CornerDebug.AddDebug($"Surface Tangential Vel: {tangentialVel.magnitude:F2} m/s");
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies strong rotation coupling when player is grounded on a rotating planet.
        /// This ensures the player moves with the planet's surface rotation, maintaining
        /// their local position as the planet spins beneath them.
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
            
            // Calculate the tangential velocity the player should have at their current position
            Vector3 targetTangentialVelocity = celestialBody.GetTangentialVelocityAtPosition(player.rigidbody.position);
            
            // Get the tangential direction (perpendicular to both radius and rotation axis)
            Vector3 omega = celestialBody.GetAngularVelocity();
            Vector3 radialDirection = (player.rigidbody.position - celestialBody.rigidbody.position).normalized;
            Vector3 tangentialDirection = Vector3.Cross(omega.normalized, radialDirection).normalized;
            
            // Calculate how much the current velocity differs from target in the tangential direction
            float currentTangentialSpeed = Vector3.Dot(player.rigidbody.velocity, tangentialDirection);
            float targetTangentialSpeed = Vector3.Dot(targetTangentialVelocity, tangentialDirection);
            
            // When grounded, apply strong coupling to quickly match the surface velocity
            float speedDifference = targetTangentialSpeed - currentTangentialSpeed;
            
            // Use a strong coupling factor for grounded objects (much stronger than the general gravitatable coupling)
            float groundedCouplingStrength = 10f; // Strong coupling for grounded state
            
            // Apply the velocity adjustment directly (instantaneous velocity change)
            Vector3 velocityAdjustment = tangentialDirection * speedDifference * groundedCouplingStrength * Time.fixedDeltaTime;
            player.rigidbody.velocity += velocityAdjustment;
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
