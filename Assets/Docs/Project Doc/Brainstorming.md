# BRAINSTORMING - Possible Interesting Mechanics

> A list of potential mechanics to explore

## Brainstorm Items


### Wave animation introduction
Develop animation for wave intro for both single segment and multi-segment grids
Ideally we can tease the infinity cubes resonance and when rebuilding a wave for the next segment we can have the cubes move into place and phase through infinity cubes. So infinity cubes reach bottom of grid in same orientation and instead of falling off. Cubes climb up the sides to move into configuration before starting (and phasing through infinity which should always be there) 

### Iterate on face painting 
- **Description**: Mine this mechanic for more fun and interesting concepts
- **Unique Value**: gameplay differentiation
- **Constraints**: 
- **Rating**: 6/10
- **Notes**: Expand face painting so painted tiles affect cubes that touch them, creating a unified painting theme. When a painted face touches a grid tile, the tile becomes painted and creates a static marker (preserving fixed positioning for strategic capture zones). Additionally, any cube touching the painted tile gets converted to the painted type (e.g., Unit cube → Matrix cube). Converted cubes are highlighted during a manual window where the player can trigger them to create area effects; after the window expires, the converted cube becomes permanent and must be collided with normally. This dual-purpose approach preserves strategic positioning control through static markers while adding mobile transformation that affects cubes behind Infinity. The painting theme flows naturally: collision paints face → face touches grid → tile becomes painted → cubes touching tile get affected. This could simplify the system by removing the need for separate cube marker manual triggering (R key), as painted tiles serve both as fixed capture zones and cube converters. Works for both wave cubes and player cubes touching painted tiles.

### Advance Grid type
- **Description**: Introduce grid movement paths with 90-degree turns (L, C, S shapes) where cubes maintain formation but movement direction rotates
- **Unique Value**: gameplay differentiation
- **Constraints**: 
- **Rating**: 8/10
- **Notes**: Grids define movement paths with 90-degree turns. Cubes maintain their formation (e.g., 5x5 array) but movement direction changes at corners. When a 5x5 formation reaches the bottom edge, it turns 90° and moves right, maintaining the same 5x5 arrangement. This creates direction-dependent puzzle difficulty: the same cube formation becomes easier or harder to intercept based on movement direction. Vertical movement (narrow intercept point) may be impossible to solve, but when cubes turn horizontal (wide intercept point), the same formation becomes trivial. Strategic timing: players wait for cubes to turn into favorable directions where marker placement is more effective. Path examples: L-shape (down → right), C-shape (down → right → up), S-shape (down → right → down → right).

### Refactor Recursion
- **Description**: Tweak recursion for more player agency and differentiation from matrix
- **Unique Value**: Lets player solve more problems
- **Constraints**: 
- **Rating**: 7/10
- **Status**: ✅ **RESOLVED - January 2026**
- **Implementation**: Recursion redesigned as repositioning tool via swap mechanics. Creates swap markers that reposition cubes using + pattern swaps (N↔S, W↔E). Player selects direction (horizontal/vertical) via arrow keys with hover-based visual preview. Manual trigger (R key) like other cube markers. Empowered swaps (Recursion+Recursion) provide independent swap and capture axis selection. Multi-hit system requires 2 hits to capture Recursion cubes. Clear differentiation from Matrix: Matrix = area capture, Recursion = repositioning tool.
- **Original Notes**: Change Recursion from auto-trigger to manual trigger (player controls activation timing). Make Recursion shape dynamic based on placement position: when placed on grid edges (top, bottom, left, right boundaries), creates a 3 row × 1 column vertical area (good for vertical wave threats). When placed on non-edges (interior tiles), creates a 1 row × 3 column horizontal area (good for horizontal wave threats). This dynamic shape adaptation gives Recursion more versatility than Matrix's fixed squares (2×2, 3×3), creating clear differentiation: Matrix = area squares, Recursion = adaptive lines. Manual trigger gives players strategic timing control, allowing them to wait for optimal cube positions before activating. Visual preview shows which shape will be created based on placement position.

### Hire youtube creator daafrikan (Yannick)
- **Description**: Hire youtube creator daafrikan (Yannick)
- **Unique Value**: daafrikan (Yannick)
- **Constraints**: 
- **Rating**: ??/10
- **Notes**: 

### Turn-Based element (enables tetris like blocks)
- **Description**: Allow the player to increment move forward manually
- **Unique Value**: Allow for contiguous set of markers to form tetris like blocks with their own properties (e.g. if  you mix unit, matrix, recursion, infinity)
- **Constraints**: Must be easily discoverable and intuitive after learning individual marker/cube properties
- **Rating**: ??/10
- **Notes**: Special stage/rule mechanic

### Visual Marker Mode Indicators
- **Description**: Screen accents or character model changes (like Dead Space) to show current marker mode
- **Unique Value**: Clear visual feedback without UI clutter, immersive indication
- **Constraints**: Must not obstruct gameplay view, needs to be instantly readable
- **Rating**: 7/10
- **Notes**: Could tie into character progression/customization

### Paint Blob Chaos
- **Description**: When infinity cubes escape, they create paint blobs at predictable locations
- **Unique Value**: Adds minimal controlled chaos, visual spectacle for escapes
- **Constraints**: Must remain predictable enough to not feel random
- **Rating**: 6/10
- **Notes**: Could scale with difficulty - more blobs in later stages

