# Planet Rotation Testing Guide

This guide provides instructions for testing the planet axial rotation feature in the Outer Wilds Clone.

## Setup

To enable rotation on a planet:

1. Open the Unity scene containing your celestial bodies
2. Select a planet GameObject (e.g., a CelestialBody component)
3. In the Inspector, find the CelestialBody component
4. Set the following values:
   - **Rotation Axis**: (0, 1, 0) for Y-axis rotation (default)
   - **Angular Velocity**: 10.0 for fast visible rotation during testing

## Test Scenarios

### Test 1: Basic Planet Rotation
**Expected Result**: The planet should rotate smoothly around the Y-axis.

1. Set angularVelocity = 10.0
2. Enter Play mode
3. Observe the planet from a distance
4. Verify the planet rotates continuously

### Test 2: Player Grounding on Rotating Planet
**Expected Result**: Player remains fixed to the rotating surface.

1. Set angularVelocity = 10.0 on a planet
2. Land the player on the planet's surface
3. Stand still (no input)
4. Observe that the player rotates with the planet
5. The player should not slide or drift off the surface

### Test 3: Walking on Rotating Planet
**Expected Result**: Player can walk normally despite rotation.

1. Set angularVelocity = 10.0
2. Land on the planet
3. Walk in different directions
4. Movement should feel natural and stable
5. Player should not experience sliding or unusual forces

### Test 4: Jumping on Rotating Planet
**Expected Result**: Player maintains rotational momentum when jumping.

1. Set angularVelocity = 10.0
2. Land on the planet
3. Jump into the air
4. While airborne, observe that the surface continues rotating beneath
5. Upon landing, the player should re-sync with the surface
6. No jarring transitions or position snapping

### Test 5: Different Rotation Speeds
**Expected Result**: System works at various speeds.

Test with:
- angularVelocity = 0.5 (slow, Earth-like)
- angularVelocity = 5.0 (moderate)
- angularVelocity = 20.0 (fast)
- angularVelocity = -10.0 (reverse rotation)

All speeds should maintain stable grounding.

### Test 6: Different Rotation Axes
**Expected Result**: Rotation works around any axis.

Test with:
- rotationAxis = (0, 1, 0) - Y-axis (vertical)
- rotationAxis = (1, 0, 0) - X-axis (horizontal)
- rotationAxis = (0, 0, 1) - Z-axis (horizontal)
- rotationAxis = (1, 1, 0) - Diagonal axis (normalized automatically)

### Test 7: Non-Rotating Planet
**Expected Result**: Normal behavior when rotation is disabled.

1. Set angularVelocity = 0.0
2. Verify the planet does not rotate
3. Player should function normally with standard grounding

### Test 8: Transition Between Planets
**Expected Result**: Smooth transition between different rotation states.

1. Set Planet A angularVelocity = 10.0
2. Set Planet B angularVelocity = 0.0
3. Launch from Planet A to Planet B
4. Land on Planet B
5. Verify no physics glitches during transition

### Test 9: High-Speed Rotation Edge Case
**Expected Result**: System remains stable even at extreme speeds.

1. Set angularVelocity = 100.0 (very fast)
2. Test player grounding
3. Centripetal force should keep player attached
4. May experience some physics instability - this is expected at extreme values

### Test 10: Jetpack on Rotating Planet
**Expected Result**: Jetpack works normally near rotating surfaces.

1. Set angularVelocity = 10.0
2. Land on planet
3. Use jetpack to hover just above surface
4. The surface should rotate beneath you
5. Re-landing should sync you back to the surface

## Expected Physics Behavior

### What Should Happen
- Player remains at the same local position on the rotating surface
- Walking feels natural and stable
- Jumping and landing work smoothly
- No visual jittering or sliding
- Rotation is smooth and frame-rate independent

### What Should NOT Happen
- Player sliding across the surface while standing still
- Player being flung off due to centrifugal force
- Jittering or stuttering during rotation
- Position "snapping" when landing
- Unusual velocity spikes

## Debugging Tips

### If Player Slides Off
- Check that the grounding raycast is hitting the planet collider
- Verify the Groundable component is detecting the CelestialBody
- Increase centripetal force by adjusting the coefficient in GroundedRotationSync
- Ensure the planet has the correct physics layer ("Planets")

### If Player Jitters
- Verify Time.fixedDeltaTime is being used (not Time.deltaTime)
- Check that rotation sync is only called in FixedUpdate
- Ensure the rotation axis is normalized
- Lower the angular velocity to a reasonable value

### If Rotation Feels Wrong
- Verify the rotation axis direction (use Unity's gizmos)
- Check the sign of angularVelocity (positive vs negative)
- Use Scene view to observe rotation from different angles
- Test with a slow rotation speed first (e.g., 1.0)

## Performance Testing

Monitor these metrics during testing:

1. **Frame Rate**: Should remain consistent with rotation enabled
2. **Physics Step Time**: Check in Profiler (should be minimal increase)
3. **Memory Usage**: No memory leaks from rotation tracking
4. **CPU Usage**: GroundedRotationSync should have minimal overhead

## Console Monitoring

Watch for these debug messages:
- Grounding status ("IsOnTheGround = true/false")
- Gravity force magnitudes
- Any error messages related to null celestial bodies

## Known Limitations

1. Extremely high angular velocities (>50 degrees/sec) may cause instability
2. Very small planets (radius < 5 units) may have unreliable grounding detection
3. Multiple nested rotating reference frames are not supported
4. Coriolis effect is not simulated for projectiles

## Success Criteria

The implementation is successful if:
- [ ] Planets rotate smoothly and continuously
- [ ] Player remains attached to rotating surfaces without sliding
- [ ] Walking, jumping, and jetpack work naturally on rotating planets
- [ ] No physics glitches or jittering occurs
- [ ] Performance impact is negligible
- [ ] System works with various rotation speeds and axes
- [ ] Transitions between planets are smooth

## Reporting Issues

If you encounter problems, provide:
1. Angular velocity and rotation axis values used
2. Planet size/radius
3. Description of the unexpected behavior
4. Console log output (if any errors)
5. Steps to reproduce
