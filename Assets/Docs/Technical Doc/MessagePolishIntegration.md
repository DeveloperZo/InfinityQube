# Message Polish Integration Guidelines

## Overview

This document outlines the integration guidelines for implementing polish features (animations, voice, and enhanced audio) with the message system in InfinityQube. The foundation hooks and event system provide clean integration points without coupling the core message system to specific polish implementations.

## Current Implementation Status (July 2025)

### ✅ Completed Features

#### Wave Completion Messages (July 8, 2025)
- **Implementation**: `WaveManager.ShowWaveCompletionMessage()`
- **Features**:
  - Progress tracking display ("Wave X/Y")
  - Capture/escape statistics
  - Pause functionality with K to continue
  - Tutorial-specific message handling
- **Integration**: Uses existing `WaveMessage` system with `RequirePause` flag

#### Stage Transition Messages (July 8, 2025)
- **Implementation**: `StageManager.HandleStageSuccess()`
- **Features**:
  - Demo completion message with final statistics
  - Time display formatting
  - Smooth transition to splash screen
  - Comprehensive cleanup before scene changes
- **Integration**: Leverages `WaveManager.ShowMessage()` for consistency

### 🔄 Pending Integration
- Animation system hookup
- Voice over support
- Enhanced audio cues for message types
- Category-based message styling

## Architecture Overview

### Core Components

1. **IMessagePolishHooks** - Interface defining all polish integration points
2. **MessagePolishEvents** - UnityEvent-based system for external polish system binding
3. **GameAudioEvent Extensions** - New audio events for message lifecycle
4. **TutorialMessageManager Integration** - Automatic hook triggering during message display

### Integration Points

The polish foundation provides three main lifecycle hooks:

- `OnMessageShow` - Triggered when a message begins displaying
- `OnMessageHide` - Triggered when a message finishes displaying normally
- `OnMessageSkip` - Triggered when a message is skipped by user input

## Implementation Guidelines

### Animation System Integration

#### Basic Animation Hook Implementation

```csharp
public class MessageAnimationHandler : MonoBehaviour
{
    [SerializeField] private Animator messageAnimator;
    
    private void Start()
    {
        // Subscribe to polish events
        if (MessagePolishEvents.Instance != null)
        {
            MessagePolishEvents.Instance.OnMessageShow.AddListener(OnMessageShowAnimation);
            MessagePolishEvents.Instance.OnMessageHide.AddListener(OnMessageHideAnimation);
        }
    }
    
    private void OnMessageShowAnimation(MessagePolishEventData eventData)
    {
        // Trigger entrance animation based on message category
        string animationTrigger = GetAnimationTrigger(eventData.category, "show");
        messageAnimator.SetTrigger(animationTrigger);
        
        // Set animation speed based on category importance
        float animationSpeed = GetAnimationSpeed(eventData.category);
        messageAnimator.speed = animationSpeed;
    }
    
    private void OnMessageHideAnimation(MessagePolishEventData eventData)
    {
        // Trigger exit animation, different if skipped
        string animationType = eventData.wasSkipped ? "skip" : "hide";
        string animationTrigger = GetAnimationTrigger(eventData.category, animationType);
        messageAnimator.SetTrigger(animationTrigger);
    }
}
```

#### Animation Categories and Timing

**Essential Messages** (0.5s default)
- Prominent entrance with attention-grabbing elements
- Slower, more deliberate animations
- Enhanced visual emphasis (glow, scale, etc.)

**Important Messages** (0.3s default)
- Standard smooth entrance/exit
- Professional, polished feel
- Balanced timing for readability

**Contextual Messages** (0.2s default)
- Quick, subtle animations
- Non-intrusive appearance
- Faster transitions to avoid interrupting flow

**Debug Messages** (0.1s default)
- Minimal animation
- Fast, functional transitions
- Developer-focused simplicity

### Audio System Integration

#### Audio Event Integration

