# Milestone 1.11 — RPG Implementation Context (Attunements & Progression)

Purpose:
Supporting context for Milestone 1.11 so the roadmap stays readable while retaining full RPG details.

---

## Foundation (already implemented in Milestone 1.2)
- SaveManager — full persistence system (JSON; Steam Cloud-ready)
- PlayerProgression — currency, attunements, stage progress
- AttunementManager — definitions + query API
- All 9 attunements integrated into CubeCollisionManager
- Economy wired to GameEvents:
  - Earn: 100 shards/wave (first playthrough only)
  - Anti-farming: no currency on replay
- Hub foundation:
  - 3 clickable buildings + UI panel system
  - Debug panel for attunement testing (F12 → Player → Attunements)

---

## Attunements (Definitions)

Design rule:
- Attunements modify cube behavior, not the collision matrix (players learn the matrix once).

### Matrix Attunements (Expansion Theme)
- Expanded Expansion: +1 area dimensions (2x2 → 3x3)
- Concentrated Expansion: +1 charge per tile
- Phaseable Expansion: Matrix vs Matrix also paints wave cube face

### Recursion Attunements (Concentration Theme)
- Concentrated Concentration: +2 charges (3 → 5)
- Expanded Concentration: +1 tile to pattern
- Phaseable Concentration: Recursion vs Recursion also paints wave cube face

### Infinity Attunements (Phaseability Theme)
- Potent Matrix Paint: +1 charge on Matrix painted faces
- Potent Recursion Paint: +1 charge on Recursion painted faces
- Untethered: vs Unit = destroy + continue (no wave join)

---

## Hub Areas (UI Targets)
Hub: Infinity’s Axiom (Stage 100 within unified Stage scene)

- Celestial Atlas — Stage selection
- Resonance Alignment Chamber — Attunement selection and unlocks
- Observation Chronicle — Stats/history display

---

## Economy (Reference)
| Aspect | Value |
|---|---|
| Currency | Axiom Shards |
| Earn | 100 shards flat per wave |
| Spend | Attunement unlocks (100–250 each) |
| Replay | No currency (anti-farming) |

---

## Milestone 1.11 UI Completion Expectations

### Resonance Alignment Chamber (Attunements)
- Display all 9 attunements with descriptions
- Show unlock status and cost
- Purchase/unlock flow with confirmation
- Equip flow with clear “equipped” state
- Visual feedback for equipped attunements
- Persistence: equipped + unlocked states survive restart

### Celestial Atlas (Stage Selection)
- Display available stages
- Show completion status
- Show unlock state (if applicable)
- Launch stage from selection
- Optional: stage preview/description

### Observation Chronicle (Stats/History)
- Display player statistics (lifetime + recent session)
- Stage completion history
- Attunement usage stats (optional, if already tracked)
- Session history summary (optional, if available)

---

## Validation Checklist (Copy into milestone if needed)
- Fresh save → earn shards → unlock an attunement → equip → quit → relaunch → verify state
- Equip attunement → enter stage → verify behavior change manifests
- Currency display correctness across hub ↔ stage transitions
- Stage select reflects completion/unlock state correctly
