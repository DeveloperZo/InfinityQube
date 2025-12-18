# Visual Architecture — Infinity Cube (Style Bible)

> **Role:** Style bible for visuals (not an implementation guide).  
> **Purpose:** Define the aesthetic intent and guardrails that keep the game coherent, readable, and “lo-fi repeatable” as content scales.  
> **Companion Doc:** Sound Architecture (Style Bible).  
> **Audience:** You, art director, contractors/freelancers, tech art.

---

## 0. Document Control
- **Owner:**
- **Reviewers:**
- **Version / Date:** December 14, 2025
- **Change Log:** Refined to emphasize style guardrails and move implementation specifics to appendices.
- **Scope:** Visual identity + readability + feedback language + collaboration gates
- **Out of Scope:** Shader graphs, exact Unity settings, import presets, file structures, and per-asset numeric tuning (unless it protects meaning/readability).

---

## 1. North Star
### 1.1 One-line fantasy
Clean, minimalist cosmic puzzle grid where cube types are instantly recognizable, state changes are clearly communicated, and visual feedback supports strategic decision-making without visual noise.

### 1.2 Pillars (3–5)
- **Type Clarity:** Each cube type has an instantly recognizable identity at gameplay distance.
- **State Readability:** Overlays communicate cube/tile state without hiding type identity.
- **Strategic Feedback:** Visual cues (divider, telegraphs, highlights) support planning and timing.
- **Cosmic Minimalism:** Clean forms with subtle depth; decoration never competes with meaning.
- **Cause-and-Effect Visibility:** Players can point to what happened and why.

### 1.3 Anti-goals (explicit “no” list)
- Never obscure cube type identity with overlays or effects.
- Never use high-frequency patterns that shimmer/crawl during motion.
- Never create ambiguity between player-owned and wave-owned cubes.
- Never stack multiple high-intensity effects simultaneously.

---

## 2. Readability First (Non-Negotiables)
### 2.1 Visual priority order (what must win)
1. Threat / telegraph / divider danger
2. Cube type identity
3. Cube state overlays (painted/phaseable/damage/etc.)
4. Ownership (player vs wave)
5. Decorative cosmic detail / background

### 2.2 Readability guardrails
- Critical information must be readable at gameplay distance and during motion.
- Decorative texture/detail must never compete with overlays or danger indicators.
- A grayscale pass must preserve: cube type, danger state, and state overlays.

### 2.3 Clarity principles
- **Visible causality:** the player can infer cause-and-effect from visuals alone.
- **No overloaded signals:** a single visual channel should not convey multiple meanings.
- **Known failure cases:** tracked in Appendix C; visual changes should reduce these over time.

---

## 3. Visual Language (What Means What)
### 3.1 Core entities
- **Cubes:** four types, ownership (player vs wave), state overlays (face paint, phaseable, damage).
- **Tiles:** base state + state overlays (marked, blocked, special targets).
- **Overlays:** telegraphs, highlights, selection/placement feedback.
- **UI:** minimal, peripheral, grid-respecting, mode indicators.

### 3.2 Semantic channels (meaning budget)
To prevent visual overload, meanings are assigned to specific channels:
- **Hue (color family):** cube type identity (primary).
- **Brightness / contrast:** danger and urgency (divider, telegraphs).
- **Opacity / translucency:** ownership (player vs wave).
- **Shape / pattern:** state overlays and accessibility fallbacks.
- **Motion:** timing/telegraph progression (used sparingly).

Rule: **One channel ≈ one meaning**. Adding new meanings must reuse existing channels carefully or add a new channel explicitly.

### 3.3 Cube type identity (high-level signatures)
Each cube type must remain recognizable with overlays active and in dense scenes.

**Unit**
- **Signature:** gray-ish, neutral, foundational "default" presence.
- **Do:** simple surface, calm readability, minimal special effects, neutral gray appearance.
- **Don't:** add strong glow or high-detail texture that competes with overlays.

**Matrix**
- **Signature:** cool, “valuable/strategic” presence with gentle emphasis.
- **Do:** slightly elevated visual importance (subtle glow/accent allowed).
- **Don’t:** blend with background or look similar to Unit in grayscale.

**Recursion**
- **Signature:** darker/heavier feel; communicates durability/multi-hit.
- **Do:** allow wear/damage expression while preserving identity.
- **Don’t:** let damage visuals turn it into a “Unit-like” neutral.

