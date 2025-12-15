# Sound Architecture — Infinity Cube (Style Bible)

> **Role:** Style bible for audio (not an implementation guide).  
> **Purpose:** Define the sonic identity, mix priorities, and atmospheric behavior so audio stays meditative, readable, and coherent under load.  
> **Companion Doc:** Visual Architecture (Style Bible).  
> **Audience:** You, audio designer, contractors/freelancers, tech audio.

---

## 0. Document Control
- **Owner:**
- **Reviewers:**
- **Version / Date:** December 14, 2025
- **Change Log:** Refined to emphasize style guardrails; implementation specifics moved to appendices.
- **Scope:** Sonic identity + mix priorities + rhythm philosophy + atmosphere behavior + collaboration gates
- **Out of Scope:** Exact mixer graphs, sample rates, Unity import minutiae, and per-asset numeric tuning (unless it protects meaning/readability).

---

## 1. North Star
### 1.1 One-line fantasy
Meditative cosmic puzzle strategy where sound reinforces timing, focus, and consequence without overwhelming the player’s attention.

### 1.2 Pillars (3–5)
- **Clarity Over Intensity:** Critical cues remain recognizable even in dense waves.
- **Rhythmic Support:** Audio reinforces the step cadence to support planning and flow.
- **Cosmic Lo-fi Restraint:** Atmosphere enhances focus; it never becomes attention-seeking.
- **Meaningful Feedback:** Actions and outcomes have distinct, learnable sonic signatures.
- **Cause-and-Effect Audibility:** Players can hear what happened and infer why.

### 1.3 Anti-goals (explicit “no” list)
- Never harsh or fatiguing.
- Never masks gameplay cues.
- Never becomes chaotic under density.
- Never introduces “new instruments” that increase learning burden without gameplay value.

---

## 2. Audio Readability (Non-Negotiables)
### 2.1 Mix priority order (what must win)
1. **Gameplay-critical cues:** landings, escapes, danger/warnings, errors
2. **Player actions:** placement, triggers/detonations, confirmations
3. **State cues:** painted/phaseable/durability changes, captures
4. **Progress cues:** wave start/complete, major transitions
5. **Atmosphere:** pads/drones/space texture
6. **UI polish:** message/menus/mode switch polish sounds

### 2.2 Clarity under load (guardrails)
- Dense moments must remain intelligible: critical cues cannot be “lost in the wash.”
- Avoid frequency masking of critical cues by atmosphere or repeated impacts.
- When density increases, the system should reduce *non-critical* layers before compressing everything.

### 2.3 Fatigue guardrails
- No piercing highs; no sharp, clicky transients as a common texture.
- Repetition is softened via **small** variation (timing micro-variation, subtle pitch drift, alternate takes) without erasing identity.
- Loudness escalation is a last resort; clarity is achieved first through separation and restraint.

---

## 3. Sonic Identity (What Makes the Game “Itself”)
### 3.1 Signature sounds (core set)
These define “Infinity Cube” and must remain stable across iterations:
- **Infinity cube signature impact** (brand sound)
- **Capture confirmation** (reward language)
- **Escape / failure warning** (threat language)
- **Marker placement + trigger language** (intent language)
- **Wave transition stings** (progress language; restrained)

### 3.2 Timbre grammar (high-level, learnable)
Each cube type has a recognizable identity that persists through density and state changes:

**Unit**
- **Signature:** clean and dependable; the “meter” of the game.
- **Do:** keep it neutral, readable, consistent.
- **Don’t:** add ornate character that competes with higher-value types.

**Matrix**
- **Signature:** “valuable/strategic” presence; lightly elevated.
- **Do:** add gentle harmonic richness (subtle, not musical dominance).
- **Don’t:** make it as loud or heavy as Recursion or as uncanny as Infinity.

**Recursion**
- **Signature:** heavier, more substantial, communicates durability.
- **Do:** express multi-hit/durability through subtle progression (not a whole new sound set).
- **Don’t:** let durability expression erase the base identity.

**Infinity**
- **Signature:** ominous, uncanny, unmistakable; communicates “obstacle/threat.”
- **Do:** maintain a distinct spectral footprint (brand recognition).
- **Don’t:** make it “rewarding,” cute, or overly musical.

### 3.3 Semantic channels (meaning budget)
To prevent audio overload, meanings are assigned to specific channels:
- **Timbre:** cube type identity (primary)
- **Loudness / presence:** urgency and warnings (used sparingly)
- **Pitch drift / modulation:** state influence (subtle; bounded)
- **Space (reverb/width):** atmosphere and density behavior
- **Rhythm / timing:** cadence reinforcement and telegraph lead

Rule: **One channel ≈ one meaning.** New meanings must not hijack channels already reserved for type identity.

### 3.4 Ownership distinction (player vs wave)
- Must be subtle; do not double the learning burden.
- Prefer **spatial placement / presence** differences over entirely different instruments.
- Preserve cube type identity across ownership.

---

## 4. Rhythm & Timing Philosophy (Strategy)
### 4.1 Relationship to gameplay rhythm
- Audio supports the step cadence and planning mindset.
- Impacts, confirmations, and warnings feel intentional and consistent.
- Silence and negative space are part of the meditative feel.

### 4.2 When audio should lead vs follow
- **Follow:** impacts, captures, confirmations (they affirm what happened).
- **Lead:** warnings/telegraphs (they help the player plan).