The system automatically integrates with AudioManager through the GameAudioEvent system:

```csharp
// New GameAudioEvent values
Enumerations.GameAudioEvent.MessageShow  // Message starts displaying
Enumerations.GameAudioEvent.MessageHide  // Message finishes normally
Enumerations.GameAudioEvent.MessageSkip  // Message skipped by user
```

#### Custom Audio Implementation

```csharp
public class MessageAudioHandler : MonoBehaviour
{
    [SerializeField] private AudioClip messageShowSound;
    [SerializeField] private AudioClip messageSkipSound;
    
    private void Start()
    {
        if (MessagePolishEvents.Instance != null)
        {
            MessagePolishEvents.Instance.OnMessageAudio.AddListener(OnMessageAudioEvent);
        }
    }
    
    private void OnMessageAudioEvent(Enumerations.GameAudioEvent audioEvent, MessagePolishEventData eventData)
    {
        switch (audioEvent)
        {
            case Enumerations.GameAudioEvent.MessageShow:
                PlayMessageShowSound(eventData.category);
                break;
            case Enumerations.GameAudioEvent.MessageSkip:
                PlayMessageSkipSound();
                break;
            // MessageHide intentionally subtle/silent
        }
    }
    
    private void PlayMessageShowSound(Enumerations.MessageCategory category)
    {
        float volume = GetVolumeForCategory(category);
        AudioSource.PlayClipAtPoint(messageShowSound, Camera.main.transform.position, volume);
    }
}
```

### Voice System Integration (Placeholder)

The foundation provides placeholder hooks for future voice system integration:

```csharp
public class MessageVoiceHandler : MonoBehaviour
{
    private void Start()
    {
        if (MessagePolishEvents.Instance != null)
        {
            MessagePolishEvents.Instance.OnMessageVoice.AddListener(OnMessageVoiceRequest);
        }
    }
    
    private void OnMessageVoiceRequest(string messageText, string messageId, int priority)
    {
        // Future implementation:
        // - Text-to-speech conversion
        // - Voice queue management
        // - Accessibility features
        // - Multi-language support
        
        Debug.Log($"Voice request: {messageText} (Priority: {priority})");
    }
}
```

## Configuration and Customization

### Polish Feature Control

```csharp
// Enable/disable polish features at runtime
MessagePolishEvents.Instance.SetPolishEnabled(
    animations: true,  // Enable message animations
    audio: true,       // Enable message audio feedback
    voice: false       // Enable voice reading (placeholder)
);

// Customize animation timing
MessagePolishEvents.Instance.SetAnimationDurations(
    defaultDuration: 0.3f,     // Standard messages
    essentialDuration: 0.5f,   // Essential messages
    contextualDuration: 0.2f   // Contextual hints
);
```

### Category-Based Customization

Different message categories should receive different polish treatment:

| Category | Animation Style | Audio Feedback | Voice Priority |
|----------|----------------|----------------|----------------|
| Essential | Prominent, attention-grabbing | Clear, noticeable | Highest (3) |
| Important | Smooth, professional | Standard volume | High (2) |
| Contextual | Subtle, non-intrusive | Quiet/minimal | Normal (1) |
| Debug | Minimal, fast | Very quiet | Low (0) |

## Performance Considerations

### Animation Performance

- Use object pooling for animated UI elements
- Limit concurrent animations (max 3-5 simultaneously)
- Optimize for mobile/low-end devices
- Consider disabling animations in performance mode

### Audio Performance

- Leverage existing AudioManager pooling system
- Use short, compressed audio clips for feedback
- Respect user audio settings and volume controls
- Implement audio ducking for important messages

### Memory Management

- Cache commonly used animation clips
- Unload unused voice resources
- Monitor UnityEvent listener count
- Clean up event subscriptions properly

## Testing and Validation

### Integration Testing