### Grid Hole Mechanic
- **Description**: Unit cubes smash through grid creating holes that must be filled by next cube. Infinity cubes create permanent corrupted tiles, others create temporary tiles
- **Unique Value**: Makes letting ANY cube escape meaningful, creates dynamic grid states
- **Constraints**: Could be visually confusing, needs clear hole vs tile indication
- **Rating**: 8/10
- **Notes**: High complexity but high reward for strategy depth

### Overlapping Marker Amplification
- **Description**: Overlapping marker placements amplify effects, first marker is base
- **Unique Value**: Rewards precise positioning, adds depth to marker placement
- **Constraints**: Could make game too easy if not balanced properly
- **Rating**: 5/10
- **Notes**: Might conflict with current distinct marker type system

### Strategic Corruption Prevention
- **Description**: Deliberately trigger painted Infinity cube faces now to prevent corrupted tiles next wave
- **Unique Value**: Transforms Infinity cubes from pure obstacles to tactical decisions, adds "defusing" mechanic
- **Constraints**: Requires players understand face painting system first
- **Rating**: 9/10
- **Notes**: Creates immediate vs delayed consequence decisions, uses existing systems cleverly

### Marker Overwrite/Replace System
- **Description**: Allow placing a marker over an existing marker to remove the old one
- **Unique Value**: More intuitive than toggle - visually shows where marker will be placed/removed, maintains single-action simplicity
- **Constraints**: Needs clear visual feedback to show what will happen
- **Rating**: 7/10
- **Notes**: Alternative to toggle system that feels more natural, could highlight existing markers when targeting

### Moving Cube Markers (Marker to Cube System)
- **Description**: Markers transform into cubes that move backward toward incoming waves, creating a symmetrical gameplay system where players mirror the wave pattern with inverse timing. Collisions between player cubes and wave cubes function as captures.
- **Unique Value**: Embodies the infinity symbol (∞) theme through actual gameplay symmetry. Transforms static defensive play into dynamic pattern mirroring. Creates visible, predictable collision points that clarify strategic planning.
- **Constraints**: Maintains step-based movement rhythm. Resource costs follow existing marker system. Requires clear boundary line definition.
- **Rating**: 9/10 (Strong thematic coherence + strategic depth)
-- **Outcome**: Accepted and implemented

### Type-Based Player Cube Movement Ranges
- **Description**: Player cubes have fixed travel distance that varies by cube type, creating different strategic profiles per marker type. Range could increase with game progression, requiring more captures to maintain effectiveness.
- **Unique Value**: Creates strategic differentiation between marker types, forces closer placement decisions, adds resource tension
- **Constraints**: Must be intuitive and clearly communicated to players. Need to balance ranges to maintain fun.
- **Rating**: 7/10 (Future consideration - keeping move-until-collision for now)
- **Notes**: 
  - **Current Decision**: Keeping "move until collision" for now (Option A) - clearer when marker converts to cube
  - **Future Option C**: Type-based ranges
    - Unit cubes: 10 tiles max travel
    - Matrix cubes: 8 tiles max travel
    - Recursion cubes: 6 tiles max travel
    - Infinity cubes: 12 tiles max travel
  - **Progression Mechanic**: As game progresses, could increase captures needed per cube type to maintain range
    - Early game: 1 capture = full range
    - Mid game: 2 captures = full range
    - Late game: 3 captures = full range
    - Creates progression incentive: capture more cubes to extend range
  - **Strategic Impact**: 
    - Forces closer marker placement (more tactical)
    - Creates resource tension (limited range = limited options)
    - Different marker types have different "reach" profiles
    - Progression system rewards skillful play (more captures = better range)
  - **Design Considerations**:
    - Need clear visual feedback for remaining travel distance
    - Range could be displayed on marker placement preview
    - Could show "range ring" around marker showing travel distance
    - Balance: Too short = frustrating, too long = no strategic difference

### Efficient Single-Dev Testing Strategy
- **Description**: Develop testing workflow that prevents combinatorial explosion as stages/waves multiply
- **Unique Value**: Saves development time, enables faster iteration, prevents burnout
- **Constraints**: Must provide confidence without full regression testing every change
- **Rating**: 10/10 (Critical workflow improvement)
- **Notes**: Current problem: Testing Stage 0 all waves → Stage 0+1 all waves → Stage 0-2 all waves becomes combinatorially intensive. Solutions to explore:
  - **Incremental Validation**: Only test new/changed content. Once Stage 0 validated, only test Stage 1. Once Stage 1 validated, only test Stage 2. Don't retest everything.
  - **Smoke Testing**: Quick critical path validation (can player place marker? Do cubes move? Do collisions work?) - 2-3 minutes per stage instead of full playthrough
  - **Test Prioritization**: High-risk areas (new mechanics, changed systems) get full testing. Low-risk areas (unchanged waves) get spot checks
  - **Automated Playtesting**: Record/playback system or AI-driven testing that can run overnight. Validate core mechanics automatically
  - **Test Isolation**: Test individual waves/systems independently. Don't require full stage completion to validate a single wave
  - **Regression Testing**: Only retest what changed. If penalty system unchanged, don't retest all penalty scenarios
  - **Test Suites**: Organized test scenarios (e.g., "Unit marker basics", "Infinity avoidance", "Penalty triggers") that can be run selectively
  - **Quick Validation Tools**: Fast feedback loops (prototyping panel, debug tools) that validate mechanics without full playthrough
  - **Test Documentation**: Track what's been validated and when. Only retest if underlying systems changed
  - **Milestone-Based Testing**: Test to milestone completion, then lock. Only retest locked content if critical bug found

---

**Last Updated:** January 27, 2026  
**Purpose:** Capture potential mechanics for future consideration
**Next Steps:** Prototype highest rated mechanics