### 4.3 Restraint principle for cadence
- Avoid adding extra rhythmic layers that compete with the step cadence.
- If atmosphere contains motion, it should be slow and secondary.

---

## 5. Atmosphere System (Meditative Evolution)
### 5.1 Density-to-texture intent
- More cubes → fuller bed, less prominence per impact (cohesion).
- Fewer cubes → more space, more intimacy per sound (focus).
- Atmosphere exists to support *attention*, not to demand it.

### 5.2 States (conceptual, not technical)
- **Full wave:** cohesive wash; impacts blend but remain readable.
- **Mid density:** balanced separation and clarity.
- **Sparse:** air and space; impacts become more characterful.
- **Solo:** intimate; a single sound has room to breathe.
- **Silence:** resolution and breath; minimal tail, no clutter.

### 5.3 Transition rules
- Transitions are smooth and non-attention-grabbing.
- Avoid “thrashing” as density changes rapidly (use hysteresis conceptually).
- In transitions, protect critical cues first, atmosphere second.

---

## 6. Modulation Rules (Paint / Phaseable / Corruption)
Modulation reflects state without overwriting cube identity.

### 6.1 General preservation rules
- Cube type identity must remain recognizable under all state changes.
- Modulation must be subtle and bounded (no large pitch jumps, no aggressive distortion).
- If modulation reduces clarity under load, it is removed or reduced.

### 6.2 State intent + allowed transformations
**Face painting**
- **Goal:** communicate “modified behavior / impending effect” without chaos.
- **Allowed:** subtle filter tilt, gentle shimmer, mild detune, slight transient softening.
- **Forbidden:** harsh distortion, wide pitch variance, complete timbre replacement.

**Phaseable**
- **Goal:** communicate temporary pass-through state clearly but calmly.
- **Allowed:** subtle spatial widening, light airy component, gentle high-end lift (non-piercing).
- **Forbidden:** loud “power-up” stings that steal attention from planning.

**Corruption / danger-influence**
- **Goal:** communicate risk/hostility without becoming fatiguing.
- **Allowed:** mild dissonance/roughness, subtle instability, restrained warning layer.
- **Forbidden:** abrasive noise beds or aggressive distortion as a default texture.

---

## 7. Spatial and “Space” (Aesthetic, Not Settings)
- Spatialization supports calm immersion, not spectacle.
- Distance behavior should reduce clutter while preserving meaning:
  - critical cues remain intelligible
  - non-critical layers fade first
- The world should feel like a coherent acoustic space, not a collection of isolated sounds.

---

## 8. Quality Gates (Audio Regression)
### 8.1 Canonical test scenarios (must be used for review)
- Worst-case density wave
- Sparse/solo wave
- High warning/telegraph moments
- Rapid repeated actions (stress)

### 8.2 Required review artifacts (for any notable audio change)
Provide:
1. 20–30 second clip (worst-case density)
2. 20–30 second clip (sparse/solo)
3. 10 second clip (warning/telegraph lead example)
4. A/B comparison (old vs new) for any signature sound changes
5. Note: “what guardrail changed and why”

### 8.3 Pass/fail checks
- Critical cues are audible and recognizable under density.
- No clipping / no harshness / no fatigue over repeated loops.
- Atmosphere supports focus rather than distracts.
- Signature sound remains distinctly “Infinity Cube.”

---

## 9. Collaboration Notes (Audio Designers / Freelancers)
- **Requires Approval:** signature sounds, warning grammar, timbre grammar for cube types, and any new semantic channels.
- **Open for Experimentation:** ambient textures, subtle variation strategy, transitional tails, gentle harmonic polish.
- **Submission Process:** include artifacts in Section 8.2 + a short note linking changes to the pillars/guardrails.

---

## Appendix A — Current Implementation Notes (Reference Only)
> Captures what exists today. The sections above are the source of truth for the style.

### A.1 Implemented event-driven audio coverage (high level)
- Cube lifecycle cues (land, capture, escape)
- Marker placement/trigger cues
- Wave start/complete cues
- Mode switch + message cues
- Error/success feedback cues

### A.2 Implemented configuration approach (high level)
- Cube audio behavior is configurable per cube type and category.
- Volume control supports a small set of top-level categories.

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
1. **Infinity signature definition:** What is the “unmistakable” quality of the Infinity signature sound (e.g., weight, spectral shape, tail behavior)? Write a one-sentence identity anchor.
2. **Warning grammar:** What is the consistent audio language for danger (escape/divider risk) that escalates by clarity first, intensity second?
3. **Variation bounds:** How much variation is allowed per cube type before recognition degrades (pitch drift range conceptually, take count targets)?
4. **Ownership cue (if any):** Do you want a subtle ownership difference in audio at all, or should ownership be purely visual?
5. **Atmosphere role split:** Should atmosphere be purely “bed” or should it subtly reflect density/state (and if so, what is the maximum allowed prominence)?

## Action Items
1. **Write identity anchors:** Add a one-sentence “signature anchor” for each cube type (Unit/Matrix/Recursion/Infinity).
2. **Define warning escalation ladder:** A 3-step ladder (e.g., subtle cue → clearer cue → urgent cue) with the restraint rule.
3. **Create an audio review pack template:** Naming + the required clips (Section 8.2) to standardize A/B review.
4. **Populate Appendix C:** List 5 ambiguity/fatigue cases observed during play (dense waves, repeated impacts, warnings getting lost, etc.).
5. **Align with Visual Architecture:** Ensure both docs share the same escalation philosophy and canonical scenario list for joint A/V review.

---