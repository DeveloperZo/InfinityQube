# Infinity Cube Audio Architecture & Implementation Guide

## Updated Audio Architecture Overview

### Current Foundation Status: 70% Complete
✅ **Solid Technical Foundation**: AudioManager, spatial audio, pooling, ScriptableObject configuration  
❌ **Missing Integration Layer**: Event-driven audio system and rhythmic synchronization

## 1. Core Audio Architecture

### 1.1 Event-Driven Audio System

#### GameAudioEvent Enumeration
```csharp
public enum GameAudioEvent
{
    // === CORE RHYTHM EVENTS ===
    RhythmBeat,           // Cosmic metronome - synced to cube movement timing
    WaveStepAdvanced,     // Each step of cube advancement
    RhythmDisrupted,      // When painted cubes break the rhythm
    
    // === CUBE LIFECYCLE EVENTS ===
    CubeLanded,           // When cube reaches new position (per step)
    CubeCaptured,         // When cube is successfully captured by marker
    CubeEscaped,          // When cube leaves the grid area  
    CubeCreated,          // When new cube spawns
    CubeDestroyed,        // When cube is destroyed/removed
    CubeCollision,        // When cube collides with player
    
    // === PLAYER MOVEMENT EVENTS ===
    PlayerMoved,          // Single step movement
    PlayerRunning,        // Continuous movement
    PlayerStopped,        // When player stops moving
    PlayerDamaged,        // When player takes damage
    PlayerRespawned,      // When player respawns after death
    
    // === MARKER EVENTS ===
    LightMarkerPlaced,    // F key marker placement
    PrimeMarkerPlaced,    // G key marker placement  
    HeavyMarkerPlaced,    // V key marker placement
    MarkerTriggered,      // When marker activates/detonates
    MarkerHit,            // When marker successfully hits cube
    MarkerMiss,           // When marker triggers but misses
    MarkerRecharge,       // When marker resources regenerate
    
    // === WAVE EVENTS ===
    WaveStarted,          // Wave initiation (ENTER key)
    WaveCompleted,        // Wave success
    WaveFailed,           // Wave failure
    WavePaused,           // Wave pause state
    WaveResumed,          // Wave resume after pause
    
    // === STAGE EVENTS ===
    StageStarted,         // Beginning of new stage
    StageCompleted,       // Stage completion
    StageFailed,          // Stage failure
    StageTransition,      // Between stages
    
    // === PAINT SYSTEM EVENTS ===
    FacePainted,          // When cube face gets painted
    PaintActivated,       // When painted face becomes active
    CorruptionApplied,    // When corruption effect triggers
    EnhancementApplied,   // When enhancement effect triggers
    PaintSpread,          // When paint spreads to adjacent faces
    
    // === MUSICAL FEEDBACK EVENTS ===
    MelodyNote,           // Musical note for captures
    HarmonyChord,         // Chord progression for combos
    DissonanceBreak,      // When cosmic harmony is disrupted
    
    // === UI EVENTS ===
    ButtonClick,          // UI button interactions
    ButtonHover,          // UI button hover
    MenuOpen,             // Menu opening
    MenuClose,            // Menu closing
    
    // === SYSTEM EVENTS ===
    GameStarted,          // Game initialization
    GamePaused,           // Game pause
    GameResumed,          // Game resume
    GameOver,             // Game over state
    
    // === ATMOSPHERIC EVENTS ===
    AmbienceStart,        // Background ambience
    AmbienceStop,         // Stop background ambience
    IntensityIncrease,    // Tension/difficulty increase
    IntensityDecrease,    // Relaxation/ease
}
```

### 1.2 Audio Event Data Structure
```csharp
[System.Serializable]
public struct AudioEventData
{
    public GameAudioEvent eventType;
    public Vector3 worldPosition;           // For 3D spatial audio
    public float intensity;                 // 0-1 for volume/pitch variation
    public Enumerations.CubeType cubeType;  // For cube-specific events
    public float rhythmTiming;              // For musical timing
    public object additionalData;           // For any extra context
}
```

