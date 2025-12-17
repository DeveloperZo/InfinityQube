# BRAINSTORMING - Possible Interesting Mechanics

> A list of potential mechanics to explore

## Brainstorm Items


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

#### Core System Design
- **Movement Pattern**: 
  - Place marker → Next step converts to cube → Moves backward each step on wave rhythm
  - All movement synchronized to wave step cadence
  - Player cubes and wave cubes are two halves of the same pattern
  
- **Marker-Cube Equivalence**:
  - Moving cube = mobile version of its marker type
  - Light cube = Unit Marker capture behavior
  - Heavy cube = Recursion Marker (multi-hit for Recursion)
  - Matrix cube = Matrix marker (area effect potential)
  - Inherits all resource costs, cooldowns, and regeneration rules from marker system

#### Strategic Depth
- **Spatial-Temporal Trade-off**:
  - Far marker placed early = late collision
  - Close marker placed late = early collision
  - Players must calculate collision points, not just positions
  
- **Pattern Mirroring**:
  - Success requires duplicating wave patterns with markers
  - Focus shifts from reactive placement to predictive mirroring
  - Visible collision points provide strategic clarity
  
- **Boundary Line Significance**:
  - Acts as axis of symmetry for the infinity theme
  - Reinforces original IQ's boundary pressure
  - Clear demarcation between player and wave space

#### Thematic Integration
- **Infinity Symbol (∞) as Gameplay**:
  - Two loops meeting in the middle = wave and player cubes
  - Symmetrical mechanics reflect mathematical infinity
  - Boundary line where infinity folds on itself
  - Converting cubes to markers = finding gaps in infinite loops
  
- **Visual Coherence**:
  - Player literally creates mirror image of threats
  - Collision points are where infinite loops complete
  - Strategic mastery means perfect symmetrical play

#### Key Interactions
1. **Infinity Cube Bypass**: 
   - Unit cube → travels backward → converts to marker before Infinity row
   - Wave passes over marker → captures occur behind Infinity cubes
   - Only Unit cubes can convert mid-flight (design decision)
   
2. **Same-Type Collisions**:
   - Matrix player cube + Matrix wave cube = Matrix marker dropped at collision
   - Recursion player cube + Recursion wave cube = Recursion Marker dropped
   - Creates resource generation through successful interceptions
   
3. **Strategic Positioning**:
   - Multiple markers at different distances = staggered interceptions
   - Can create defensive walls or surgical strikes
   - Timing of placement matters as much as position

#### Simplified Design Decisions
- **Resource Management**: Uses existing marker charge/cooldown system
- **Collision Behavior**: Moving marker hitting cube = standard capture for that marker type
- **Player Cube Interactions**: Pass through each other (no collision between player cubes)
- **Conversion Rules**: Only Unit cubes can convert back to markers mid-flight
- **Triggering**: Marker automatically becomes cube on next wave step (no manual trigger needed)

#### Why This Works
- **Not a new system**: Extension of existing markers with movement
- **Thematically perfect**: Infinity symbol becomes core gameplay loop
- **Strategic clarity**: Can see your plan executing in real-time
- **Depth without complexity**: Simple rule (markers can move) creates emergent strategy
- **Solves IQ's limitation**: Adds offensive play to defensive game
- **Answers are in existing systems**: Most design questions resolve by following marker system logic

---

**Last Updated:** July 04, 2025  
**Purpose:** Capture potential mechanics for future consideration
**Next Steps:** Prototype highest rated mechanics