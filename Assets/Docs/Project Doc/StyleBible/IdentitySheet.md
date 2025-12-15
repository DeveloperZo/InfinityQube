# Core Concept Identity Sheets — Infinity Cube
> **Role:** Compact, pasteable identity sheets for core concepts (style bible layer, not implementation).  
> **Use:** Align look/feel/sound/meaning so content scales without semantic drift.  
> **Shared rule:** *Clarity first, intensity second.*  
> **Shared review pack:** gameplay-distance screenshot, grayscale screenshot, dense clip, sparse clip, divider transition clip, and (for audio) dense + sparse clips + A/B if signature changed.

---

## Sheet 1 — Unit Cube (Updated)

### Identity Anchor (1 sentence)
The meter: the calm, dependable baseline that makes all other interactions readable.

### Player Promise
“If I learn Unit interactions, I can always predict outcomes and time them under pressure.”

### Visual Grammar
- **Primary cue:** neutral gray/gray-blue, **mid-value dominant**, soft cosmic haze (atmosphere, not “content”).
- **Continuity target (across variants):** reads as the *same* Unit family at gameplay scale—stable silhouette, stable mid-value, stable restraint.
- **Detail policy:** micro-dust / low-frequency noise only; **no** readable nebula shapes, constellations, grids, cracks, loops, or “space art.”
- **Edge policy:** thin, crisp outline with controlled highlight; bloom can be tuned later, but Unit must remain the **least emissive** cube family once Matrix/Recursion/Infinity are finalized.
- **Stage tint rule (Option B):** Unit may receive the stage’s **neutral tint** only (low saturation, bloom-inert).
- **Forbidden palette overlap:** Unit tint must not drift into **Matrix (blue/cyan)**, **Recursion (purple/magenta)**, **Danger (red/warm)**, or **Infinity (near-black dominance)** families.
- **Ownership encoding:** player = translucent; wave = opaque.  
  - **Fallback cue:** subtle value-only rim/edge treatment for player-owned only (non-color channel).

### Audio Grammar
- **Timbre:** clean, short-decay impact; neutral “tempo bed.”
- **Variation policy:** subtle alternates (micro pitch/texture) without changing envelope or identity.

### Interaction Grammar (collision “verb” tendencies)
- **Baseline verb:** single capture language.
- **Unit → Infinity:** **sacrifice** (no painting); the reaction carries emphasis, Unit remains restrained.

### Reference Assets
- **Attract Look 1:** unit_cosmic_current
- **Attract Look 2:** unit_cosmic
- **Repel Look 1:**
- **Attract Feel 1:**
- **Attract Feel 2:**
- **Repel Feel 1:**
- **Attract Sound 1:**
- **Attract Sound 2:**
- **Repel Sound 1:**

### Do / Don’t
- **Do:** keep Unit the least “showy” and most legible; cosmic reads as mood, not spectacle.
- **Do:** prioritize grayscale readability (mid-value stability) over hue.
- **Don’t:** let Unit edges become the brightest element in mixed cube sets (unless nothing higher-order is present).
- **Don’t:** add glow/sparkle/long tails that compete with telegraphs/danger or higher-value types.

### Quality Gates (pass/fail)
- In dense waves, Unit remains recognizable without stealing focus.
- In grayscale, Unit stays distinct via **mid-value + smoothness** (not hue).
- At gameplay scale, interior reads as **haze/dust**, not stars or “space art.”
- In mixed sets, Unit remains the **least emissive** family (post-bloom balancing).

### Open Questions / Action Items
- Define neutral stage tint presets (3–5 named moods) and saturation/value bounds for Unit.
- Lock Unit’s capture/impact loudness as the **mix reference baseline** for all other cube interactions.
- Decide whether the edge highlight stays purely lighting-driven or gets a minimal authored outline (still bloom-inert).

---

## Sheet 2 — Matrix Cube

### Identity Anchor (1 sentence)
The bloom: strategic value expressed as controlled area coverage and deliberate player choice.

### Player Promise
“When Matrix shows up, I’m thinking about space, efficiency, and planned triggers.”

### Visual Grammar
- **Primary cue:** cool hue family with *subtle* emphasis (allowed: gentle glow/accent).
- **Shape/pattern fallback:** a distinct corner accent / micro-pattern that survives grayscale.
- **State tolerance:** overlays preserve “valuable/strategic” read; never confused with Unit.

