# VSCode Unity Integration

Simple setup for Unity development with VSCode debugging and build validation.

## What's Included

The `.vscode/` folder contains:
- **launch.json** - Unity debugging configurations
- **tasks.json** - Build validation shortcuts
- **settings.json** - Unity-optimized VSCode settings
- **extensions.json** - Recommended extensions
- **keybindings.json** - Quick shortcuts

## How to Use

### Debugging
1. Open Unity Editor with the project
2. In VSCode, press `F5` and select "Attach to Unity Editor"
3. Set breakpoints in C# scripts
4. Play in Unity - breakpoints will trigger in VSCode

### Build Validation
- `Ctrl+Shift+T` - Run full validation pipeline
- `Ctrl+Shift+B` - Quick build check only

### Essential Extensions
Install these for the best experience:
- C# Dev Kit
- Unity Tools for Visual Studio Code

## How It Works

**Debugging**: VSCode connects to Unity's debugging port, allowing you to debug scripts directly while Unity is running.

**Build Validation**: Custom tasks run Unity in batch mode to validate builds without opening the full editor.

**Settings**: Configured to hide Unity temp files and optimize C# IntelliSense for Unity projects.

That's it! The integration provides professional debugging and automated testing without complexity.