```csharp
[ContextMenu("Test Polish Integration")]
public void TestPolishIntegration()
{
    if (MessagePolishEvents.Instance != null)
    {
        // Test all event types
        MessagePolishEvents.Instance.TestShowEvent();
        StartCoroutine(DelayedHideTest());
        StartCoroutine(DelayedSkipTest());
    }
}

private IEnumerator DelayedHideTest()
{
    yield return new WaitForSeconds(2f);
    MessagePolishEvents.Instance.TestHideEvent();
}

private IEnumerator DelayedSkipTest()
{
    yield return new WaitForSeconds(1f);
    MessagePolishEvents.Instance.TestSkipEvent();
}
```

### Debug Information

Monitor polish system performance through debug interfaces:

```csharp
// Get comprehensive event statistics
var stats = MessagePolishEvents.Instance.GetEventStatistics();
Debug.Log($"Polish Events: {stats["Total Events Triggered"]}");
Debug.Log($"Animations: {stats["Animation Events"]}");
Debug.Log($"Audio: {stats["Audio Events"]}");
```

## Future Enhancement Guidelines

### Animation System Expansion

- **UI Transitions**: Smooth panel slide-ins, fade effects
- **Particle Effects**: Cosmic particles for important messages
- **3D Elements**: Floating message panels in game space
- **Responsive Design**: Animations that adapt to screen size

### Advanced Audio Features

- **Spatial Audio**: Messages positioned in 3D space
- **Dynamic Music**: Background music that responds to message urgency
- **Environmental Audio**: Messages that interact with game ambient sound
- **Accessibility**: Audio descriptions for visual elements

### Voice System Features

- **Text-to-Speech**: Real-time TTS for all messages
- **Voice Acting**: Recorded voice clips for key messages
- **Multi-Language**: Localized voice support
- **Accessibility**: Screen reader integration

### Context-Aware Polish

- **Adaptive Timing**: Animation speed based on player reading speed
- **Emotional Context**: Polish that matches game mood/tension
- **Player Preference**: User-customizable polish levels
- **Performance Scaling**: Automatic polish reduction on low-end devices

## Integration Checklist

When implementing polish features, ensure:

- [ ] Subscribe to appropriate MessagePolishEvents
- [ ] Handle all message categories appropriately
- [ ] Respect performance limitations
- [ ] Provide configuration options
- [ ] Test with TutorialMessageManager integration
- [ ] Validate audio integration with AudioManager
- [ ] Document any new polish capabilities
- [ ] Consider accessibility implications
- [ ] Test on target performance devices
- [ ] Implement graceful degradation for missing systems

## Best Practices

1. **Separation of Concerns**: Keep polish implementation separate from core message logic
2. **Performance First**: Polish should enhance, not hinder, user experience
3. **Graceful Degradation**: System should work even if polish features fail
4. **User Control**: Always provide options to disable polish features
5. **Accessibility**: Consider users with visual, audio, or motor impairments
6. **Platform Awareness**: Different polish levels for different platforms
7. **Testing Coverage**: Test all combinations of enabled/disabled features
8. **Documentation**: Keep integration guidelines updated as features expand

## Troubleshooting

### Common Issues

**Polish events not triggering**: Verify MessagePolishEvents.Instance is available before TutorialMessageManager initialization

**Audio not playing**: Check AudioManager integration and ensure GameAudioEvent handling is implemented

**Animation conflicts**: Ensure only one animation system is controlling message UI at a time

**Performance issues**: Monitor event frequency and implement throttling if necessary

**Memory leaks**: Properly unsubscribe from UnityEvents in OnDestroy methods

### Debug Commands

```csharp
// Print current polish system state
MessagePolishEvents.Instance.PrintStatistics();

// Test individual event types
MessagePolishEvents.Instance.TestShowEvent();
MessagePolishEvents.Instance.TestHideEvent();
MessagePolishEvents.Instance.TestSkipEvent();

// Reset event statistics
MessagePolishEvents.Instance.ResetStatistics();
```
