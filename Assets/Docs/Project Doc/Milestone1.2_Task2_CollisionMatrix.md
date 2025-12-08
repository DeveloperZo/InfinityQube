# Milestone 1.2 - Task 2: Cube Collision Matrix

> **Status**: ✅ **COMPLETE - All Collisions Defined**  
> **Date**: December 2025

---

## Overview

This document defines the complete collision matrix for all cube type combinations. All player cube → wave cube interactions have been specified with their behaviors, triggers, and strategic implications.

---

## Refined Collision Table

| Player Cube | Wave Cube | Behavior | Description | Status |
|-------------|-----------|----------|-------------|--------|
| Unit | Unit | Standard capture | Player Unit collides with Wave Unit and removes it from the grid | ✓ |
| Unit | Matrix | 2x2 area capture | Player Unit collides with Wave Matrix and triggers a 2x2 capture area centered on collision point | ✓ |
| Unit | Recursion | Column capture | Player Unit collides with Wave Recursion and auto-captures 3 cubes as wave passes over collision tile | ✓ |
| Unit | Infinity | Face paint, Unit destroyed | Player Unit collides with Wave Infinity, paints collision face, Unit destroyed; when face touches grid, tile auto-captures (Unit behavior) | ✓ |
| Matrix | Unit | 2x2 area capture | Player Matrix collides with Wave Unit and triggers a 2x2 capture area expanding from Matrix's position | ✓ |
| Matrix | Matrix | Triggerable 3x3 marker | Player Matrix collides with Wave Matrix and creates a 3x3 manual marker centered on collision point; single trigger | ✓ |
| Matrix | Recursion | Degrading 2x2 marker | Player Matrix collides with Wave Recursion and creates a 2x2 area marker; each tile has 1 charge; player manually triggers; area shrinks over triggers | ✓ |
| Matrix | Infinity | Face paint, Matrix destroyed | Player Matrix collides with Wave Infinity, paints collision face, Matrix destroyed; when face touches grid, tile becomes 2x2 manual marker (Matrix behavior) | ✓ |
| Recursion | Unit | Column capture | Player Recursion collides with Wave Unit and auto-captures 3 cubes as wave passes over collision tile | ✓ |
| Recursion | Matrix | Auto 1x3 marker | Player Recursion collides with Wave Matrix and creates a 1x3 vertical marker (3 tiles deep); each tile auto-captures as wave passes | ✓ |
| Recursion | Recursion | Cross marker | Player Recursion collides with Wave Recursion and creates a cross-shaped marker (5 tiles - 1x3 vertical + 1x3 horizontal, overlapping at center); each tile auto-captures as wave passes | ✓ |
| Recursion | Infinity | Face paint, Recursion destroyed | Player Recursion collides with Wave Infinity, paints collision face, Recursion destroyed; when face touches grid, tile auto-captures 3 cubes (Recursion behavior) | ✓ |
| Infinity | Unit | Wave join | Player Infinity collides with Wave Unit, removes Unit, takes its position; moves with wave; passes through at player edge | ✓ |
| Infinity | Matrix | Face paint, continue up | Player Infinity collides with Wave Matrix, paints collision face, continues up; when face touches grid, tile becomes 2x2 manual marker (Matrix behavior) | ✓ |
| Infinity | Recursion | Face paint, continue up | Player Infinity collides with Wave Recursion, paints collision face, continues up; when face touches grid, tile auto-captures 3 cubes (Recursion behavior) | ✓ |
| Infinity | Infinity | Face paint, resonance | Player Infinity collides with Wave Infinity, paints collision face, continues up; when face touches grid, ALL Infinity cubes on grid become phaseable for that turn | ✓ |

---

## Quick Reference: Cube Identities

| Cube | Identity | Trigger Type | Shape Language |
|------|----------|--------------|----------------|
| Unit | Simple, foundational | Instant | Single tile |
| Matrix | Area, expansion | Manual | 2x2, 3x3 squares |
| Recursion | Repetition, concentration | Auto | 1x3 lines, cross |
| Infinity | Immutable, rhythmic | Painted face (inherits target behavior) | N/A - affects other cubes |

---

## Key Design Decisions

### 1. Same-Type Matching Rewards

**Matrix+Matrix**: 
- Creates 3x3 triggerable marker (enhanced reward)
- Rewards player for matching types
- Strategic depth: players want to match Matrix cubes for larger area effects

