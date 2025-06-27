# AI Audio Implementation Guide for Infinity Cube

## Quick Start Workflow

### Step 1: Tool Setup
1. **ElevenLabs Account** (Primary SFX Generation)
   - Sign up at https://elevenlabs.io/sound-effects
   - Subscribe to Starter plan ($5/month) or Creator plan ($22/month)
   - Access Text-to-Sound generator

2. **Soundraw Account** (Ambient/Background Music)
   - Sign up at https://soundraw.io
   - Subscribe to Creator plan ($19.99/month)
   - Enables commercial game use

### Step 2: Audio Generation Process

#### Core Game Sound Effects (ElevenLabs)

**Marker Placement Sounds:**
```
Prompts:
- "Soft cosmic chime, crystalline marker placement on grid"
- "Gentle electronic beep, precise placement sound"
- "Subtle energy pulse, marker activation tone"
```

**Cube Capture Sounds by Type:**
```
Unit Cube:
- "Small cube absorption, gentle whoosh with crystalline chime"
- "Light energy capture, soft harmonic resonance"

Prime Cube:
- "Powerful cube capture, deep harmonic bass with energy crackling"
- "Prime energy absorption, rich resonant tone with sparkles"

Infinity Cube:
- "Mystical infinite cube capture, ethereal harmonics with cosmic echo"
- "Reality-bending sound, infinite reverb with celestial tones"

Recursion Cube:
- "Heavy matter compression, deep thrum with gravitational pull sound"
- "Recursion core collapse, low frequency pulse with metallic resonance"
```

**Paint System Effects:**
```
Corruption (Dark Matter):
- "Dark matter corruption spreading, ominous bubbling with distortion"
- "Cosmic liquid spreading, eerie whispers with static interference"

Enhancement (Stellar Plasma):
- "Stellar plasma enhancement, bright energetic fizzing with sparkles"
- "Cosmic liquid empowerment, uplifting harmonics with gentle bubbling"
```

**System Feedback:**
```
- "Wave completion success, triumphant cosmic chord progression"
- "Resource regeneration, gentle energy building with soft chimes"
- "Detonation sequence, controlled explosion with harmonic aftermath"
```

#### Ambient Cosmic Audio (Soundraw)

**Background Atmosphere Settings:**
- Genre: Ambient, Electronic, Cinematic
- Mood: Mysterious, Peaceful, Cosmic
- Tempo: Slow (60-80 BPM)
- Length: 2-5 minutes (for looping)
- Instruments: Pads, Ambient textures, Subtle percussion

**Adaptive Music Layers:**
- **Exploration Phase**: Gentle cosmic ambient
- **Active Phase**: Slightly more rhythmic with subtle pulse
- **Tension Phase**: Deeper bass with cosmic tension
- **Success Phase**: Uplifting harmonic resolution

### Step 3: Unity Integration

#### Audio Folder Structure
```
Assets/Audio/
├── SFX/
│   ├── Core/
│   │   ├── SFX_MarkerPlace_01.wav
│   │   ├── SFX_MarkerPlace_02.wav
│   │   └── SFX_MarkerPlace_03.wav
│   ├── Cubes/
│   │   ├── SFX_CubeCapture_Unit_01.wav
│   │   ├── SFX_CubeCapture_Prime_01.wav
│   │   ├── SFX_CubeCapture_Infinity_01.wav
│   │   └── SFX_CubeCapture_Recursion_01.wav
│   ├── Paint/
│   │   ├── SFX_Paint_Corruption_01.wav
│   │   ├── SFX_Paint_Enhancement_01.wav
│   │   └── SFX_Paint_Spread_01.wav
│   └── System/
│       ├── SFX_WaveComplete_01.wav
│       ├── SFX_ResourceRegen_01.wav
│       └── SFX_Detonation_01.wav
├── Ambient/
│   ├── AMB_CosmicBackground_Calm.ogg
│   ├── AMB_CosmicBackground_Active.ogg
│   └── AMB_CosmicBackground_Tension.ogg
└── Music/
    ├── MUS_MainTheme.ogg
    └── MUS_GameplayLoop.ogg
```

#### Audio Import Settings

**For Short SFX (< 2 seconds):**
- Load Type: Decompress On Load
- Compression Format: PCM
- Quality: 100%
- Force To Mono: Check (if appropriate)

**For Ambient/Music (> 10 seconds):**
- Load Type: Compressed In Memory
- Compression Format: Vorbis
- Quality: 70%
- Force To Mono: Uncheck

#### Basic Unity AudioManager Integration

