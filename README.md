# InfinityQube

> **A grid-based tactical puzzle game inspired by Intelligent Qube, featuring a novel bidirectional wave system where player-placed markers transform into backward-moving cubes that collide with incoming formations.**

**Status:** Development concluded February 2026  
**Engine:** Unity 6 (6000.2.14f1) | **Platform:** PC (Windows) | **Language:** C#

---

## What Is InfinityQube?

[Intelligent Qube](https://en.wikipedia.org/wiki/Intelligent_Qube) (also called IQ) was a PlayStation 1 puzzle game from 1998 where players stood on a grid and tried to capture advancing cube waves before they rolled off the edge. It was a cult classic that never got a proper modern successor.

InfinityQube takes that foundation and adds a core innovation: **the Symmetrical Wave System**. Instead of capturing cubes in place, players place markers on the grid. When the wave advances forward, those markers transform into player cubes that move *backward* — creating calculated collisions at intersection points. The infinity symbol becomes gameplay: two halves of a pattern meeting in the middle.

The core mechanic works in five steps each turn:

```
1. Wave cubes move DOWN one row          (threats advance)
2. Markers on tiles transform into cubes  (your markers become your weapons)
3. Player cubes move UP one row           (your cubes intercept)
4. Collisions resolve                     (16-type matrix determines outcomes)
5. Results apply                          (captures, penalties, rewards)

         WAVE CUBES (advancing down)
         [U] [M] [∞] [R] [U]
              ↓  ↓  ↓
         ═══ GRID TILES ═══
              ↑  ↑  ↑
         [u]    [m]    [u]
         PLAYER CUBES (moving up, transformed from markers)

  U/M/R/∞ = Wave cubes    u/m/r/∞ = Player cubes
```

When a player cube meets a wave cube, the collision is resolved based on a **16-combination matrix** — 4 cube types (Unit, Matrix, Recursion, Infinity) against 4 cube types. Every pairing has a unique outcome. An Infinity cube is completely immutable to everything except another Infinity cube. A Recursion cube doesn't capture — it rearranges the board through swap mechanics. These aren't just complexity layers; they're fundamentally different verbs the player wields.

---

## Why Development Stopped

An honest [market analysis](Assets/Docs/Project%20Doc/MarketAnalysis.md) in February 2026 identified structural challenges:

- **Genre identity problem** — The game sits between "tactical puzzle" (think-then-act) and "action-puzzle" (react-in-real-time). Steam players shop by mental model, and InfinityQube didn't clearly match either audience's expectations.
- **Missing replayability hooks** — 12 linear stages with no meta-progression, leaderboards, or run variance meant a single-playthrough experience competing against games with 10x the content.
- **No clear "aha!" moment** — The collision matrix is rich but no single combination creates the kind of emergent surprise that makes players tell their friends about the game (think "Wait, I can push the RULE itself?" in Baba Is You).
- **9+ months of remaining work** — Approximately 202 estimated points remained across 4 development phases.

The Symmetrical Wave System was confirmed as genuinely novel — no other game on Steam offers markers-that-become-backward-moving-cubes. But a novel core mechanic inside an unclear product isn't enough to ship.

---

## Documentation Guide

This project produced an unusually large documentation corpus — 25+ documents spanning game design, technical architecture, market analysis, and retrospective analysis. For someone wanting to understand what was built and how AI-assisted development worked in practice, these are the key entry points:

### Start Here

| Document | What It Is | Why Read It |
|----------|-----------|-------------|
| [Axiomatic Additions](Assets/Docs/Project%20Doc/AxiomaticAdditions.md) | Project retrospective — accomplishments, weaknesses, full infrastructure catalog, git history analysis | **The single best document for understanding the full project.** Covers what worked, what didn't, every auxiliary system built, and the complete development timeline derived from git data. |
| [Market Analysis](Assets/Docs/Project%20Doc/MarketAnalysis.md) | Honest product-market fit assessment | The document that led to stopping development. Unflinching analysis of genre identity, replayability gaps, and remaining effort. |
| [Milestone Tracking](Assets/Docs/Project%20Doc/MilestoneTracking.md) | Completed work, velocity metrics, commit references | The quantitative record — every milestone, every point estimate, weekly/monthly commit data, velocity trends. |

### Game Design

| Document | What It Is |
|----------|-----------|
| [Game Design Document](Assets/Docs/Project%20Doc/GameDesignDocument.md) | Master reference for all gameplay systems and mechanics |
| [Gameplay Mechanics](Assets/Docs/Project%20Doc/3_GameplayMechanics.md) | Detailed system documentation — collision matrix, marker types, wave progression |
| [MDA Framework Analysis](Assets/Docs/Project%20Doc/MDA_Framework.md) | Formal Mechanics-Dynamics-Aesthetics analysis tracing how cube types create emergent dynamics and aesthetic experiences |
| [Level Design](Assets/Docs/Project%20Doc/4_LevelDesign.md) | Stage design philosophy and wave authoring guidelines |
| [Puzzle Heuristics](Assets/Docs/Project%20Doc/5_PuzzleHeuristics.md) | Puzzle design rules and difficulty curve principles |

### Style & Identity

| Document | What It Is |
|----------|-----------|
| [Identity Sheets](Assets/Docs/Project%20Doc/StyleBible/IdentitySheet.md) | Core concept identity sheets — compact reference for look/feel/sound/meaning of each element |
| [Visual Architecture](Assets/Docs/Project%20Doc/StyleBible/5_VisualArchitecture.md) | "Cosmic lo-fi" aesthetic — dark means danger, light means safety |
| [Sound Architecture](Assets/Docs/Project%20Doc/StyleBible/6_SoundArchitecture.md) | "Cosmic jazz" audio philosophy (designed but never fully realized) |

### Technical & Infrastructure

| Document | What It Is |
|----------|-----------|
| [Technical Debt](Assets/Docs/Technical%20Doc/TechnicalDebt.md) | File size violations, cleanup plans, architecture health scoring (6.5/10) |
| [Technical Critiques](Assets/Docs/Technical%20Doc/TechnicalCritiques.md) | Codebase analysis with specific improvement recommendations |
| [Debug Panel Guide](Assets/Docs/Technical%20Doc/DebugPanelGuide.md) | F12 debug system usage and panel reference |
| [Testing & Validation](Assets/Docs/Technical%20Doc/UnityTestingValidationSystem.md) | Automated testing pipeline bridging VS Code to Unity |
| [File Logging System](Assets/Docs/Technical%20Doc/FileLoggingSystem.md) | Logging infrastructure documentation |

### Planning & Strategy

| Document | What It Is |
|----------|-----------|
| [Roadmap](Assets/Docs/Project%20Doc/Roadmap.md) | 4-phase development plan with point estimates for every milestone |
| [Executive Summary](Assets/Docs/Project%20Doc/1_ExecutiveSummary.md) | Project positioning, target audience, key features |
| [Brainstorming](Assets/Docs/Project%20Doc/Brainstorming.md) | Design explorations and feature ideation |

### AI Governance (living in `.cursor/`)

The AI governance files aren't in the Docs folder — they live in `.cursor/rules/` and `.cursor/skills/` because they're loaded automatically by the Cursor IDE at the start of every AI session. See the [AI-Assisted Development](#ai-assisted-development) section below for details on what these files contain and how they work.

---

## What Was Built

### Core Systems

- **Symmetrical Wave System** — The defining innovation. Markers transform into backward-moving cubes on wave advancement, creating bidirectional collisions. Fully implemented and working.

- **Four-Tier Marker System** — Unit (precision single-tile), Matrix (2x2/3x3 area coverage), Recursion (swap-based repositioning), and Infinity (resonance trigger). Each with distinct economy rules — Unit markers regenerate, while Matrix (max 5), Recursion (max 8), and Infinity (max 3) markers are granted per-stage/wave with hard caps.

- **16-Type Collision Matrix** — Every combination of 4 wave cube types vs 4 player cube types produces a unique result:

| Player \ Wave | Unit | Matrix | Recursion | Infinity |
|---------------|------|--------|-----------|----------|
| **Unit** | Capture | +2x2 marker | Capture (1 hit) | Player cube destroyed |
| **Matrix** | 2x2 marker | +3x3 marker | 2x2 marker | Player cube destroyed |
| **Recursion** | 1 hit + swap | 1 hit + swap | Empowered swap | Player cube destroyed |
| **Infinity** | Wave cube destroyed | Wave cube destroyed | Wave cube destroyed | **Resonance** |

- **Resonance Mechanic** — When Player Infinity collides with Wave Infinity: immediate trigger, ALL wave Infinity cubes become phaseable for 2-4 moves (other player cubes can pass through them), and the Player Infinity cube is consumed. A sacrifice mechanic with board-wide consequences.

- **Face Painting & Corruption** — Infinity cubes paint corruption onto tiles they pass over, with a learnable 3-move telegraph system (dim > bright > trigger). Rotation tracking ensures only the front face paints, creating predictable danger zones.

- **Multi-Segment Grid System** — Grids support L-shaped, C-shaped, and S-shaped paths where cubes change direction at segment transitions. Configurable from 5x20 to 11x50 tiles.

- **Hub World ("Infinity's Axiom")** — Stage selection, statistics, and attunement management, implemented as Stage 100 within the existing scene system rather than a separate scene.

- **Attunement System** — 9 RPG-style abilities across Matrix and Recursion types (Mastery, Abundance, Infinity Forge, Clone, Infinity Gateway, etc.) that modify marker behavior. One active per marker type, integrated into the collision resolution system.

- **Scenario Testing Framework** — Automated gameplay validation using configured test stages for repeatable collision outcome testing.

- **Audio System Architecture** — 7 subsystem files (AudioManager, AudioSourcePool, AudioPlaybackSystem, AudioVolumeController, CubeAudioSystem, etc.) with the framework built but audio content pending.

- **Debug Infrastructure** — F12 debug panel with system state inspection, wave prototyper, console capture, and per-manager toggleable logging via a custom `LogExtensions` system.

### Content

| Element | Delivered |
|---------|-----------|
| Playable stages | 6 (Tutorial + Stages 1-5) + Hub |
| Cube types | 4 (Unit, Matrix, Recursion, Infinity) |
| Marker types | 4 primary + 2 generated (Cube, Swap) |
| Collision combinations | All 16 implemented |
| C# scripts | 156 files |
| ScriptableObject data files | 50+ (stages, waves, attunements, audio) |
| Design documents | 25+ files |
| Stages designed (on paper) | 13 total |

### Milestones Completed

The project used a **point estimation system** where 1 point equals approximately 1 day of active development work (5 points = 1 week, 22 points = 1 month at measured velocity).

| Milestone | What It Delivered | Points |
|-----------|-------------------|--------|
| 1.0 Core Game Development | Architecture, gameplay loop, wave system, markers, face painting, audio, debug tools | 65 |
| 1.1 Refine Markers | Collision matrix, resonance, enhanced face painting, marker economy | 14 |
| 1.2 RPG/Progression | Save system, attunements, hub foundation, economy wiring | 12 |
| 1.3 Database Enhancement | Central stage database, wave data system, attunement database, editor tools | 6 |
| 1.4 Stage Content Creation | 13 stages designed, 4 implemented, stage numbering convention | 20 |
| 1.5 Demo Stage Content | Tutorial stage, messaging system, splash-to-stage loop | 5 |
| 1.6 Unit & Infinity (Stages 1-2) | Unit marker mechanics validated, Infinity avoidance, penalty/scoring | 4 |
| 1.7 Matrix (Stages 3-5) | Matrix area capture, cube marker generation, Infinity barrier puzzles | 6 |
| 1.12 Hub Implementation | Full gameplay loop: Splash > Hub > Stage > Hub | 4 |
| 1.13 Advanced Grid System | Multi-segment grids (L/C/S shapes), segment-aware movement | 5 |
| 1.16 Scenario Testing | Automated scenario validation framework | 3 |

**Total: ~144 points across 11 milestones, with 8 formally completed and 130+ documented in [Milestone Tracking](Assets/Docs/Project%20Doc/MilestoneTracking.md). All of this in ~4.5 months of active development within a 9-month calendar span.**

The game was approximately 40% complete by point estimate, with Phases 2-4 (content polish, commercial prep, Steam integration) untouched. See [Axiomatic Additions](Assets/Docs/Project%20Doc/AxiomaticAdditions.md) for the full breakdown of what was accomplished and what fell short.

---

## AI-Assisted Development

This project was built by a solo developer using AI coding assistants as a core part of the workflow — not as an occasional helper, but as the primary implementation partner. The primary tools were [Cursor](https://cursor.com/) (an AI-native code editor built on VS Code) and Claude (Anthropic's large language model). The AI didn't just write code; it operated within a structured governance system that evolved over the project's lifetime. Understanding this infrastructure is key to understanding what was actually achieved.

### What the AI Did

The AI assistant handled the majority of code implementation, refactoring, documentation writing, and architectural analysis. The human developer provided creative direction, game design decisions, playtesting feedback, and approval for high-risk changes. In practice, the workflow looked like this:

1. **Human** decides what to build (e.g., "Implement the collision matrix for all 16 cube type combinations")
2. **Human** provides context via design documents and architecture rules
3. **AI** reads existing code, plans the implementation, and writes the code
4. **Human** reviews, playtests, identifies issues
5. **AI** iterates based on feedback
6. Repeat until the feature works correctly

This produced a measured development velocity of **22 points/month** — meaning approximately 22 days of productive output per calendar month of active development. For a solo developer on a complex Unity game with 156 scripts, this is significant.

### The AI Governance System

Rather than letting the AI operate freely, the project developed a structured system of **rules**, **skills**, **approval gates**, and **validation requirements** that constrained and guided AI behavior. These lived in the `.cursor/` directory and were automatically loaded into every AI session.

#### Rules (8 files, always-on context)

These are instructions the AI reads at the start of every interaction. They encode project knowledge, constraints, and hard rules:

| Rule File | What It Does |
|-----------|-------------|
| `core-constraints.mdc` | NEVER/ALWAYS rules — things the AI must never do (split core managers, bypass collision system, use FindObjectOfType in Update) and must always do (cache references, use LogExtensions, respect file size limits) |
| `game-architecture.mdc` | Complete game architecture documentation — cube types, marker economy, collision matrix, wave lifecycle, penalty/reward rules. Prevents the AI from making changes that violate game design. |
| `file-standards.mdc` | File naming conventions, required region structure for all classes, file size limits with decomposition patterns |
| `code-patterns.mdc` | Exact code patterns the AI must follow — singleton implementation, debug logging, manager reference caching, Odin Inspector usage |
| `validation.mdc` | Definition of Done — what must be true before the AI can consider a task complete (build validates, tests pass, file sizes within limits, performance acceptable) |
| `documentation-standards.mdc` | Rules separating Project Docs (no code) from Technical Docs (code allowed) — prevents the AI from putting implementation details in design documents |
| `approval-gates.mdc` | Complexity scoring system (1-10) with automatic approval triggers. Tasks scoring 7+ require human approval before the AI proceeds. Green/Yellow/Red zone classification for files. |
| `unity-patterns.mdc` | Unity-specific patterns — MonoBehaviour lifecycle, event subscription pairing, DOTween usage, coroutine management |

The **approval gates** system deserves special attention. Every task the AI undertook was scored 1-10 for complexity:

| Score | What It Means | AI Autonomy |
|-------|--------------|-------------|
| 1-3 | Single file, established patterns | Full autonomy — AI proceeds without asking |
| 4-6 | Multiple files, moderate complexity | AI proceeds but must run integration tests |
| 7-10 | Cross-system, architectural impact | **AI must stop and get human approval** |

Files were also classified into zones:
- **Green Zone** (full autonomy): Debug systems, UI, test files, data assets
- **Yellow Zone** (conditional): Adding private methods is fine; changing public APIs requires approval
- **Red Zone** (always requires approval): Enumerations.cs, core manager files, singleton implementations, collision matrix

This meant the AI could move fast on low-risk work while being forced to pause and explain itself before touching critical systems.

#### Skills (3 files, domain expertise)

Skills are longer reference documents the AI consults when working on specific types of tasks:

| Skill File | What It Contains |
|------------|-----------------|
| `SKILL.md` | Complete Unity game development guide tailored to this project — includes lessons learned from actual bugs (NullRef in CubeManager.MoveForward, stage progression failures from event timing), anti-patterns found in the codebase, file decomposition patterns that worked (the Tile system extraction), DOTween and Odin Inspector patterns |
| `debugging-guide.md` | Project-specific debugging workflows — F12 debug panel usage, common issues and their root causes (all drawn from actual project history), console log filtering by `[ClassName]` format |
| `testing-patterns.md` | Unity test patterns — edit mode, play mode, scene-based integration tests, mock patterns for manager references |

The key insight about skills is that they **encode lessons from failures**. The SKILL.md file has a "Recurring Bug Patterns" table that lists bugs the AI caused and how to prevent them. When the AI caused a NullReferenceException by using `FindFirstObjectByType` in a frequently-called method, that mistake was documented in the skill file so it would never happen again. The AI literally learned from its own mistakes across sessions.

#### Validation Requirements

Before the AI could mark any task as complete, it had to verify:

1. File sizes within limits (750 lines for core components, 600 for managers, 300 for utilities)
2. No partial implementations (complete methods only, no half-finished control structures)
3. Region structure followed (8 required regions in specific order)
4. Build compiles in Unity
5. Collision matrix consistency preserved (if touching collision code)
6. Infinity immutability rule not violated
7. Integration tests pass (for tasks scoring 5+)
8. Performance targets met (60 FPS, <3s load, <2GB memory)

### The Documentation Corpus

One of the most visible outputs of AI-assisted development is the documentation. A solo developer typically doesn't produce 25+ design documents, an MDA (Mechanics-Dynamics-Aesthetics) framework analysis, a visual architecture style bible, a sound architecture specification, a market analysis, milestone tracking with velocity metrics, and technical debt tracking. The AI made this level of documentation practical — and the documentation in turn made the AI more effective, because it could reference authoritative design documents instead of guessing at intent.

Key documents produced:

| Document | What It Is |
|----------|-----------|
| Game Design Document | Master reference for all gameplay systems and mechanics |
| Executive Summary | Project positioning, target audience, key features |
| Gameplay Mechanics | Detailed system documentation for all game systems |
| MDA Framework Analysis | Formal Mechanics-Dynamics-Aesthetics analysis of the game experience |
| Visual Architecture (Style Bible) | Dark = danger, light = safety aesthetic system |
| Sound Architecture (Style Bible) | "Cosmic jazz" audio philosophy specification |
| Market Analysis | Honest product-market fit assessment that led to stopping development |
| Roadmap | 4-phase development plan with point estimates for every milestone |
| Milestone Tracking | Completed work, velocity metrics, commit references |
| Technical Debt | File size violations, cleanup plans, architecture health scoring |
| Technical Critiques | Codebase analysis with specific improvement recommendations |

### What This Proves (and Doesn't)

**What it demonstrates:**
- A solo developer with AI assistance can build a 156-script Unity game with complex interlocking systems (16-type collision matrix, multi-segment grids, RPG attunements) in ~4.5 months of active work
- AI governance infrastructure (rules, approval gates, complexity scoring, skills with lessons learned) is essential — without constraints, the AI introduces bugs, violates architecture patterns, and creates inconsistency. The governance system was the project's most important meta-engineering decision.
- Context quality determines AI effectiveness — the documentation and rule infrastructure acted as a force multiplier. Every document produced made the AI better at its next task.
- 22 productive days per calendar month of active development is achievable for complex game systems, not just boilerplate
- AI can produce professional-grade documentation alongside code — 25+ design documents, MDA analysis, style bible, market analysis — at a level that traditional solo development rarely achieves
- The AI learned from its own failures across sessions through documented bug patterns and anti-patterns, creating a form of persistent institutional knowledge

**What it doesn't prove:**
- That the AI made correct high-level design decisions — the genre identity confusion existed from the start, and the AI followed the human's creative direction without challenging it
- That AI-assisted code is inherently defect-free — recurring bugs were common enough to warrant a "lessons learned" table, and 12 files exceeded size limits despite rules forbidding it
- That this is the most efficient workflow — it's one data point, not a benchmark

---

## Development History

### Timeline

**April 29, 2025 — Project Born**
First commit: `.gitattributes` and `.gitignore`. Within days, cubes were falling on a grid.

**May-June 2025 — The Explosion (191 commits)**
Peak development. May produced 106 commits (~25 points), June added 85 more. Core systems built: grid management, wave progression, player actions, four-tier markers, face painting, corruption tiles, debug infrastructure (F12 panel with 10+ sub-panels), save system, statistics tracking, and stage content. Two separate weeks hit 42 commits each — the project's all-time peak, characteristic of AI-assisted sprints where a single focused session produces dozens of meaningful changes.

**July 2025 — Wind-Down (15 commits)**
Audio system architecture, documentation, validation pipeline. The pace slowed as foundation work completed.

**July 15 - November 14, 2025 — The Hiatus**
Four months of silence — zero commits. The return proved how valuable the documentation infrastructure was: all project context was encoded in files the AI could read, not in the developer's memory. Two weeks after returning, velocity was back to 25 commits/week.

**November-December 2025 — Return & Refinement (62 commits)**
Collision matrix formally designed and implemented (all 16 combinations). Resonance system for Infinity-vs-Infinity interactions. Marker economy rules. Stages 0-5 built, playtested, and validated. AI governance infrastructure (rules, skills, approval gates) formalized.

**January 2026 — Refactor & Ship (18 commits)**
A deliberate refactoring sprint: WaveManager 3,324→1,737 lines, GridManager 2,885→~2,000, CubeManager -595 lines. Hub world implemented. Multi-segment grids (L/C/S shapes) working. Full gameplay loop complete: Splash > Hub > Stage > Hub.

**February 2026 — Conclusion**
Market analysis revealed structural challenges. Development concluded.

> For the complete git history analysis — weekly commit cadence, authorship breakdown, pace analysis, and the full narrative arc of 305 commits — see [Axiomatic Additions: The Journey](Assets/Docs/Project%20Doc/AxiomaticAdditions.md#the-journey-development-timeline--git-history).

### By the Numbers

| Metric | Value |
|--------|-------|
| Total commits | 305 |
| Calendar span | April 29, 2025 – February 6, 2026 (9.3 months) |
| Active development time | ~4.5 months |
| Hiatus | Mid-July to mid-November 2025 (4 months) |
| Total C# scripts | 156 |
| Documented points completed | 130+ (8 formally tracked milestones) |
| Measured velocity | 22 points/month (sustained across active periods) |
| Peak month | May 2025 (106 commits, ~25 points) |
| Peak week | 42 commits (achieved twice: May 4 and June 15) |
| Infrastructure built | ~12,000+ lines across 50+ files (see [catalog](Assets/Docs/Project%20Doc/AxiomaticAdditions.md#what-was-built-the-auxiliary-systems-catalog)) |
| AI governance files | 8 rules + 3 skills (always-on AI context) |
| Design/technical documents | 25+ |
| ScriptableObject data files | 50+ |

---

## Interesting Tidbits

> For a deeper dive into the auxiliary systems catalog (12 infrastructure systems totaling ~12,000+ lines), development accomplishments, weaknesses, and behind-the-scenes facts, see [Axiomatic Additions](Assets/Docs/Project%20Doc/AxiomaticAdditions.md).

### The Collision Matrix Is More Like a Language
Designing 16 unique collision outcomes forced every combination to feel purposeful. Unit vs Unit is a clean capture. Matrix vs Matrix escalates to a 3x3 area marker. Recursion doesn't capture anything — it creates swap markers that rearrange the board. And Infinity is completely immutable to everything except itself. The matrix became less of a lookup table and more of a vocabulary players learn over time.

### Infinity Cubes Escaping Is a Feature, Not a Bug
When an Infinity cube reaches the end of the grid and escapes, there's no penalty. Every other cube type penalizes you for escaping. But Infinity cubes are meant to be avoided, not captured (unless you have Infinity markers). This inverts the player's instinct — instead of "capture everything," the game teaches you "some things are meant to pass through."

### The 4-Month Hiatus Was Actually Productive
Not in terms of code, but in terms of perspective. Coming back after four months meant looking at the game with fresh eyes. The entire marker terminology got standardized (the original system used "Light/Heavy/Prime" names that didn't communicate anything). The collision matrix was formally designed on paper before a single line of code was written. Sometimes stepping away is the most productive thing you can do.

### The Hub Is Just Stage 100
Rather than creating a separate scene for the hub world, it was consolidated into the existing Stage scene system as "Stage 100" (named "Infinity's Axiom"). The GridManager, WaveManager, and all existing systems just... work, because the hub is technically a stage with no waves. This architectural shortcut eliminated an entire class of scene-transition bugs.

### 12 Files Were Too Big — And That's Documented
The project maintained strict file size limits (750 lines for core components, 600 for managers). At the time development stopped, 12 files exceeded these limits. Rather than hiding this debt, it was tracked in a dedicated TechnicalDebt.md with specific refactoring plans and architecture health scoring (6.5/10). The January 2026 refactoring pass was specifically tackling this — GridManager and WaveManager both got significant subsystem extractions.

### The AI Learned from Its Own Bugs
The skill file (`SKILL.md`) contains a "Recurring Bug Patterns" table documenting bugs the AI caused: NullReferenceExceptions from using `FindFirstObjectByType` in frequently-called methods, stage progression failures from event subscription timing, out-of-bounds errors from missing grid validation. Each bug was documented with cause and prevention, so the AI would read them at the start of every future session and avoid repeating them. This is machine learning in the most literal, low-tech sense — write down what went wrong, read it next time.

### The Cosmic Lo-Fi Aesthetic Inverts the Standard
Most puzzle games use white/clean minimalism. InfinityQube deliberately inverts this: dark means danger (Infinity cubes are black), light means safety. The "cosmic lo-fi" identity was documented in a full style bible. The audio vision was "cosmic jazz" — polyrhythmic complexity that simplifies to sparse solo performance as the player enters flow state. Neither the visual nor audio identity was fully realized in the game, but the design specifications exist as complete documents.

### The Market Analysis Killed the Project (And That's Okay)
The most valuable document produced might be the one that ended development. The market analysis was unflinching: "The game is wrapped in a product that doesn't clearly match what puzzle players *or* action players expect when they click 'Add to Cart.'" It identified the Symmetrical Wave System as genuinely novel but concluded that novelty inside an unclear product isn't enough. Knowing when to stop is its own form of intelligence — human or artificial.

---

## Technical Architecture

The codebase is organized around singleton managers communicating via a centralized event system (`GameEvents`):

```
StageManager → WaveManager → CubeManager (wave cubes)
     │              │
     │              ▼
     │        CubeCollisionManager (resolves all 16 combinations)
     │              ▲
     ▼              │
PlayerActionManager → PlayerMarkerSystem → CubeManager (player cubes)
```

**Key architectural decisions:**
- **Data-driven content** — All stage, wave, and attunement configurations live in Unity ScriptableObject assets (a Unity pattern for storing game data outside of code), not hardcoded values. This means game designers (or AI assistants) can tune gameplay without touching C#.
- **Component extraction pattern** — Large managers delegate to focused subsystem classes (e.g., `GridRowManager`, `WaveMovementSystem`, `CubeFacePaintingSystem`) rather than growing monolithically.
- **DOTween Pro for animations** — A Unity tweening library used for all visual animations (fades, movements, scaling). Coroutines reserved for async logic only.
- **Odin Inspector for editor tooling** — A Unity editor enhancement plugin used for debug section organization, inspector buttons for testing, and validation attributes on data fields.
- **Centralized collision resolution** — All cube interactions flow through `CubeCollisionManager`. Individual cubes never resolve their own collisions.
- **Event-driven communication** — Managers communicate via C# events and a static `GameEvents` class rather than direct references where possible.

---

## Project Structure

```
Assets/
├── data/                  # ScriptableObject assets (stages, waves, attunements, audio)
├── Docs/
│   ├── Project Doc/       # Game design, roadmap, milestones, market analysis, style bible
│   └── Technical Doc/     # Architecture, technical debt, testing reports
├── scripts/               # 156 C# files
│   ├── Managers/          # Core systems (Grid, Wave, Stage, Player, Collision)
│   │   ├── Grid/          # Grid subsystems (rows, batch operations)
│   │   ├── Wave/          # Wave subsystems (movement, spawning)
│   │   └── Cube/          # Cube subsystems (face painting, audio)
│   ├── Data/              # ScriptableObject class definitions
│   ├── Components/        # Tile system, camera, visual effects
│   ├── UI/                # Game UI, player UI, stage transitions
│   ├── Core/              # Singletons, analyzers, event systems
│   ├── Hub/               # Hub scene management
│   └── Testing/           # Integration and scenario testing
└── .cursor/
    ├── rules/             # 8 AI governance rule files (always-on context)
    ├── skills/            # 3 AI skill files (domain expertise + lessons learned)
    └── environment.json   # AI environment configuration
```

---

## Running the Project

1. Open in **Unity 6** (version 6000.2.14f1)
2. Requires **DOTween Pro** and **Odin Inspector** packages (paid Unity Asset Store plugins, not included in repo)
3. Open the Stage scene
4. Press Play — the splash screen leads to the hub, which leads to stage selection

---

*Development period: April 2025 — February 2026 (~4.5 months active across 9 calendar months)*  
*Solo developer with AI-assisted workflow (Cursor + Claude)*  
*305 commits | 156 scripts | 130+ development points | 8 AI rule files | 25+ design documents*  

**Key Documents:** [Axiomatic Additions](Assets/Docs/Project%20Doc/AxiomaticAdditions.md) (retrospective) | [Market Analysis](Assets/Docs/Project%20Doc/MarketAnalysis.md) (why development stopped) | [Milestone Tracking](Assets/Docs/Project%20Doc/MilestoneTracking.md) (velocity data)
