# Physics System Components

This directory contains the physics simulation components for the Outer Wilds Clone.

## Core Components

### Gravitatable
Handles gravitational forces between celestial bodies and other objects. Calculates and applies Newton's law of universal gravitation.

### Gravitation
Static class containing gravity computation laws and force calculations for both celestial bodies and non-celestial objects.

### AcceleratedMonoBehaviour
Base class for MonoBehaviours that receive an initial velocity on instantiation.

### MaxGravitatableCelestialBody
Stores information about the celestial body with the maximum gravitational influence on an object. Used to determine which planet's surface to orient toward.

### TowardsCelestialBodyRotatable
Rotates rigidbodies (like the player) to orient their "down" direction toward the nearest massive celestial body, creating the effect of standing on a planet's surface.

### GroundedRotationSync (NEW)
Synchronizes grounded objects with rotating planet surfaces. Handles:
- Position tracking relative to rotating surfaces
- Velocity matching to keep objects attached
- Centripetal force application to prevent objects from flying off
- Smooth transitions between rotating and non-rotating surfaces

See [Documentation/PlanetRotation.md](../Documentation/PlanetRotation.md) for detailed information on the rotation system.

## Physics Integration

The physics system works through several coordinated steps each frame:

1. **Gravity Application** (`Gravitatable.ApplyGravity()`)
   - Calculates gravitational forces from all celestial bodies
   - Applies forces to rigidbody
   - Returns info about the strongest gravitational influence

2. **Orientation** (`TowardsCelestialBodyRotatable.RotateIfNeeded()`)
   - Rotates player to face away from the nearest massive body
   - Creates the sensation of "down" toward the planet

3. **Movement** (Player-specific)
   - Processes player input for walking, jumping, or thruster use
   - Applies forces based on whether grounded or in space

4. **Rotation Synchronization** (`GroundedRotationSync.SyncWithRotatingSurface()`)
   - If grounded on a rotating planet, adjusts position and velocity
   - Applies centripetal force to maintain attachment
   - Ensures player doesn't slide or get thrown off by rotation

## Adding Rotation to Planets

To make a planet rotate, add the following to its CelestialBody component:

```csharp
public Vector3 rotationAxis = Vector3.up;  // Y-axis rotation
public float angularVelocity = 5.0f;        // 5 degrees per second
```

The planet will automatically rotate, and grounded objects will be kept attached to the surface.

## Performance Notes

- Gravity calculations use sqrMagnitude instead of magnitude where possible
- Rotation sync only activates when objects are grounded on rotating surfaces
- Centripetal force calculations use cached values and early-exit conditions
- All physics updates occur in FixedUpdate for consistent timing
