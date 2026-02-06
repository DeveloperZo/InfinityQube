# Scenario Scene Setup

## Overview

Scenarios can now be tied to specific scenes instead of stage data. This allows each scenario to have its own scene setup, making it easier to test scenarios in isolation.

## Creating a Scenario Scene

### For D001_BasicPlayerDeath

1. **Duplicate the Stage scene**:
   - In Unity, right-click `Assets/Scenes/Stage.unity`
   - Select "Duplicate"
   - Rename to `D001_Scene.unity`

2. **Verify scene setup**:
   - Ensure all required managers are present:
     - GridManager
     - WaveManager
     - PlayerManager
     - PlayerActionManager
     - ScenarioLoader (will be created automatically if missing)
   - Ensure the scene has proper lighting and camera setup

3. **Add scene to build settings**:
   - File → Build Settings
   - Click "Add Open Scenes" or drag `D001_Scene.unity` into the Scenes list
   - This ensures the scene can be loaded at runtime

## Scenario Configuration

When creating a scenario, set:
- `sceneName`: The name of the scene to load (e.g., "D001_Scene")
- `sceneAsset`: (Optional) Reference to the scene asset in the project

If both are set, `sceneAsset` takes priority.

## Scene Loading Flow

1. ScenarioLoader checks if scenario has a scene specified
2. If scene name differs from current scene, loads the new scene asynchronously
3. After scene loads, continues with scenario setup:
   - Spawns cubes
   - Places markers
   - Sets player position
   - Starts wave (if configured)
4. Fires `OnScenarioLoaded` event for ScenarioRunner

## Benefits

- **Isolation**: Each scenario can have its own scene setup
- **Flexibility**: Scenarios aren't tied to stage progression
- **Testing**: Easier to test scenarios independently
- **Debugging**: Can customize scene for specific scenario needs

## Migration from Stage-Based

Old scenarios using `stage` field will continue to work (backwards compatible). New scenarios should use `sceneName` instead.
