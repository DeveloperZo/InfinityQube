# Visual Architecture — Infinity Cube
A meditative cosmic puzzle where colored light floats in active darkness. Cubes are the only light sources. The void is the enemy. Noise is failure.

## Critical Decisions (Locked)
These decisions cascade into every visual system. Non-negotiable.

| Decision | Lock |
|---|---|
| Danger Language | **Darkness = danger.** Light leaving = threat approaching. **No red palette. No bright warnings.** |
| Infinity Identity | **Infinity cubes ARE the danger palette.** Near-black, monochrome void. The enemy is nothingness. |
| Ownership | **Edge/rim = PRIMARY.** Opacity = SECONDARY (disabled for Infinity). |
| Phaseable | **Opacity = phaseable state** (not ownership). Applies to **cubes only**. |
| Mint | **Mint is reserved for ownership only.** It is NOT used for selection/hover/valid placement. |
| Selection / Placement Feedback | **Tile markers + motion/value only.** No hue changes; no mint. |
| Opacity Safety Rule | **Tiles never use alpha** to communicate danger. (Tile alpha stays 1.0; use albedo/roughness/edge fade/noise blends.) |
| White | **White is "void interaction" only:** (a) Infinity veins (identity) and (b) 2–3 frame consequence flashes. **No other cube uses #FFFFFF.** |
| Cyan | No longer reserved. Matrix moved to violet. Cyan available for grid/ambient **only** (not selection, not warnings). |
| Matrix Hue | Violet/purple family. Strategic, rare, cosmic. |
| Recursion Hue | Pure warm amber/gold. **No magenta. No purple undertone.** |
| Recursion Damage | **Subtractive simplification. No additive noise.** |
| Stardust | **Gameplay-critical and must survive dense scenes.** Can scale down into "canary mode" but never fully disabled. |

---

## 1. North Star
A meditative cosmic puzzle where colored light floats in active darkness. Cubes are the only light sources. The void is the enemy. Noise is failure.

Core contract: **Color = life. Cubes = light. Void = enemy.**  
When the player captures cubes, they collect light. When Infinity cubes appear, light disappears.

---

## 2. Core Visual Pillars
**Pillar 1: Readability First**  
Type > State > Ownership > Decoration. If you cannot identify a cube at gameplay distance, the game has failed.

**Pillar 2: Semantic Restraint**  
One channel, one meaning. Hue = type. Darkness = danger. Edge = ownership. Opacity = phaseable. Motion = timing.

**Pillar 3: Cosmic Minimalism**  
Clean forms. Decoration never competes with meaning. Cosmic reads as depth and space, not glitter.

**Pillar 4: Cause-and-Effect Visibility**  
Players can point to what happened and why. No hidden state. No ambiguous feedback.

**Pillar 5: Noir Stardust as Gameplay Indicator**  
Stardust is not decorative. It is the canary in the coal mine.  
- Stardust presence = safety  
- Stardust death = danger proximity  
- Stardust pull = threat direction  
This is gameplay-critical ambient telemetry.

---

## 3. Color System
Cubes are the light sources of this world. Each type owns its hue band exclusively. The world around them is darkness of varying depth.

### 3.1 Semantic Channel Allocation (Single Authority)
A feature requiring a channel to carry two meanings must be redesigned.

