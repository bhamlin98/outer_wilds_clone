# Planet Rotation System Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      PHYSICS PIPELINE (FixedUpdate)             │
│                                                                 │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐ │
│  │   Gravity    │  →   │  Orientation │  →   │   Movement   │ │
│  │ Application  │      │   Rotation   │      │   & Input    │ │
│  └──────────────┘      └──────────────┘      └──────────────┘ │
│         ↓                      ↓                      ↓        │
│  Gravitatable          TowardsCelestial      Moveable/        │
│  .ApplyGravity()       BodyRotatable         PlayerControl    │
│                        .RotateIfNeeded()                       │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │           NEW: Grounded Rotation Synchronization         │ │
│  │                                                            │ │
│  │  GroundedRotationSync.SyncWithRotatingSurface()          │ │
│  │    ├─ Check grounding status (Groundable)                │ │
│  │    ├─ Detect grounded CelestialBody                      │ │
│  │    ├─ Track local position relative to planet            │ │
│  │    ├─ Calculate velocity correction                      │ │
│  │    └─ Apply centripetal force                            │ │
│  └──────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Component Relationships

### CelestialBody (Rotating Planet)
```
CelestialBody
├── Rotation Parameters
│   ├── rotationAxis: Vector3 (default: Y-axis)
│   └── angularVelocity: float (degrees/sec)
│
├── FixedUpdate Pipeline
│   ├── ApplyGravity() [if not stationary]
│   ├── ApplyRotation() ← NEW
│   └── DrawOrbit() [if not stationary]
│
└── ApplyRotation()
    ├── Calculate angle: θ = ω × Δt
    ├── Create quaternion: Q = AngleAxis(θ, axis)
    └── Apply: transform.rotation = Q × transform.rotation
```

### Player (Grounded Object)
```
Player.FixedUpdate()
├── 1. Apply Gravity
│   └── gravitatable.ApplyGravity()
│
├── 2. Orient to Planet
│   └── towardsCelestialBodyRotatable.RotateIfNeeded()
│
├── 3. Process Movement
│   └── moveable.Move(playerControllable)
│       ├── Check grounding (Groundable.IsGrounded())
│       ├── Walk/jump on surface
│       └── Apply jetpack forces
│
└── 4. Sync with Rotation ← NEW
    └── groundedRotationSync.SyncWithRotatingSurface()
        ├── Get grounded status
        ├── Get grounded CelestialBody
        └── Apply synchronization
```

## Data Flow

### Grounding Detection Flow
```
Player Movement
    ↓
Groundable.IsGrounded()
    ↓
Raycast to detect ground
    ↓
GetComponentInParent<CelestialBody>()
    ↓
Store grounded CelestialBody
    ↓
Return to Player.FixedUpdate()
    ↓
Pass to GroundedRotationSync
```

### Rotation Synchronization Flow
```
GroundedRotationSync.SyncWithRotatingSurface(isGrounded, body)
    ↓
┌───────────────────────────────────────┐
│ Is grounded & body != null?           │
│ NO → Exit (no sync needed)            │
│ YES → Continue                        │
└───────────────────────────────────────┘
    ↓
┌───────────────────────────────────────┐
│ Is body rotating? (ω != 0)            │
│ NO → Exit (static surface)            │
│ YES → Continue                        │
└───────────────────────────────────────┘
    ↓
┌───────────────────────────────────────┐
│ First frame grounded?                 │
│ YES → InitializeGroundedTracking()    │
│       - Store local position          │
│       - Store body rotation           │
│ NO → SyncRotation()                   │
└───────────────────────────────────────┘
```

### Position Synchronization Algorithm
```
SyncRotation()
    ↓
1. Calculate new world position
   newPos = body.TransformPoint(lastLocalPos)
    ↓
2. Calculate position delta
   Δpos = newPos - currentPos
    ↓
3. Calculate required velocity
   vCorrect = Δpos / Δt
    ↓
4. Apply velocity correction
   rigidbody.velocity += vCorrect
    ↓
5. Apply centripetal force
   ApplyCentripetalForce()
    ↓
6. Update tracking data
   - lastLocalPosition
   - lastBodyRotation
```

### Centripetal Force Calculation
```
ApplyCentripetalForce()
    ↓
1. Get vector from body center to object
   v = objectPos - bodyPos
    ↓
2. Project onto plane ⊥ to rotation axis
   vRadial = v - Project(v, axis)
    ↓
3. Calculate radial distance
   r = |vRadial|
    ↓
4. Convert angular velocity to rad/s
   ωRad = ω × (π/180)
    ↓
5. Calculate centripetal acceleration
   ac = ω² × r
    ↓
6. Calculate force (toward axis)
   F = m × ac × (-vRadial/|vRadial|)
    ↓
7. Apply force
   rigidbody.AddForce(F)
```