**Infinity**
- **Signature:** ominous, uncapturable obstacle; unmistakably different.
- **Do:** restrained but high-contrast silhouette; phaseable state is clearly distinct.
- **Don’t:** make it look capturable or “rewarding.”

### 3.4 Ownership distinction (player vs wave)
- **Primary strategy:** ownership is expressed through **opacity** (player = translucent, wave = opaque).
- **Fallback strategy:** if translucency becomes ambiguous in a scenario, ownership must also be readable via **one additional non-color cue** (e.g., edge treatment, subtle rim, or trail language) without changing cube type identity.
- **Preservation rule:** cube type identity (hue family) must remain stable across ownership.

### 3.5 State overlays (paint/phaseable/damage/etc.)
All state overlays must preserve type identity and remain readable in dense scenes.

**Face painting**
- **Primary cue:** painted face is clearly distinguishable from unpainted faces.
- **Telegraph grammar:** as the trigger approaches, telegraph increases **clarity first** (dim→bright, small→large), then intensity (pulse rate).
- **Fallback cue:** remains readable without relying on hue alone.

**Phaseable**
- **Primary cue:** distinct visual state change that cannot be confused with “damaged” or “painted.”
- **Fallback cue:** visible via value/contrast or pattern, not hue alone.

**Damage / durability (Recursion)**
- **Primary cue:** communicates remaining durability without erasing identity.
- **Fallback cue:** pattern/value shift (not just color shift) so it works in grayscale.

---

## 4. Palette & Contrast (Strategy, Not Codes)
### 4.1 Palette roles
- **Type palette:** reserved for cube type identity.
- **Danger palette:** reserved for divider danger and urgent warnings.
- **Safe/affordance palette:** reserved for “okay/target/highlight” cues.
- **Neutral palette:** grid, UI frames, background.

### 4.2 Color meaning rules
- Type colors do not change to communicate danger or ownership.
- Danger uses contrast/brightness and the divider language first; it should not “repaint the world.”
- Highlights are guidance tools; they must not compete with danger or type identity.

### 4.3 Contrast and restraint
- Background stays supportive; contrast and motion are limited.
- Avoid high-frequency sparkle on common objects; reserve detail for meaning.

---

## 5. Materials & Texture Style (Guardrails)
### 5.1 Material character rules
- Clean forms + subtle depth over noisy surfaces.
- Emissive is reserved for meaning (telegraph/state), not decoration.
- “Cosmic” should read as depth/space, not as glitter/noise.

### 5.2 Texture frequency rules
- Avoid patterns that shimmer/crawl in motion.
- If a texture draws attention during step movement, it is too loud.
- Detail density increases only for rare/special moments or to improve legibility.

### 5.3 “When to add detail” policy
Detail is allowed only when it:
- improves legibility at gameplay distance, or
- increases comprehension of state/urgency, or
- supports the meditative atmosphere without competing with meaning.

---

## 6. Motion & Camera (Feel Constraints)
### 6.1 Motion philosophy
- Step-based motion should feel deliberate and calming.
- Micro-motion (pulse/drift) must never distract from planning.

### 6.2 Camera rules
- Keep the grid readable and stable.
- Avoid aggressive camera movement; allow only subtle response.

### 6.3 Transitions
- State changes should be readable, not flashy.
- “Silence moments” exist visually: sometimes minimal feedback is correct.

---

## 7. VFX & Telegraph Grammar (Minimal but Informative)
### 7.1 Feedback hierarchy
- Telegraphs and divider state are always more legible than capture/collision effects.
- Highlights are guidance; they are subtle and never dominant.
- Capture/collision effects are short, controlled, and purposeful.

### 7.2 Escalation rules
- Warnings escalate by **clarity first**, intensity second.
- Avoid stacking high-intensity effects; sequence effects when possible.
- In dense waves, reduce secondary effects rather than increasing everything.

### 7.3 “No Visual Noise” pledge
- **Too busy** means: cube types are unreadable, tile states are ambiguous, or safe/danger is unclear.
- **Cut priority:** decorative effects → secondary overlays → non-critical highlights. Preserve: danger, type identity, primary state overlays.

---