### 1.3 Enhanced AudioManager Interface
```csharp
public interface IAudioEventSystem
{
    void TriggerAudioEvent(AudioEventData eventData);
    void TriggerAudioEvent(GameAudioEvent eventType, Vector3 position = default, float intensity = 1f);
    void TriggerCubeAudioEvent(GameAudioEvent eventType, Enumerations.CubeType cubeType, Vector3 position, float intensity = 1f);
    void SubscribeToGameEvents();
    void UnsubscribeFromGameEvents();
    void SetRhythmTempo(float beatsPerMinute);
    void StartCosmicMetronome();
    void StopCosmicMetronome();
}
```

## 2. Rhythmic Audio System (NEW - CRITICAL)

### 2.1 Cosmic Metronome Implementation
The cosmic metronome synchronizes with WaveManager's `moveInterval` to create the rhythmic foundation:

```csharp
public class RhythmicAudioController : MonoBehaviour
{
    [Header("Cosmic Metronome")]
    public AudioClipSet rhythmBeatSounds;
    public float beatVolume = 0.3f;
    public bool enableMetronome = true;
    
    [Header("Rhythm Synchronization")]
    public bool syncToWaveManager = true;
    public float customBPM = 120f;
    
    private Coroutine metronomeCoroutine;
    private float currentBeatInterval;
    
    // Subscribe to WaveManager events
    private void OnEnable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnStepAdvanced += OnWaveStepAdvanced;
            WaveManager.Instance.OnWaveStarted += StartMetronome;
            WaveManager.Instance.OnWaveEnded += StopMetronome;
        }
    }
    
    private void OnWaveStepAdvanced()
    {
        if (enableMetronome)
        {
            AudioManager.Instance.TriggerAudioEvent(GameAudioEvent.RhythmBeat);
        }
    }
}
```

### 2.2 Musical Feedback System
Transform player actions into musical elements:

```csharp
public class MusicalFeedbackController : MonoBehaviour
{
    [Header("Capture Melodies")]
    public AudioClipSet melodyNotes;
    public float[] musicalScale = { 1f, 1.125f, 1.25f, 1.33f, 1.5f, 1.67f, 1.875f, 2f };
    
    [Header("Combo System")]
    public AudioClipSet harmonyChords;
    public int minComboForHarmony = 3;
    
    private int currentComboCount = 0;
    private int currentNoteIndex = 0;
    
    public void OnCubeCaptured(Enumerations.CubeType cubeType)
    {
        // Play musical note based on cube type and current sequence
        float pitch = GetPitchForCubeType(cubeType);
        AudioEventData eventData = new AudioEventData
        {
            eventType = GameAudioEvent.MelodyNote,
            intensity = 0.8f,
            additionalData = pitch
        };
        
        AudioManager.Instance.TriggerAudioEvent(eventData);
        
        // Build toward harmony
        currentComboCount++;
        if (currentComboCount >= minComboForHarmony)
        {
            AudioManager.Instance.TriggerAudioEvent(GameAudioEvent.HarmonyChord);
        }
    }
}
```

## 3. Integration Points with Existing Systems

### 3.1 WaveManager Integration
```csharp
// Add to existing WaveManager.cs
public class WaveManager : MonoBehaviour
{
    // Existing code...
    
    public System.Action OnStepAdvanced;
    public System.Action OnWaveStarted;
    public System.Action OnWaveEnded;
    
    // In existing AnimateMove method:
    private void AnimateMove()
    {
        // Existing cube movement code...
        
        // NEW: Trigger rhythm beat
        OnStepAdvanced?.Invoke();
        
        // NEW: Trigger cube landed events for each cube
        foreach (var cube in activeCubes)
        {
            Vector3 worldPos = GridToWorldPosition(cube.position);
            AudioManager.Instance.TriggerCubeAudioEvent(
                GameAudioEvent.CubeLanded, 
                cube.type, 
                worldPos
            );
        }
    }
    
    // In existing OnCubeCaptured method:
    public void OnCubeCaptured(Enumerations.CubeType cubeType)
    {
        // Existing code...
        
        // NEW: Trigger cube captured audio event
        Vector3 cubePosition = GetCapturedCubePosition(cubeType);
        AudioManager.Instance.TriggerCubeAudioEvent(
            GameAudioEvent.CubeCaptured, 
            cubeType, 
            cubePosition
        );
    }
}
```

