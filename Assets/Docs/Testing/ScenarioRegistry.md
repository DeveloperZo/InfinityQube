# Scenario Registry

> Maps features/scripts to test scenarios for manual and AI-assisted validation.
> Status: ⏳ = Pending, ✅ = Passing, ❌ = Failing

---

## How to Use

1. **Load scenario**: Menu → Tools → Infinity Qube → Scenarios → Scenario Window
2. **Enter Play Mode** and click "Load Scenario"
3. **Observe** the game state - wave runs, check expected behavior
4. **Manually verify** outcomes match expectations

---

## Death Scenarios

### D001: Basic Player Death
| Field | Value |
|-------|-------|
| **Status** | ⏳ Pending |
| **Scripts** | PlayerManager, CubeManager |
| **Tags** | death, player, unit |

**Setup**: Wave Unit cube at (3,8), player at (3,2), wave starts  
**Expected**: Cube moves down, hits player, player dies (deaths=1) and respawns  
**Verify**: `PlayerManager.playerDeaths == 1` after collision

---

## Capture Scenarios

### Keystone_UnitVsUnit
**Setup**: Wave Unit at (3,15), Player Unit at (3,2)  
**Expected**: Cubes collide, 1 capture  

### Keystone_MatrixCollision  
**Setup**: 3x3 Wave Units, Player Matrix cube  
**Expected**: 9 captures (area effect)

---

## By Script

| Script | Scenarios |
|--------|-----------|
| PlayerManager.cs | D001_BasicPlayerDeath |
| CubeManager.cs | D001, Keystone_UnitVsUnit, Keystone_MatrixCollision |

---

## Adding Scenarios

Use menu: **Tools → Infinity Qube → Scenarios → Create Empty Scenario**

Or modify `ScenarioEditorTools.cs` and run **Create SDD Scenarios**.