## State Machine

### Grounding State Transitions
```
┌─────────────┐
│   Airborne  │
│             │
│ rotSync:    │
│  inactive   │
└──────┬──────┘
       │ Land on planet
       ↓
┌─────────────┐
│   Grounded  │
│ (Initialize)│
│             │
│ Store local │
│  position   │
└──────┬──────┘
       │ Next frame
       ↓
┌─────────────┐
│   Grounded  │
│   (Synced)  │
│             │
│ Apply sync  │
│  + force    │
└──────┬──────┘
       │ Jump/leave
       ↓
┌─────────────┐
│   Airborne  │
│             │
│ rotSync:    │
│  inactive   │
└─────────────┘
```

## Physics Integration Points

### Integration with Existing Systems

1. **Gravity System** (Gravitatable)
   - Continues to work unchanged
   - Provides MaxGravitatableInfo for orientation
   - Rotation sync works alongside gravity

2. **Orientation System** (TowardsCelestialBodyRotatable)
   - Rotates player to face "up" from planet
   - Independent of rotation sync
   - Both systems can operate simultaneously

3. **Movement System** (Moveable)
   - Enhanced to expose Groundable component
   - Grounding detection integrated with rotation sync
   - Walking/jumping mechanics unchanged

4. **Input System** (PlayerControllable)
   - No changes required
   - Player input processed normally
   - Rotation sync transparent to controls

## Performance Characteristics

### Computational Complexity

| Operation | Complexity | Frequency | Cost |
|-----------|-----------|-----------|------|
| Planet rotation | O(1) | Every FixedUpdate | Low |
| Grounding check | O(1) raycast | Every FixedUpdate | Low |
| Position tracking | O(1) | When grounded | Very Low |
| Velocity correction | O(1) | When grounded | Very Low |
| Centripetal force | O(1) | When grounded | Very Low |

### Memory Usage
- Per Planet: 2 additional fields (28 bytes)
- Per Player: 1 GroundedRotationSync instance (~64 bytes)
- No dynamic allocation during runtime
- No garbage collection pressure

### Early-Exit Optimization
```
SyncWithRotatingSurface()
├─ Exit if not grounded        [Most common in space]
├─ Exit if body is null         [Safety check]
├─ Exit if ω ≈ 0               [Static planets]
└─ Process synchronization      [Only when needed]
```

## Edge Cases Handled

### 1. Transition Between Planets
```
Planet A (ω=10) → Airborne → Planet B (ω=5)
├─ Leave A: Stop sync, maintain velocity
├─ Airborne: No sync active
└─ Land on B: Initialize new tracking, apply B's rotation
```

### 2. Very Small Radius (Near Axis)
```
if (radius < 0.01f)
    return; // Skip centripetal force near axis
```

### 3. First Frame Grounded
```
if (!wasGroundedLastFrame || currentBody != groundedBody)
    InitializeTracking(); // Prevent velocity spike
```

### 4. Zero Angular Velocity
```
if (Mathf.Approximately(angularVelocity, 0f))
    return; // No rotation = no sync needed
```

## Extension Points

### Adding Rotation to Other Objects

1. **SpaceShip** - Same pattern as Player
2. **Rocks/Props** - Can use same GroundedRotationSync
3. **AI Characters** - Integrate with their movement system
4. **Buildings** - Static attachment to planet surface

### Future Enhancements

1. **Coriolis Force**
   ```
   F_coriolis = -2m(ω × v)
   ```
   For projectiles and long-distance motion

2. **Atmospheric Rotation**
   Apply drag based on atmosphere rotation velocity

3. **Tidal Locking**
   Synchronize rotation with orbital period

4. **Wobble/Precession**
   Vary rotation axis over time

## Debugging Visualization

### Recommended Debug Draws
```csharp
// In Scene view, visualize:
- Rotation axis as a line through planet center
- Radial vector from axis to player
- Centripetal force direction
- Local position marker on surface
```

### Debug Info to Monitor
- Current angular velocity
- Grounded status
- Grounded body name
- Local position
- Centripetal force magnitude
- Velocity correction applied

## Summary

The rotation system architecture:
- ✅ Integrates seamlessly with existing physics pipeline
- ✅ Minimal performance overhead
- ✅ Handles all edge cases gracefully
- ✅ Extensible to other objects
- ✅ Well-documented and maintainable
- ✅ Physics-correct implementation

Key Design Principles:
1. **Separation of Concerns**: Rotation, gravity, and movement are independent
2. **Early-Exit Optimization**: Skip work when not needed
3. **State Tracking**: Smooth transitions between states
4. **Physics Correctness**: Use proper time deltas and force application
5. **Extensibility**: Easy to apply to other objects