### Audio Grammar
- **Timbre:** gently richer harmonic content than Unit, still restrained.
- **Mix rule:** Matrix never outranks danger/warnings; it’s valuable, not threatening.

### Interaction Grammar (collision “verb” tendencies)
- **Area Bloom:** square geometry language (2x2/3x3 family).
- **Choice/Trigger:** outcomes should feel *intentional* (player agency), not chaotic power.

### Do / Don’t
- **Do:** use square footprint cues (outlines, soft expanding frame) for area reactions.
- **Don’t:** turn Matrix into “UI glow candy” or a loud musical lead.

### Quality Gates (pass/fail)
- Matrix reads as “valuable/area” at gameplay distance.
- In grayscale, Matrix is not mistaken for Unit.

### Open Questions / Action Items
- Choose Matrix grayscale discriminator (default): **lighter value band + corner accent**.
- Decide maximum allowed emissive use for Matrix (guardrail, not numbers).

---

## Sheet 3 — Recursion Cube

### Identity Anchor (1 sentence)
The grind: durability and sequence—multi-hit payoff that rewards planning without noise.

### Player Promise
“Recursion feels substantial; I can read progress and commit to a plan.”

### Visual Grammar
- **Primary cue:** heavier/darker presence; communicates durability.
- **Durability cue:** damage expression must preserve identity (avoid “turning into Unit”).
- **Grayscale fallback:** durability uses **pattern/value shift** (not hue alone).

### Audio Grammar
- **Timbre:** weightier than Unit/Matrix; subtle progression across hits (same family, evolving).
- **Fatigue rule:** progression is audible but never becomes harsh or clicky.

### Interaction Grammar (collision “verb” tendencies)
- **Charges / Degradation:** communicates “work remaining” and “sequence.”
- **Patterned payoff:** reactions should feel stepwise and learnable (not explosive spectacle).

### Do / Don’t
- **Do:** express durability with restrained wear and consistent “heft.”
- **Don’t:** add randomization that makes outcomes feel inconsistent.

### Quality Gates (pass/fail)
- Player can tell “how damaged” Recursion is at gameplay distance.
- Under density, Recursion remains distinct from Matrix/Unit.

### Open Questions / Action Items
- Pick Recursion grayscale discriminator (default): **dark value band + faint wear grid**.
- Define the “hit progression” ladder in 3 steps (identity-preserving, not new sounds).

---

## Sheet 4 — Infinity Cube

### Identity Anchor (1 sentence)
The void made solid: a black-hole cube that only *reluctantly* reveals light, and only when the world forces it to.

### Player Promise
“If I recognize Infinity, I know I’m dealing with inevitability—what I do now is about preventing collapse, not styling outcomes.”

---

## Visual Grammar

### Core Read Hierarchy
- **Primary read:** near-black mass first (Infinity is the darkest family on screen).
- **Secondary read:** surface *sheen + micro-topography* (the “lazy liquid” / heavy skin).
- **Tertiary read:** sparse embedded specks (rare, dim; never a starfield wallpaper).

### Material / Surface Rules (based on your latest)
- **Material:** black granite/obsidian skin with subtle viscous ripple (seen in specular + shading, not geometry noise).
- **Specular:** controlled, tight highlights; **no emissive rim**. (Infinity should not win by brightness.)
- **Detail density:** low-frequency ripples > high-frequency glitter.
- **Embedded light:** *extremely sparse* micro-specks, muted white + very dim blue, appearing deep within (not sitting on the surface).

### Edge & Outline Policy
- **Default:** no “hero wireframe.”
- **Allowed:** a thin *non-emissive* edge catch (lighting-based) only if needed for readability on dark stages.
- **Forbidden:** bright outline glow or thick rim bloom (reads Matrix/hero, not Infinity).

### Tile-Glow Interaction (the thing your latest surface enables)
- Infinity should “take color” primarily through **reflections / sheen** from grid tiles:
  - tile emission causes subtle traveling color across ripples
  - color never fills the cube; it skims like reluctant evidence
- **Clamp rule:** even when lit by a bright tile, Infinity must remain darker than Unit/Matrix highlights.

---

## Evolution Read (what you gained, what to avoid)
- **Infinity POC (wireframe starfield):** clear “space,” but too readable/pretty; risks “generic star cube.”
- **Cosmic Infinity (white veining):** strong drama, but the **white wash becomes the hero** (and veining can read like cracks/energy).
- **Current (black material cube):** best Infinity direction—**mass + depth** with room for tile-driven color. Keep this.