**Recursion+Recursion**:
- Creates cross-shaped marker (5 tiles)
- Auto-captures as wave passes
- Rewards matching with expanded capture area

### 2. Infinity Collision Behavior

**Infinity+Infinity**:
- Face paint + resonance effect
- When painted face touches grid, ALL Infinity cubes become phaseable
- Creates strategic opportunity for that turn
- Maintains Infinity's immutable nature while providing interaction

**Infinity + Other Types**:
- Face paint mechanism
- Inherits behavior of target cube type when face touches grid
- Infinity continues upward (doesn't stop)
- Creates delayed effects based on painted face

### 3. Area Effect Variations

**Matrix Collisions**:
- Matrix+Unit: 2x2 area (standard Matrix behavior)
- Matrix+Matrix: 3x3 triggerable marker (enhanced reward)
- Matrix+Recursion: Degrading 2x2 marker (shrinks over triggers)

**Recursion Collisions**:
- Recursion+Unit: Column capture (3 cubes)
- Recursion+Matrix: Auto 1x3 vertical marker
- Recursion+Recursion: Cross marker (5 tiles)

### 4. Face Painting System Integration

All Infinity collisions use face painting:
- Painted face inherits behavior of target cube type
- Effect triggers when face touches grid
- Creates delayed strategic effects
- Maintains Infinity's unique identity

---

## Implementation Notes

### Collision Detection
- Collisions detected in `PlayerMarkerSystem.ProcessCollisionAtPosition()`
- Same-type matching detected via `isSameTypeMatch` parameter
- Area effects calculated via `GetAreaPositions()`

### Marker Generation
- Cube markers generated in `ProcessCubeCapture()`
- Matrix+Matrix: Creates 3x3 cube marker (triggered with R key)
- Recursion+Recursion: Creates cross marker (auto-captures)
- Other combinations: Various marker types based on collision

### Face Painting
- Face painting handled in `CubeManager.PaintFace()`
- Face status affects cube behavior when active
- Infinity collisions trigger face painting on collision face

---

## Strategic Implications

### Matching Rewards
- **Matrix+Matrix**: Largest reward (3x3 triggerable marker)
- **Recursion+Recursion**: Expanded capture area (cross shape)
- Encourages players to match cube types for enhanced effects

### Infinity Interactions
- Infinity+Infinity: Resonance effect creates strategic opportunities
- Infinity+Other: Face painting creates delayed effects
- Maintains Infinity's unique role while providing interaction

### Area Effect Strategy
- Matrix provides area coverage (2x2 standard, 3x3 for matching)
- Recursion provides column/line coverage (1x3, cross)
- Unit provides single-tile precision

---

## Testing Checklist

- [ ] Unit+Unit: Standard capture works
- [ ] Unit+Matrix: 2x2 area capture triggers correctly
- [ ] Unit+Recursion: Column capture (3 cubes) works
- [ ] Unit+Infinity: Face paint + Unit destroyed
- [ ] Matrix+Unit: 2x2 area capture from Matrix position
- [ ] Matrix+Matrix: 3x3 triggerable marker created
- [ ] Matrix+Recursion: Degrading 2x2 marker created
- [ ] Matrix+Infinity: Face paint + Matrix destroyed
- [ ] Recursion+Unit: Column capture works
- [ ] Recursion+Matrix: Auto 1x3 vertical marker created
- [ ] Recursion+Recursion: Cross marker (5 tiles) created
- [ ] Recursion+Infinity: Face paint + Recursion destroyed
- [ ] Infinity+Unit: Wave join behavior
- [ ] Infinity+Matrix: Face paint + continue up
- [ ] Infinity+Recursion: Face paint + continue up
- [ ] Infinity+Infinity: Face paint + resonance effect

---

## Related Documentation

- **Gameplay Mechanics**: [3_GameplayMechanics.md](3_GameplayMechanics.md#cube-collision-matrix) - Full collision table
- **Task 1**: [Milestone1.2_Task1_Verification.md](Milestone1.2_Task1_Verification.md) - Marker placement and spawning
- **Planning**: [Milestone1.2_Planning.md](Milestone1.2_Planning.md) - Overall milestone planning

---

**Last Updated**: December 2025  
**Status**: ✅ All collision combinations defined and documented