## 8. UI Style (Support, Not Center Stage)
### 8.1 UI principles
- Minimal, calm, aligned to grid logic.
- Information density is intentionally limited.
- UI does not steal attention from the grid.

### 8.2 Typography and layout (high-level)
- Simple, geometric, readable.
- Motion is restrained and consistent.

---

## 9. Quality Gates (Style Regression)
### 9.1 Canonical scenarios (must be used for review)
- Worst-case density wave (all cube types present)
- Mixed overlays active (paint + phaseable + durability cues)
- Divider visible in both safe and danger states
- Fast play / repeated actions (rapid placements, multiple captures)
- Player-owned vs wave-owned cube collisions

### 9.2 Required review artifacts (for any notable visual change)
Provide:
1. Gameplay-distance screenshot (worst-case density)
2. Grayscale screenshot (same frame)
3. 10-second clip (worst-case density movement)
4. 10-second clip (sparse/solo moment)
5. 10-second clip showing divider transition (safe→danger)

### 9.3 Pass/fail checks
- Cube types remain recognizable at gameplay distance (including dense scenes).
- Ownership remains unambiguous (primary + fallback where needed).
- Danger state is immediately recognizable and does not obscure type identity.
- No shimmer/crawl during motion.
- “Lo-fi repeatability”: not visually fatiguing over time.

---

## 10. Collaboration Notes (Freelancers / Art Director)
- **Requires Approval:** changes to cube type identity, divider language, core overlay grammar, ownership encoding strategy.
- **Open for Experimentation:** background depth treatments, subtle material variation, non-critical polish effects, highlight pacing.
- **Submission Process:** include the required review artifacts from Section 9.2 and note which guardrails the change touches.

---

## Appendix A — Current Implementation Notes (Reference Only)
> This appendix captures what is implemented today. It is not the source of truth for the style; the sections above are.

### A.1 Cube type palette (implemented)
- Unit: gray/neutral family  
- Matrix: cool/blue family  
- Recursion: purple family (darker/heavier)  
- Infinity: near-black family (ominous)

### A.2 Ownership encoding (implemented)
- Player cubes: translucent
- Wave cubes: opaque
- Type hue preserved across both

### A.3 Implemented core visual systems (high level)
- Tile overlays communicate key tile states (marked/blocked/special).
- Face-paint telegraph uses progressive clarity escalation (approach-to-trigger).
- Recursion communicates durability via damage expression.
- Phaseable Infinity is visually distinct from normal Infinity state.

---

## Appendix B — Decision Log (Why These Choices Exist)
- <Decision> → <Reason> → <Trade-off>
- <Decision> → <Reason> → <Trade-off>

---

## Appendix C — Known Failure Cases (Track and Reduce)
- <Failure case: what becomes unclear, when, and why>
- <Failure case: what becomes unclear, when, and why>

---

# Open Questions / Action Items

## Open Questions
1. **Ownership fallback cue:** What is the single additional non-color cue for ownership when translucency is ambiguous (edge treatment, rim, trail, glyph)? Choose one to standardize.
2. **Grayscale discriminators:** In grayscale, what are the primary discriminators between Unit/Matrix/Recursion (value bands, edge treatment, pattern)? Confirm the intended fallback cues.
3. **Divider dominance:** In danger state, what is the maximum allowed visual dominance of the divider (so it remains clear but not distracting)?
4. **Telegraph vs highlight conflicts:** When telegraphs and tutorial highlights overlap, which wins and how is the loser reduced (dim, suppressed, delayed)?
5. **Tile state hierarchy:** When multiple tile states could apply, what is the semantic priority order (not implementation order) so meaning is consistent?

## Action Items
1. **Add Appendix B decisions:** Capture 5–10 key decisions (type palette, ownership strategy, divider language, telegraph grammar, “no noise” cut order).
2. **Populate Appendix C:** List your top 5 current ambiguity cases observed in playtests (dense waves, overlaps, translucency, etc.).
3. **Define the canonical camera distance:** Record one camera framing used for the review artifacts so all comparisons are apples-to-apples.
4. **Create a “Style Review Pack” template:** A short checklist + file naming convention for the 5 required artifacts in Section 9.2.
5. **Align with Sound Architecture:** Ensure the same escalation philosophy (“clarity first, intensity second”) appears in the Sound bible and the same canonical scenarios are used for A/V review.