| Channel | Meaning | Values | Conflicts / Rules |
|---|---|---|---|
| Hue (on cubes) | Cube type identity | Gray=Unit, Violet=Matrix, Amber=Recursion, Achromatic=Infinity | **No selection/hover/placement uses hue.** No system borrows type hues. |
| Darkness / Light | Danger level | Lit=safe, Unlit=danger (quantified in Section 4) | Inverted from typical bright=danger. Team must internalize. |
| Edge / Rim (on cubes) | Ownership | Player = mint rim/brackets; Wave = none (or wave-specific non-hue cue if needed) | Must survive min zoom. Must not compete with silhouette/type. |
| Opacity (on cubes only) | Phaseable state | Solid=opaque; Phaseable=40–50% | **Not used for ownership.** **Infinity never goes translucent.** |
| Tile Markers (under-cube) | Selection/hover/valid placement | Shape + value + pulse (no hue) | Must not be confused with danger (darkness). |
| Motion / Pulse | Timing + threat direction | Pulse rate=urgency; Stardust pull=direction | Idle ambient must be distinct from telegraph motion. |
| Pattern / Texture | State (paint, damage) | Paint = face pattern; Damage = subtraction | Pattern frequency must survive motion without shimmer. |
| White (#FFFFFF) | Void interaction | (a) Infinity veins (identity), (b) consequence flash (2–3 frames) | **No other cube uses #FFFFFF.** Never sustained. Never decorative. |

---

### 3.2 Infinity Cube — Void / Danger Incarnate
Pure monochrome. Near-black core with white lightning veins. Infinity IS the danger palette.

<table>
<tr><th width="80">Swatch</th><th>Name</th><th>Hex</th><th>Role</th></tr>
<tr><td style="background:#0A0A0A; border:1px solid #444;">&nbsp;</td><td>Near Black</td><td><code>#0A0A0A</code></td><td>Core and surface</td></tr>
<tr><td style="background:#FFFFFF; border:1px solid #ccc;">&nbsp;</td><td>Pure White</td><td><code>#FFFFFF</code></td><td>Veins and specks(identity + void interaction)</td></tr>
<tr><td style="background:#0D0D1A; border:1px solid #444;">&nbsp;</td><td>Deep Space</td><td><code>#0D0D1A</code></td><td>Placed around pure white for softer contrast</td></tr>
</table>

**Property Specification**
- Surface: matte. Absorbs light; returns nothing. No specular.
- Veins: #FFFFFF at **low intensity** (see Section 5) and 0.3–0.5 opacity. Thin, sharp, **not glow**.
- Edge behavior: minimal contrast with background. Infinity dissolves into void.
- Min-zoom survival: veins must be visible at 24px. **1px minimum vein width.**

---

### 3.3 Unit Cube — Neutral Foundation
Steel gray. Calm, readable, default cube.

<table>
<tr><th width="80">Swatch</th><th>Name</th><th>Hex</th><th>Role</th></tr>
<tr><td style="background:#4A5568;">&nbsp;</td><td>Steel Gray</td><td><code>#4A5568</code></td><td>Base</td></tr>
<tr><td style="background:#8B9CAD;">&nbsp;</td><td>Light Steel</td><td><code>#8B9CAD</code></td><td>Highlight</td></tr>
<tr><td style="background:#2D3748;">&nbsp;</td><td>Deep Gray</td><td><code>#2D3748</code></td><td>Shadow</td></tr>
<tr><td style="background:#0D0D1A; border:1px solid #444;">&nbsp;</td><td>Deep Space</td><td><code>#0D0D1A</code></td><td>Background</td></tr>
</table>

**Rule:** Unit never uses #FFFFFF as a sustained edge glow; highlights stay in gray/steel family.

---

### 3.4 Matrix Cube — Strategic Value (Violet)
Vivid violet nebula with star clusters. Occupies violet band exclusively.

<table>
<tr><th width="80">Swatch</th><th>Name</th><th>Hex</th><th>Role</th></tr>
<tr><td style="background:#8B5CF6;">&nbsp;</td><td>Vivid Violet</td><td><code>#8B5CF6</code></td><td>Primary</td></tr>
<tr><td style="background:#A78BFA;">&nbsp;</td><td>Soft Lavender</td><td><code>#A78BFA</code></td><td>Secondary</td></tr>
<tr><td style="background:#6D28D9;">&nbsp;</td><td>Deep Purple</td><td><code>#6D28D9</code></td><td>Deep</td></tr>
<tr><td style="background:#EDE7FF;">&nbsp;</td><td>Stars (NOT #FFFFFF)</td><td><code>#EDE7FF</code></td><td>Star highlights (off-white violet)</td></tr>
<tr><td style="background:#1A0D2E; border:1px solid #444;">&nbsp;</td><td>Violet Space</td><td><code>#1A0D2E</code></td><td>Background reference for texture only</td></tr>
</table>

**Rule:** Matrix "stars" are **off-white** (tinted) to preserve #FFFFFF rarity.

---

### 3.5 Recursion Cube — Dense Energy (Pure Warm)
Amber/gold energy network. Pure warm palette; no purple/magenta undertone.

<table>
<tr><th width="80">Swatch</th><th>Name</th><th>Hex</th><th>Role</th></tr>
<tr><td style="background:#FFAA00;">&nbsp;</td><td>Amber Gold</td><td><code>#FFAA00</code></td><td>Primary</td></tr>
<tr><td style="background:#FF6B00;">&nbsp;</td><td>Deep Orange</td><td><code>#FF6B00</code></td><td>Hot</td></tr>
<tr><td style="background:#FFD54F;">&nbsp;</td><td>Light Gold</td><td><code>#FFD54F</code></td><td>Bright</td></tr>
<tr><td style="background:#D4A056;">&nbsp;</td><td>Warm Bronze</td><td><code>#D4A056</code></td><td>Undertone</td></tr>
<tr><td style="background:#2D1B0D; border:1px solid #444;">&nbsp;</td><td>Deep Umber</td><td><code>#2D1B0D</code></td><td>Dark base</td></tr>
</table>

**Rule:** Recursion never uses #FFFFFF; "hotter" damage tiers use warm cream/gold (defined in Section 9.1).

---

### 3.6 Grid Tile — Stage Foundation
Dark blue-gray platform. The stage is lit only insofar as it **receives** light; danger removes that capacity.

<table>
<tr><th width="80">Swatch</th><th>Name</th><th>Hex</th><th>Role</th></tr>
<tr><td style="background:#1A2030; border:1px solid #444;">&nbsp;</td><td>Night Blue</td><td><code>#1A2030</code></td><td>Base</td></tr>
<tr><td style="background:#2A3545; border:1px solid #444;">&nbsp;</td><td>Slate Blue</td><td><code>#2A3545</code></td><td>Surface</td></tr>
<tr><td style="background:#4A5A6A;">&nbsp;</td><td>Steel Blue</td><td><code>#4A5A6A</code></td><td>Highlight</td></tr>
<tr><td style="background:#4A6A7A;">&nbsp;</td><td>Steel Cyan</td><td><code>#4A6A7A</code></td><td>Edge reference (keep desaturated; never "neon")</td></tr>
<tr><td style="background:#0D0D1A; border:1px solid #444;">&nbsp;</td><td>Deep Space</td><td><code>#0D0D1A</code></td><td>Global background</td></tr>
</table>

**Important:** Any "edge glow" is treated as *reflectance/grade* tied to the safety state (not a separate warning palette).

---

### 3.7 Guidance Palette (Updated for Channel Purity)
No danger palette exists. Danger is communicated through darkness (Section 4). Guidance uses **shape + value + motion**, not hue.

<table>
<tr><th width="80">Swatch</th><th>Element</th><th>Hex</th><th>Use</th></tr>
<tr><td style="background:#69F0AE;">&nbsp;</td><td>Mint</td><td><code>#69F0AE</code></td><td><b>Ownership rim/brackets ONLY</b> (not selection/hover)</td></tr>
<tr><td style="background:#FFFF8D;">&nbsp;</td><td>Soft Yellow</td><td><code>#FFFF8D</code></td><td>Tutorial hints (UI-layer only)</td></tr>
<tr><td style="background:#FFFFFF; border:1px solid #ccc;">&nbsp;</td><td>White</td><td><code>#FFFFFF</code></td><td>Consequence flash only (2–3 frames)</td></tr>
</table>

| Element | Allowed | Forbidden |
|---|---|---|
| Ownership | **Mint rim/brackets only** (#69F0AE), constant width | Mint for selection/hover/valid placement |
| Selection/Hover | Tile marker geometry + pulse + value shift (neutral) | Hue shifts, mint, violet, amber |
| Valid placement | Tile marker "open ring" + steady pulse | Colored halos |
| Invalid placement | Tile marker "broken ring" (gapped) + no pulse | Red/orange warnings |
| Tutorial | Soft yellow (#FFFF8D), UI-layer only | Using yellow for gameplay states |

---

## 4. Darkness-as-Danger System
Danger is not a color. Danger is light leaving. Quantified, implementable, and consistent.

### 4.1 The Five Properties of Black (Tiles)
**Tile alpha stays 1.0 at all times.** (Opacity channel is reserved for phaseable cubes.)

| Property | Safe State | Danger State | Implementation |
|---|---|---|---|
| Temperature | Warm black (#1A2030) | Cool dead black (#0A0A14) | Shader lerp tile base color by danger float (0–1). |
| Reflectivity | Subtle specular. Roughness 0.7 | Matte. Roughness 1.0 | Lerp roughness 0.7 → 1.0 by danger float. Metalness 0. |
| Edge Definition | Clear edges; edge reference visible | Edges dissolve; edge fades to 0 | Edge intensity lerp 1.0 → 0.0 by danger float. |
| Depth | Floor feels solid | Floor feels "dead/flat"; void impression rises | Blend void-noise **into albedo/normal** (no alpha) by danger float. |
| Stardust | Gentle drift, full density | Directional pull, density drops, speed increases | Particle system driven by danger + threat vector (Section 4.1 + Section 12). |

### 4.2 Danger Proximity Float
Single float per tile: 0.0 safe → 1.0 kill zone. Drives all black properties.

(Stages unchanged; tile opacity language removed. Depth is now albedo/normal blend only.)

---

## 5. Emissive Intensity Bands (Updated for White Purity)
Bright is life/identity; dark is threat. White is reserved for void interaction only.

| Band | Min | Max | Usage |
|---|---:|---:|---|
| Void (danger) | 0.00 | 0.05 | Kill zone. Background void. Infinity core. |
| Shadow (threat) | 0.05 | 0.15 | Imminent danger tiles. Cubes in danger zones (type glow suppressed). |
| Ambient (safe base) | 0.15 | 0.30 | Safe tile reflectance/grade; background depth. |
| Type Identity | 0.30 | 0.55 | Cube type glow (violet/amber/gray). **Infinity veins live here by default.** |
| State Feedback | 0.55 | 0.75 | Selection/hover/placement markers (neutral), paint feedback. |
| Consequence (white) | 0.75 | 1.00 | #FFFFFF flash for capture/kill (2–3 frames). Optional Infinity "impact" only if it is a consequence moment. |

Rules:
- **Infinity veins default to Type Identity band (≤ 0.55).** They only enter Consequence band if a specific void-impact event occurs (2–3 frames).
- Under dense wave (33+ cubes), type emissive clamps down to 0.40 to preserve separation from feedback.

---

## 8. Ownership Encoding (Unchanged intent; clarified scope)
Edge language is PRIMARY. Opacity is SECONDARY and disabled for Infinity.

| Owner | Primary Cue | Secondary Cue | Infinity |
|---|---|---|---|
| Player | Mint rim/brackets (#69F0AE), 2–3px @ 1080p | Optional 70% opacity (non-Infinity only) | Rim only. No opacity change. |
| Wave | No mint rim; solid edge | Opaque 100% | Solid edge only. |

Note: Selection/hover never uses mint. Selection feedback is tile-marker based (Section 3.7 / Section 9).

---

## 9. State Overlays (Updated: selection moved off mint; Recursion white removed)
| State | Primary Cue | Fallback | Type Safe? |
|---|---|---|---|
| Selected | **Tile marker** under cube: neutral "open ring" + pulse in State Feedback band | Slight value lift on cube (no hue shift) | Yes |
| Hover | Tile marker: neutral "thin ring" no pulse | None | Yes |
| Valid placement | Tile marker: open ring + steady low pulse | None | Yes |
| Invalid placement | Tile marker: broken ring (gapped), no pulse | None | Yes |
| Painted Face | Face pattern distinct | Pattern/contrast | Yes |
| Phaseable | Opacity 40–50% + shimmer | Dashed edge treatment | Yes |
| Damage (Recursion) | Subtractive: network darkens, nodes removed | 3 locked tiers | Yes |
| Telegraph | Clarity escalation: dim→bright, small→large | Pulse rate increases | Yes |

### 9.1 Recursion Damage Tiers (Updated: "hotter" stays warm, never pure white)
Add a dedicated warm "hot" color (non-white):
- **Hot Cream (warm, non-white): #FFF2C6** (Recursion-only; still warm; not #FFFFFF)

<table>
<tr><th width="80">Swatch</th><th>Name</th><th>Hex</th><th>Use</th></tr>
<tr><td style="background:#FFF2C6;">&nbsp;</td><td>Hot Cream</td><td><code>#FFF2C6</code></td><td>Critical (1 HP) Recursion glow</td></tr>
</table>

| Tier | Visual | Implementation |
|---|---|---|
| Full (3 HP) | All nodes lit; full amber glow; complete network | Damage mask 0%. |
| Damaged (2 HP) | ~30% nodes dark; connections dim; glow reduced | Mask 30%. Masked nodes go to #2D1B0D. |
| Critical (1 HP) | Most nodes dark. Remaining trunks read **simpler and hotter** (warm cream) to preserve identity at min zoom | Mask 80%. Remaining nodes shift #FFD54F → **#FFF2C6**; emissive capped at **0.55** (Type Identity band). |

Rule: Critical tier simplifies pattern (fewer thicker trunks) rather than increasing detail.

---

## 12. VFX Hierarchy (Updated: stardust survives dense scenes)
Priority order (highest → lowest):
1) Danger/darkness — divider state, tile darkening  
2) Type identity — cube hue, glow  
3) State overlays — paint, phaseable, damage  
4) Ownership — mint rim  
5) Guidance — tile markers (selection/placement)  
6) Ambience — stardust, depth