### 3.2 PlayerActionManager Integration
```csharp
// Add to existing PlayerActionManager.cs
public class PlayerActionManager : MonoBehaviour
{
    // Existing code...
    
    // In existing PlaceMarker methods:
    private void PlaceLightMarker()
    {
        // Existing placement logic...
        
        // NEW: Trigger marker placement audio
        Vector3 worldPos = GridToWorldPosition(playerManager.GridPosition);
        AudioManager.Instance.TriggerAudioEvent(
            GameAudioEvent.LightMarkerPlaced, 
            worldPos, 
            0.8f
        );
    }
    
    // In existing TriggerMarkers method:
    public void TriggerLightMarkers()
    {
        // Existing trigger logic...
        
        // NEW: Trigger marker activation audio for each marker
        foreach (var marker in lightMarkers)
        {
            Vector3 worldPos = GridToWorldPosition(marker.position);
            AudioManager.Instance.TriggerAudioEvent(
                GameAudioEvent.MarkerTriggered, 
                worldPos, 
                1.0f
            );
        }
    }
}
```

### 3.3 PlayerManager Integration
```csharp
// Add to existing PlayerManager.cs  
public class PlayerManager : MonoBehaviour
{
    // Existing code...
    
    // In existing movement methods:
    private void MovePlayer(Vector2Int newPosition)
    {
        // Existing movement logic...
        
        // NEW: Trigger player movement audio
        Vector3 worldPos = GridToWorldPosition(newPosition);
        AudioManager.Instance.TriggerAudioEvent(
            GameAudioEvent.PlayerMoved, 
            worldPos, 
            0.6f
        );
    }
}
```

## 4. Implementation Plan

### Phase 1: Event System Foundation (Week 1)
**Priority: CRITICAL - Core Integration**

#### Day 1-2: Event Enumeration & Data Structures
- [ ] Create `GameAudioEvent` enum in new script
- [ ] Create `AudioEventData` struct
- [ ] Add `IAudioEventSystem` interface to existing AudioManager

#### Day 3-4: AudioManager Event Integration
- [ ] Add event triggering methods to AudioManager
- [ ] Implement event-to-sound mapping system
- [ ] Update existing PlayCubeLandingSound() to use events

#### Day 5-7: Manager Integration
- [ ] Add event triggers to WaveManager (cube landed, captured, escaped)
- [ ] Add event triggers to PlayerActionManager (marker placement, triggering)
- [ ] Add event triggers to PlayerManager (movement)
- [ ] Test all basic event triggering

**Completion Criteria**: All existing manual audio calls replaced with event triggers

### Phase 2: Rhythmic Audio System (Week 2)
**Priority: CRITICAL - Cosmic Metronome**

#### Day 1-3: Rhythm Controller Implementation
- [ ] Create RhythmicAudioController component
- [ ] Implement WaveManager synchronization
- [ ] Add cosmic metronome beat generation
- [ ] Sync beat timing to cube movement intervals

#### Day 4-5: Musical Feedback System
- [ ] Create MusicalFeedbackController component
- [ ] Implement capture-to-melody system
- [ ] Add combo-to-harmony progression
- [ ] Integrate with existing statistics system

#### Day 6-7: Paint System Audio Integration
- [ ] Add paint application audio events
- [ ] Implement "rhythm disruption" when painted cubes activate
- [ ] Add visual-audio sync for cosmic liquid effects

**Completion Criteria**: Players can hear and internalize the cosmic rhythm

### Phase 3: Audio Content & Polish (Week 3)
**Priority: HIGH - Content & Atmosphere**

#### Day 1-3: AI-Generated Sound Content
- [ ] Generate core action sounds using ElevenLabs
- [ ] Create ambient cosmic background using Soundraw
- [ ] Generate musical feedback tones and chords
- [ ] Import and configure in Unity with proper settings