```csharp
// Add this to existing audio management system
[System.Serializable]
public class CubeAudioClips
{
    [Header("Cube Capture Sounds")]
    public AudioClip unitCubeCapture;
    public AudioClip primeCubeCapture;
    public AudioClip infinityCubeCapture;
    public AudioClip recursionCubeCapture;
    
    [Header("Paint System")]
    public AudioClip paintCorruption;
    public AudioClip paintEnhancement;
    
    [Header("Core Actions")]
    public AudioClip markerPlacement;
    public AudioClip detonation;
    public AudioClip waveComplete;
}

// Integration example for existing cube capture system
public void PlayCubeCaptureSound(CubeType cubeType)
{
    AudioClip clipToPlay = cubeType switch
    {
        CubeType.Unit => cubeAudioClips.unitCubeCapture,
        CubeType.Prime => cubeAudioClips.primeCubeCapture,
        CubeType.Infinity => cubeAudioClips.infinityCubeCapture,
        CubeType.Recursion => cubeAudioClips.recursionCubeCapture,
        _ => cubeAudioClips.unitCubeCapture
    };
    
    if (clipToPlay != null)
    {
        AudioSource.PlayClipAtPoint(clipToPlay, transform.position);
    }
}
```

### Step 4: Quality Control Process

#### Audio Standards Checklist
- [ ] **Volume Consistency**: All SFX within -12dB to -6dB range
- [ ] **Format Optimization**: WAV for short SFX, OGG for longer audio
- [ ] **No Clipping**: Check for audio distortion or clipping
- [ ] **Appropriate Length**: SFX under 3 seconds, ambient 2-5 minutes
- [ ] **Cosmic Theme**: Audio matches game's cosmic aesthetic
- [ ] **Unity Compatibility**: All files import without errors

#### Testing Workflow
1. **Generate 3-5 variations** of each sound type
2. **Import into Unity** and test playback
3. **Test in-game integration** with existing systems
4. **Gather feedback** on audio quality and theme fit
5. **Iterate** based on gameplay testing

### Step 5: Batch Generation Strategy

#### Week 1 Priority Sounds
```
High Priority (Generate First):
1. Marker placement (3 variations)
2. Unit cube capture (2 variations)
3. Prime cube capture (2 variations)
4. Basic cosmic ambient background (1 track)

Medium Priority:
5. Infinity cube capture (2 variations)
6. Recursion cube capture (2 variations)
7. Wave completion sound (1 variation)
8. Detonation sound (2 variations)

Low Priority:
9. Paint corruption effects (2 variations)
10. Paint enhancement effects (2 variations)
11. Additional ambient layers
```

### Step 6: Naming Convention

#### File Naming Format
```
[Category]_[Type]_[Variation].[ext]

Examples:
- SFX_MarkerPlace_01.wav
- SFX_CubeCapture_Unit_01.wav
- AMB_CosmicBackground_Calm.ogg
- MUS_GameplayLoop.ogg
```

#### Category Codes
- **SFX**: Sound Effects
- **AMB**: Ambient Audio
- **MUS**: Music
- **STG**: Stingers (short musical phrases)

### Step 7: Integration with Existing Code

#### Locate Audio Integration Points
Based on the project analysis, integrate audio calls in these existing systems:

1. **Marker System**: Add audio to marker placement logic
2. **Cube Capture**: Enhance cube capture events with audio
3. **Detonation System**: Add explosion sound effects
4. **Wave Management**: Audio for wave start/complete
5. **Paint System**: Sound effects for face painting mechanics

#### Example Integration Points
```csharp
// In marker placement system
public void PlaceMarker()
{
    // Existing marker logic...
    
    // Add audio
    AudioManager.Instance.PlayMarkerPlacementSound();
}

// In cube capture system
public void OnCubeCapture(CubeController cube)
{
    // Existing capture logic...
    
    // Add audio
    AudioManager.Instance.PlayCubeCaptureSound(cube.CubeType);
}
```

## Troubleshooting Common Issues

### Audio Not Playing
1. Check AudioSource component is present
2. Verify AudioListener is in scene
3. Confirm audio files imported correctly
4. Check volume levels and mute settings

### Performance Issues
1. Use compressed formats for longer audio
2. Limit concurrent audio sources
3. Consider audio pooling for frequent SFX
4. Monitor memory usage with Unity Profiler

### Quality Issues
1. Check original generation settings
2. Verify Unity import settings
3. Test audio levels for consistency
4. Consider re-generating with different prompts

## Next Steps After Implementation

1. **Player Testing**: Gather feedback on audio enhancement
2. **Iteration**: Refine sounds based on gameplay experience
3. **Expansion**: Generate additional variations and special effects
4. **Optimization**: Fine-tune performance and memory usage
5. **Polish**: Add subtle audio features like dynamic mixing

---

This guide provides a complete workflow for implementing AI-generated audio in Infinity Cube, from tool setup through Unity integration and quality control.