### Density-Based Reduction (Updated)
Stardust is gameplay-critical. It transitions from "ambient" to "canary mode" but never fully turns off.

| Cube Count | Effects |
|---:|---|
| 1–16 | Full effects. Full stardust ambient + directional telemetry. |
| 17–32 | Stardust density 70%. Depth/noise blend 70%. |
| 33–48 | **Stardust Canary Mode:** density 15–25%, **directional-only**, no random drift; speed 2x. Depth/noise blend 50%. Type emissive clamps to 0.40. |
| 49–64 | **Minimal Canary Mode:** density 5–10%, directional streaks only (short lifetimes), speed 3x. Depth/noise blend 25%. Type emissive clamps to 0.40. Guidance tile markers remain. Phaseable preserved. |

Canary Mode Implementation:
- Fewer particles, shorter lifetime, strictly directional velocity toward threat vector.
- No sparkle variation, no hue variation, no secondary noise.

---

## 13. Numeric Locks (Relevant updates only)
- Min tile size at min zoom: **24×24px**
- Min rim width: **2px @ 1080p**
- Infinity vein min width: **1px @ 24×24px**
- Tiles: **alpha always 1.0**
- Stardust: never disabled; Canary Mode density floors: **≥ 5%**

---

## 14. Quality Gates (Updated to reflect stardust + channel purity)
Add two explicit checks:

| Check | Pass | Fail |
|---|---|---|
| Channel Purity | In a dense scene, **mint indicates ownership only**; selection/placement readable without mint | Mint used for selection/valid placement or any hue-based guidance appears |
| Stardust Telemetry in Dense Mode | At 49+ cubes, player can still infer threat direction from stardust pull within 2 seconds | Stardust absent or too subtle to read in dense conditions |

---

## 17. Semantic Channel Registry (Updated items)
- **Mint (#69F0AE): Ownership only**
- **Selection/hover/placement: Tile markers + value + pulse only** (no hue)
- **Tiles: never use alpha**
- **#FFFFFF: Void interaction only** (Infinity veins + 2–3 frame consequence flashes)
- **Recursion "hot" at 1HP uses #FFF2C6, never #FFFFFF**
- **Stardust: always present; scales into Canary Mode under density**