---

## Interaction Grammar (collision “verb” tendencies)
- **Baseline verb:** *consume / compress / silence.*
- **Unit → Infinity:** *sacrifice* (no painting); visually: Unit’s haze collapses inward and disappears, leaving a brief **dark pulse** (dimming wave), not a flash.
- **Infinity landing:** a soft “pressure settle” (micro ripple + subtle specular shift) rather than sparks.

---

## Audio Grammar
- **Timbre:** low, short-decay “pressure thunk” + sub tail; minimal high end.
- **Variation:** slight pitch/weight variation by mass/state, never a bright transient.
- **Mix rule:** Infinity impact is heavier than Unit, but not louder than Danger telegraphs.

---

## Do / Don’t
- **Do:** make black itself the allure (depth, heaviness, inevitability).
- **Do:** let grid/tile light paint Infinity indirectly via sheen.
- **Don’t:** add star density, glitter, or nebula forms that read as decoration.
- **Don’t:** use white cracks/filaments as a default motif (reads “energy” not “void”).

---

## Quality Gates (pass/fail)
- **Downscale test:** at 64px, Infinity still reads as the darkest cube with a subtle sheen.
- **Mixed-family test:** next to Unit, Infinity feels heavier/inevitable, not just “darker Unit.”
- **Tile-glow test:** color appears as a reluctant skim, not a fill or aura.
- **No collision:** does not resemble Recursion (spirals/loops) or Matrix (cyan data glow).

---

## Open Questions / Action Items
- Decide if Infinity ever uses a **thin edge cue** (lighting-only) or stays silhouette-only.
- Set a numeric **“darkness floor”** (max albedo/value) so tile lighting can’t over-brighten it.
- Define “embedded specks” density target (e.g., “< 1% of pixels as bright points at 1k render”).


---

## Sheet 5 — Grid Tiles (Base + Semantic States)

### Identity Anchor (1 sentence)
The board: stable, readable, and semantically consistent—theme can change, meaning cannot.

### Player Promise
“I always know where I can act, what is dangerous, and what is primed—without guessing.”

### Visual Grammar
- **Base tile:** neutral, supportive, low-frequency texture (no shimmer in motion).
- **Semantic overlays:** reserved, consistent, and higher priority than theme variation.
- **State priority (semantic):**
  1) cannot act / blocked  
  2) primed / armed / will trigger  
  3) guidance / tutorial highlight  
  4) hover / cosmetic

### Audio Grammar (optional, restrained)
- Tiles generally shouldn’t “talk” unless they change rules.
- If tiles affect gameplay state, use subtle modulation cues that don’t mask cube language.

### Theme Variation Policy
- Allowed per level: base tile hue drift, roughness, background integration, border treatment.
- Not allowed: changing the meaning colors/grammar of blocked/primed/danger states.

### Do / Don’t
- **Do:** keep overlays legible at gameplay distance and in grayscale.
- **Don’t:** reuse cube identity hues for tile semantics.

### Quality Gates (pass/fail)
- In grayscale, blocked vs primed vs normal remains readable.
- In dense scenes, tile overlays do not compete with cube identity or divider danger.

### Open Questions / Action Items
- Confirm the canonical set of tile states and their meanings (1 page “tile glossary”).
- Decide whether tile base theme varies by **chapter mood** only or also subtle **difficulty tone** (non-semantic channel only).

---

## Sheet 6 — Background / World Space

### Identity Anchor (1 sentence)
The void: cosmic depth that supports focus and never competes with the grid.

### Player Promise
“I can plan calmly; the background enhances immersion without stealing attention.”

### Visual Grammar
- **Priority:** always below grid + cues.
- **Motion:** slow, low-frequency parallax only; avoid sparkly noise.
- **Contrast:** background never approaches cube/overlay contrast ranges.

### Audio Grammar
- Atmosphere as “bed,” not “lead.”
- If atmosphere reflects density, it does so subtly and yields to critical cues.

### Do / Don’t
- **Do:** use background changes for chapter mood and rest moments.
- **Don’t:** use background changes to convey moment-to-moment danger (that’s divider/telegraphs).

### Quality Gates (pass/fail)
- A worst-case dense wave remains readable with background enabled.
- No shimmer/crawl during step motion.