#### Day 4-5: Atmospheric Audio System
- [ ] Implement background ambient audio controller
- [ ] Add intensity-based audio scaling
- [ ] Create stage transition audio
- [ ] Add UI interaction sounds

#### Day 6-7: Testing & Polish
- [ ] Comprehensive audio system testing
- [ ] Volume balancing and mixing
- [ ] Performance optimization
- [ ] Bug fixes and refinement

**Completion Criteria**: Complete audio experience supporting "Intelligent Mastery of Cube Rhythms with Cosmic Wanderlust"

### Phase 4: Advanced Features (Week 4+)
**Priority: MEDIUM - Enhancement**

- [ ] Dynamic music system that responds to gameplay intensity
- [ ] Advanced paint system audio (corruption/enhancement effects)
- [ ] Accessibility options (visual rhythm indicators for hearing impaired)
- [ ] Audio achievement system integration
- [ ] Advanced spatial audio for large grids

## 5. Success Metrics

### Technical Metrics
- [ ] All 25+ identified game events trigger appropriate audio
- [ ] Audio system maintains 60fps performance with no frame drops
- [ ] Memory usage stays under 50MB for audio assets
- [ ] Zero audio-related bugs or glitches

### Design Metrics  
- [ ] Players can identify cube movement rhythm within 30 seconds
- [ ] Musical feedback creates satisfying "conducting" feeling
- [ ] Paint effects feel like "cosmic disruption" of rhythm
- [ ] Overall audio supports "cosmic wanderlust" aesthetic

### Player Experience Metrics
- [ ] Playtesters report enhanced engagement with audio on
- [ ] Audio provides clear feedback for off-screen events
- [ ] Musical elements feel integrated, not distracting
- [ ] Cosmic theme is reinforced through audio design

## 6. Audio Asset Requirements

### Core Sound Effects (ElevenLabs Generation)
```
High Priority:
- Marker placement sounds (3 variations)
- Cube capture by type (Unit, Prime, Infinity, Dense - 2 each)
- Cosmic metronome beat (3 variations)
- Wave start/complete sounds (1 each)

Medium Priority:  
- Paint application sounds (2 variations)
- Detonation effects (3 variations)
- Player movement sounds (2 variations)
- UI interaction sounds (3 variations)

Low Priority:
- Advanced paint effects (corruption/enhancement)
- Musical feedback tones (8 note scale)
- Atmospheric enhancement effects
```

### Ambient & Musical Content (Soundraw Generation)
```
- Background cosmic ambient (2-3 minute loop)
- Intensity layers (calm, active, tense)
- Musical progression elements
- Stage transition stingers
```

## 7. Quality Standards

### Audio Technical Standards
- **Format**: WAV for SFX (<3 sec), OGG for ambient/music (>10 sec)
- **Quality**: PCM for SFX, Vorbis 70% for longer audio
- **Volume**: Consistent levels, -12dB to -6dB range
- **Length**: SFX under 3 seconds, ambient 2-5 minutes

### Design Standards
- **Cosmic Theme**: All audio supports space/cosmic aesthetic
- **Clarity**: Clear distinction between different event types
- **Integration**: Audio feels connected to game mechanics
- **Accessibility**: Important information conveyed both visually and auditorily

## 8. Legacy System Integration

### Existing AudioManager Features to Preserve
✅ **Keep**: Spatial audio system, object pooling, volume controls, debug tools  
✅ **Enhance**: Add event triggering layer on top of existing foundation  
✅ **Extend**: Integrate with CubeAudioConfiguration system

### Migration Strategy
1. **Phase 1**: Add event system alongside existing manual calls
2. **Phase 2**: Gradually replace manual calls with event triggers  
3. **Phase 3**: Remove deprecated manual calling methods
4. **Phase 4**: Full event-driven audio system

---

**Last Updated:** December 29, 2024  
**Architecture Status:** Event system designed, implementation plan defined  
**Next Milestone:** Phase 1 - Event System Foundation implementation