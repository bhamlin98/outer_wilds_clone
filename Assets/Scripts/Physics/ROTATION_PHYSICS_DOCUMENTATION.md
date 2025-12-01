# Axial Rotation Physics Implementation

## Overview

This implementation adds axial rotation to celestial bodies (planets) and ensures that physics objects (player, ship, debris) correctly experience the effects of planetary rotation. Objects near or on the surface of rotating planets will experience tangential velocity and move in circular motion consistent with the planet's rotation.

## Physics Model

### Core Concepts

1. **Angular Velocity (ω)**: Each celestial body has an angular velocity vector representing its rotation axis and speed.
   - Configured via `rotationAxis` (normalized direction) and `angularSpeedDegrees` (rotation rate in degrees/second)
   - Converted to radians/second for physics calculations: ω = axis × (speed × π/180)

2. **Tangential Velocity (v_tangent)**: Objects at a given position relative to a rotating planet should have a tangential velocity component.
   - Formula: **v_tangent = ω × r**
   - Where r is the position vector from planet center to object
   - This is a cross product that gives velocity perpendicular to both the rotation axis and radius

3. **Rotation Coupling**: Objects are "coupled" to the planet's rotation based on distance
   - Objects on the surface experience strong coupling (quickly match surface velocity)
   - Objects in space near the planet experience weaker coupling that falls off with distance

## Implementation Details

### CelestialBody.cs

Added properties:
- `Vector3 rotationAxis`: Rotation axis in world space (default: Vector3.up)
- `float angularSpeedDegrees`: Angular speed in degrees per second

Added methods:
- `UpdateRotation()`: Rotates the planet transform each frame
- `GetAngularVelocity()`: Returns ω vector in radians/second
- `GetTangentialVelocityAtPosition(Vector3 worldPosition)`: Calculates v_tangent = ω × r for any world position

### Gravitatable.cs

Added rotation coupling logic that applies after gravity forces:
- `ApplyRotationCoupling(CelestialBody)`: Adjusts object velocity to match planet rotation
- Only applies within 2× planet radius
- Coupling strength falls off linearly with distance from surface
- Formula: Force = tangentialDirection × speedDifference × couplingStrength × mass

Parameters:
- `applyRotationCoupling`: Enable/disable coupling (default: true)
- Works automatically for all objects using Gravitatable (Player, SpaceShip, etc.)

### Player Movement (Moveable.cs)

Added strong grounded coupling:
- `ApplyGroundedRotationCoupling()`: Called when player is grounded
- Detects which celestial body player is standing on via raycast
- Applies strong velocity adjustment (10× coupling strength) to match surface velocity
- Ensures player maintains local position on rotating surface

Debug visualization added showing:
- Current rotating planet name and rotation rate
- Surface tangential velocity magnitude

### Groundable.cs

Enhanced to track ground contact:
- Stores `RaycastHit` information to identify ground collider
- `GetGroundCollider()`: Returns collider player is standing on
- Used to determine which planet's rotation to couple with

## Configuration

### Setting Up Planet Rotation

In Unity Editor or prefab YAML:
1. Set `rotationAxis` to desired axis (typically {x: 0, y: 1, z: 0} for vertical)
2. Set `angularSpeedDegrees` to rotation rate (0 = no rotation)

Current planet configurations:
- **Timber Hearth**: 10°/s rotation around Y-axis
- **Ash Twin**: 15°/s rotation around Y-axis
- **Ember Twin**: 12°/s rotation around Y-axis
- **Sun Station**: 5°/s rotation around Y-axis
- **Sun**: 0°/s (stationary, no rotation)

## Physics Behavior

### When Grounded on Rotating Planet

1. Player raycast detects ground surface
2. `ApplyGroundedRotationCoupling()` identifies the planet
3. Calculates target tangential velocity at player position
4. Applies strong velocity adjustment (0.5 interpolation factor - closes 50% of gap per physics step)
5. Player maintains approximate local position as planet rotates beneath them

### When Airborne Near Rotating Planet

1. `Gravitatable.ApplyRotationCoupling()` runs for dominant gravity source
2. Calculates distance-based coupling strength (1.0 at surface, 0.0 at 2× radius)
3. Applies gentler force to gradually match rotation
4. Allows orbital mechanics while still feeling rotation effects

### When Jumping Off Surface

1. Player retains velocity at moment of jump
2. Velocity includes tangential component from surface rotation
3. Trajectory appears curved relative to rotating surface
4. Natural behavior for rotating reference frame

## Assumptions and Limitations

1. **Constant Angular Velocity**: Planets rotate at constant rate (no angular acceleration)
2. **Rigid Body Model**: Planets treated as rigid bodies with uniform rotation
3. **Simplified Coupling**: Uses velocity adjustment rather than full fictitious forces (centrifugal, Coriolis)
4. **No Frame Transforms**: All physics computed in inertial world frame, not local planetary frame
5. **Distance Threshold**: Coupling limited to 2× planet radius for performance and stability

## Tuning Parameters

### Grounded Coupling Strength (Moveable.cs)
```csharp
float groundedCouplingStrength = 0.5f;
```
Interpolation factor (0.0 to 1.0) - how much of the velocity gap to close per physics step. Higher values = faster matching of surface velocity when grounded. 0.5 closes 50% of the gap per step, providing strong coupling while remaining stable.

### Airborne Coupling Radius (Gravitatable.cs)
```csharp
float couplingRadius = celestialBody.radius * 2f;
```
Defines maximum distance for rotation coupling effects

### Angular Speeds (Planet Prefabs)
- Typical range: 0-20°/s
- Higher values = faster visible rotation, stronger velocity effects
- Consider radius when tuning (larger radius = higher surface speeds at same angular rate)

## Testing Recommendations

1. Stand on rotating planet and observe staying in place relative to surface
2. Jump and observe curved trajectory relative to surface
3. Land ship on rotating planet and verify it doesn't drift
4. Observe debug output showing tangential velocities
5. Test at different distances from planet (coupling falloff)
6. Verify non-rotating planets (angularSpeedDegrees = 0) behave normally

## Future Enhancements

Possible improvements (out of scope for this implementation):
- Full Coriolis force effects for moving objects
- Angular acceleration/deceleration
- Non-uniform rotation (e.g., tidal locking)
- Frame-based rendering (camera rotation with planet)
- Rotation visualization (rotation axis gizmos)
