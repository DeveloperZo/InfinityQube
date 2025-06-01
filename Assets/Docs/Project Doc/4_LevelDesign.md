# Level Design

> This document details the Level Design section of Infinity Cube's Game Design Document. For full project documentation, see [Game Design Document](GameDesignDocument.md).

## Purpose
Describes the structured progression and learning curve, specifying stage-by-stage instructional goals, complexity scaling, and challenge phases to guide consistent gameplay pacing and player experience.

## 4.1 Learning Curve
### Stage Progression
| Stages | Focus | Content |
|--------|-------|---------|
| 1-2 | Core Loop | Movement, marking, capture |
| 3-6 | Cube Types | Normal → Blue → Black combinations |
| 7-10 | Advanced | Complex mechanics and interactions |
| 11-12 | Mastery | All mechanics combined |

Each stage introduces one primary concept while reinforcing previous knowledge through practical application. The difficulty curve follows a stair-step pattern where each new mechanic is introduced in isolation, then combined with previously learned systems.

## 4.2 Stage Composition
### Core Elements
- **Grid Configuration**
  - Starting size: 3×10
  - Width progression: 3 → 5 → 7 → 11
- **Wave Structure**
  - 2-4-8 waves per stage
  - Teaching-focused objectives
- **Design Elements**
  - Strategic cube patterns
  - Calculated constraints
  - Clear success metrics

## 4.3 Progression Structure
### Tutorial Phase (2 stages)
#### Stage 1 – Movement, Marking & Normal Cubes
- **Grid:** 3×10
- **Waves:** 3×3
- **Learning Goals:**
  - Movement controls
  - Obstacle avoidance
  - Marker placement
  - Normal-cube capture

##### Wave Breakdown
1. **Black Cube Avoidance**
   - Composition: Single centered Black cube
   - Constraints: No markers available
   - Focus: Movement learning
   - Guidance: 
      Messages explaining black cubes
      Message explaining black cubes escaping grid
      Provide information to player if they die
   - Pace: Slow

2. **Normal Cube Capture**
   - Composition: Three strategic Normal cubes
   - Constraints: Two markers maximum
   - Focus: Marker placement optimization
   - Guidance:
      Guided placement hints
      Marker function explanation
      Success feedback on captures
      Learn Grid loss on normal cube escape
   Pace: Normal

3. **Strategic Choice**
   - Composition: Full 3×3 mixed wave
   - Constraints: Limited resources
   - Focus: Acceptable loss concept
   - Guidance:
      Resource management tips
      Loss acceptance messaging
      Strategic priority hints
   - Pace: Normal

#### Stage 2 – Blue Cube Introduction
- **Grid:** 5×15
- **Waves:** 5×5
- **Learning Goals:**
  - Blue cube mechanics


##### Wave Progression
1. **Blue Introduction**
   - Composition: 5 Blue cubes arranged to cascade and clear wave with one marker
   - Constraints: Unlimited markers
   - Focus: Blue cube effect demonstration
   - Guidance:
         Blue cube explanation
         Detonation system introduction
         Area effect demonstration
   - Pace: Slow

2. **Mixed Blue-Normal-Black**
   - Composition: Combined Blue and Normal cubes
   - Constraints: Standard marker limits
   - Focus: Tactical timing practice
   - Guidance:
      Timing strategy tips
      Priority target guidance
      Combo effect explanation
   - Pace: Normal

3. **Blue Enhancement Strategy**
   - Composition: Full 3×3 mixed wave
   - Constraints: Limited markers
   - Focus: Charge stacking mastery
   - Guidance:
         Enhancement mechanics explanation
         Stacking strategy tips
         Advanced combo tutorials
   - Pace: Normal

### Core Mechanics Phase (4 stages)
#### Stage 3 – Blue Cube Fundamentals
- **Grid:** 5×15
- **Focus:** Detonation mastery

#### Stage 4 – Mixed Color Waves
- **Grid:** 5×15
- **Focus:** Multi-type management

#### Stage 5-6
- Content in development

### Challenge Phase (4 stages)
#### Stages 7-10
- Advanced mechanics introduction
- Complex interaction mastery
- Content in development

### Mastery Phase (2 stages)
#### Stage 11 – Dynamic Adaptation
- Variable patterns
- Creative problem-solving
- Multiple valid approaches

#### Stage 12 – Ultimate Test
- Maximum difficulty
- Multi-phase challenges
- Complete mastery validation

---
**Last Updated:** [Current Date]  
**Related Documents:**
- [Game Design Document](GameDesignDocument.md)
- [Game Overview](2_GameOverview.md)
- [Gameplay Mechanics](3_GameplayMechanics.md)