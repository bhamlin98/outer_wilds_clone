# Planet Axial Rotation System

## Overview

The planet axial rotation system simulates realistic day-night cycles and planetary rotation physics. Planets can rotate around a configurable axis at a specified angular velocity, and objects grounded on the planet surface (such as the player) remain attached to their local position as the surface rotates beneath them.

## Core Components

### CelestialBody Rotation

The `CelestialBody` class has been extended with the following rotation parameters:

- **rotationAxis** (Vector3): The axis around which the planet rotates, specified in local space. Defaults to `Vector3.up` (Y-axis).
- **angularVelocity** (float): The rotation speed in degrees per second. A positive value rotates counterclockwise around the axis, while negative rotates clockwise.

#### Configuration

To configure a planet's rotation:

1. Select the planet GameObject in the Unity hierarchy
2. In the Inspector, find the CelestialBody component
3. Set the **Rotation Axis** (default: 0, 1, 0 for Y-axis)
4. Set the **Angular Velocity** in degrees per second (e.g., 10 for a fast rotation, 0.5 for Earth-like)

**Example configurations:**
- Earth-like rotation: angularVelocity = 0.004167 (one rotation every 24 in-game minutes if 1 second = 1 minute)
- Fast rotation for testing: angularVelocity = 10.0
- Retrograde (backward) rotation: angularVelocity = -5.0
- No rotation: angularVelocity = 0.0

### Rotation Physics

The rotation is applied in `CelestialBody.FixedUpdate()` using Unity's physics system:

```csharp
float rotationAngle = angularVelocity * Time.deltaTime;
Quaternion deltaRotation = Quaternion.AngleAxis(rotationAngle, rotationAxis.normalized);
transform.rotation = deltaRotation * transform.rotation;
```

This ensures smooth, frame-rate independent rotation that integrates with the physics simulation.

## Grounded Object Synchronization

### GroundedRotationSync Component

The `GroundedRotationSync` class handles keeping grounded objects (like the player) attached to rotating planet surfaces. It provides:

1. **Position Synchronization**: Updates the object's position to match the rotating surface
2. **Velocity Matching**: Adjusts the object's velocity to move with the surface
3. **Centripetal Force**: Applies inward force to counteract the centrifugal effect of rotation

### How It Works

When an object is grounded on a rotating planet:

1. The system tracks the object's local position relative to the planet
2. Each frame, it calculates where that local position has moved in world space due to rotation
3. It applies velocity corrections to move the object to the new position
4. It applies centripetal force to keep the object rooted to the surface

#### Centripetal Force Calculation

The centripetal force is calculated as:

```
F = m × ω² × r
```

Where:
- m = object's mass
- ω = angular velocity (rad/s)
- r = distance from rotation axis

The force is directed toward the rotation axis, counteracting the tendency of the object to fly off due to rotation.

### Integration with Player

The player's `FixedUpdate()` now includes:

```csharp
// Sync with rotating planet surface if grounded
Groundable groundable = moveable.GetGroundable();
bool isGrounded = groundable.IsGrounded();
CelestialBody groundedBody = groundable.GetGroundedBody();
groundedRotationSync.SyncWithRotatingSurface(isGrounded, groundedBody);
```

This is called after gravity and movement are applied, ensuring the player experiences correct physics.

## Physics Interactions

### With Gravity

The rotation sync works alongside the existing gravity system:
- Gravity pulls the player toward the planet center
- Rotation sync keeps the player attached to the rotating surface
- Together, they create the experience of standing on a rotating world

### With Player Movement

When walking on a rotating planet:
- Player input controls movement relative to the local surface
- The rotation sync adjusts the player's world velocity to account for surface rotation
- The player experiences natural movement as if the surface were stationary

### With Jumping

When the player jumps:
- The rotation sync detects the player is no longer grounded
- It stops applying synchronization forces
- The player maintains the tangential velocity from the rotating surface
- Upon landing, synchronization resumes

## Performance Considerations

The rotation sync is only active when:
1. The object is grounded on a planet
2. The planet has a non-zero angular velocity
3. The object is a rigidbody subject to physics

This ensures minimal performance impact when rotation is not needed.

## Extending to Other Objects

The `GroundedRotationSync` component can be used for any rigidbody that should remain attached to rotating surfaces:

```csharp
// In your MonoBehaviour
private GroundedRotationSync rotationSync;

void Awake()
{
    Rigidbody rb = GetComponent<Rigidbody>();
    rotationSync = new GroundedRotationSync(rb, transform);
}

void FixedUpdate()
{
    // Determine if grounded and which body
    bool isGrounded = /* your grounding check */;
    CelestialBody groundedBody = /* your grounded body detection */;
    
    rotationSync.SyncWithRotatingSurface(isGrounded, groundedBody);
}
```

## Implementation Notes

### Code-Only Configuration

As requested, rotation parameters are configured entirely in code through the CelestialBody component. No prefab or scene changes are required to enable rotation - simply set the angularVelocity field to a non-zero value.

### Stationary Bodies

Stationary bodies (like the Sun) can still rotate. The `isStationary` flag only prevents orbital motion, not rotation. This allows for features like a rotating star with planets in fixed orbits.

### Multiple Rotating Bodies

The system correctly handles:
- Transitioning between differently rotating planets
- Landing on a non-rotating planet after leaving a rotating one
- Multiple players or objects on the same rotating planet
- Objects on different planets rotating at different rates

## Testing Tips

To test the rotation system:

1. Set a high angular velocity (e.g., 20-30 degrees/second) for visible rotation
2. Land the player on the planet's surface
3. Observe the player remaining in place relative to the surface as it rotates
4. Try walking, jumping, and using the jetpack
5. Gradually reduce angular velocity to more realistic values

## Troubleshooting

### Player slides off during rotation
- Increase the centripetal force calculation (adjust coefficient in GroundedRotationSync)
- Check that the grounding raycast is detecting the planet correctly
- Verify the rotation axis is correct

### Player appears to jitter
- Ensure rotation sync is called in FixedUpdate, not Update
- Check that Time.deltaTime is being used correctly
- Verify the local position tracking is initialized properly

### Rotation feels wrong
- Check the rotation axis direction (use right-hand rule)
- Verify angular velocity sign (positive vs negative)
- Test with a slow rotation first, then increase speed

## Future Enhancements

Potential future improvements:
- Coriolis effect for long-distance projectiles
- Variable rotation speed (tidal locking, orbital resonance)
- Rotation axis precession (axial tilt changes over time)
- Visual indicators of rotation (shadows, day-night cycle)