### Open Questions / Action Items
- Decide “chapter palette bands” (3–5) for background moods (names only, not codes).
- Decide if any background moment is allowed to “crescendo” (default: no).

---

## Sheet 7 — Divider (The Boundary)

### Identity Anchor (1 sentence)
The boundary: the calm constraint that becomes urgent only when it must.

### Player Promise
“I always understand safe vs danger and where I’m allowed to act.”

### Visual Grammar
- **Default state:** present but quiet.
- **Danger state:** immediate clarity; must not obscure cube identity.
- **Dominance cap:** danger escalates via **contrast/readability first**, not size or strobing.

### Audio Grammar
- Divider is mostly silent; danger escalation can add a restrained warning motif if needed.
- Warnings must not mask Infinity signature or escape cues.

### Do / Don’t
- **Do:** make safe→danger transition unmistakable but not fatiguing.
- **Don’t:** let divider become the most visually complex element.

### Quality Gates (pass/fail)
- Safe vs danger is recognized instantly at gameplay distance.
- Divider never causes cube types to be misread.

### Open Questions / Action Items
- Define maximum “divider dominance” rule (qualitative): e.g., “never brighter than telegraph peak.”
- Decide if divider gets an audio motif at all (default: only on danger transitions, very subtle).

---

## Sheet 8 — Collisions (Reaction Families / “Element Table” Feel)

### Identity Anchor (1 sentence)
Reactions: collisions resolve into a small set of learnable verbs with consistent visual+audio grammar.

### Player Promise
“I can predict outcomes by type pairing, and the game teaches me through consistent reaction language.”

### Reaction Families (standardize these names)
1. **Capture (Single)**
   - **Visual:** small confirm flash; minimal particles; fast resolution.
   - **Audio:** short confirm; never louder than landing baseline.
   - **Under load:** always preserved.

2. **Bloom (Area Capture)**
   - **Visual:** square outline expansion + gentle fill; footprint clarity beats spectacle.
   - **Audio:** soft swell; avoid “explosion.”
   - **Under load:** suppress secondary particles; preserve footprint outline.

3. **Charge / Degrade (Limited uses, shrinking areas, expiring markers)**
   - **Visual:** ticks/segments and shrink language; communicates “work remaining.”
   - **Audio:** thinning/reduction motif; never harsh.
   - **Under load:** preserve the “remaining” indicator; drop flourishes.

4. **Prime (Face Paint / Delayed Trigger)**
   - **Visual:** painted-face cue + deterministic telegraph progression.
   - **Audio:** subtle modulation that says “primed,” not “powered.”
   - **Under load:** telegraph wins; other effects defer.

5. **Resonance (Global Rule Shift / Phase Window)**
   - **Visual:** unmistakable world-state cue; calm but decisive.
   - **Audio:** singular signature cue; should be instantly recognized.
   - **Under load:** always preserved; suppress everything else if needed.

6. **Sacrifice / Conversion (Destroy + transform + continue/join behaviors)**
   - **Visual:** clear cause/effect (what was lost, what changed, what continues).
   - **Audio:** loss motif (restrained) + continuation cue (subtle).
   - **Under load:** preserve the “what changed” signal; drop decoration.

### Do / Don’t
- **Do:** map every collision outcome to exactly one reaction family.
- **Don’t:** invent a one-off VFX/SFX for a single pairing unless it becomes a new family.

### Quality Gates (pass/fail)
- In dense waves, reaction families are still distinguishable (especially Prime/Resonance).
- Reaction cues never overwrite cube identity; cube identity persists through reactions.

### Open Questions / Action Items
- Create a one-page “Collision Verb Glossary” that lists each verb and its cues (no pairings table here).
- Decide which verb(s) are allowed to “peak” visually/audio (default: Resonance only).

---

## Appendix — Minimal “Style Review Pack” Template (copy/paste)
- `VIS_Dense_GameplayDistance.png`
- `VIS_Dense_Grayscale.png`
- `VIS_Dense_10s.mp4`
- `VIS_Sparse_10s.mp4`
- `VIS_Divider_SafeToDanger_10s.mp4`
- `AUD_Dense_30s.wav`
- `AUD_Sparse_30s.wav`
- `AUD_WarningLead_10s.wav`
- `AUD_Signature_AB.wav` (only if signature changed)
- `NOTES.md` (which guardrails changed and why)

