# VS Code Integration for Unity Testing

> **Purpose**: VS Code configuration files for integrating Unity validation pipeline  
> **Location**: Copy these files to your project root's `.vscode/` directory  

---

## **tasks.json Configuration**

✅ **Already Created**: `.vscode/tasks.json` is now in your project root.

The configuration includes:

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Unity: Validate Implementation",
            "type": "shell",
            "command": "${workspaceFolder}/Assets/scripts/validate_implementation.bat",
            "group": "test",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "new"
            },
            "problemMatcher": [],
            "detail": "Run Unity validation pipeline after implementation",
            "options": {
                "cwd": "${workspaceFolder}"
            }
        },
        {
            "label": "Unity: Quick Build Check",
            "type": "shell",
            "command": "C:/Program Files/Unity/Hub/Editor/2022.3.47f1/Editor/Unity.exe",
            "args": [
                "-batchmode",
                "-quit",
                "-projectPath",
                "${workspaceFolder}",
                "-executeMethod",
                "InfinityQube.Testing.BuildValidationSystem.ValidateBuildOnly",
                "-logFile",
                "quick_build_log.txt"
            ],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "new"
            },
            "problemMatcher": [],
            "detail": "Quick compilation check without full validation"
        },
        {
            "label": "Unity: Open Validation Results",
            "type": "shell",
            "command": "code",
            "args": [
                "${workspaceFolder}/Assets/Docs/Technical Doc/ValidationResults.md"
            ],
            "group": "test",
            "presentation": {
                "echo": false,
                "reveal": "silent",
                "focus": false,
                "panel": "shared"
            },
            "detail": "Open current validation results in VS Code"
        },
        {
            "label": "Unity: Open Handoff Report",
            "type": "shell",
            "command": "code",
            "args": [
                "${workspaceFolder}/Assets/Docs/Technical Doc/HandoffReport.md"
            ],
            "group": "test",
            "presentation": {
                "echo": false,
                "reveal": "silent",
                "focus": false,
                "panel": "shared"
            },
            "detail": "Open current handoff report in VS Code"
        }
    ]
}
```

---

## **keybindings.json Configuration**

✅ **Already Created**: `.vscode/keybindings.json` is now in your project root.

The keyboard shortcuts are:

```json
[
    {
        "key": "ctrl+shift+t",
        "command": "workbench.action.tasks.runTask",
        "args": "Unity: Validate Implementation",
        "when": "!terminalFocus"
    },
    {
        "key": "ctrl+shift+b",
        "command": "workbench.action.tasks.runTask", 
        "args": "Unity: Quick Build Check",
        "when": "!terminalFocus"
    }
]
```

---

## **settings.json Configuration**

Add to `.vscode/settings.json` in your project root:

```json
{
    "files.associations": {
        "*.cs": "csharp"
    },
    "omnisharp.enableRoslynAnalyzers": true,
    "omnisharp.useModernNet": true,
    "files.exclude": {
        "**/.git": true,
        "**/.DS_Store": true,
        "**/Thumbs.db": true,
        "**/validation_log.txt": false,
        "**/quick_build_log.txt": false
    },
    "search.exclude": {
        "**/node_modules": true,
        "**/bower_components": true,
        "**/*.code-search": true,
        "**/Library": true,
        "**/Temp": true,
        "**/Logs": true
    },
    "terminal.integrated.defaultProfile.windows": "Command Prompt"
}
```

---

## **Usage Instructions**

### **After Implementation Complete**

1. **Save all files** in VS Code
2. **Open Command Palette** (`Ctrl+Shift+P`)
3. **Type**: `Tasks: Run Task`
4. **Select**: `Unity: Validate Implementation`
5. **Wait for completion** and review results

### **Quick Validation During Development**

1. **Use keyboard shortcut**: `Ctrl+Shift+B`
2. **Or run task**: `Unity: Quick Build Check`
3. **Check console output** for immediate feedback

### **View Results**

1. **Run task**: `Unity: Open Validation Results`
2. **Or run task**: `Unity: Open Handoff Report`
3. **Or navigate manually** to `Assets/Docs/Technical Doc/`

---

## **Workflow Integration**

### **Recommended Development Flow**

```
1. Complete implementation in VS Code
   ↓
2. Ctrl+Shift+B (Quick Build Check)
   ↓
3. Fix any immediate compilation issues
   ↓
4. Ctrl+Shift+T (Full Validation)
   ↓
5. Review ValidationResults.md
   ↓
6. If passed: Continue to next strategic cycle
7. If failed: Address issues and re-validate
```

### **For AI Agents**

```yaml
post_implementation_workflow:
  1. Save all modified files
  2. Run task: "Unity: Validate Implementation"
  3. Parse ValidationResults.md for score
  4. If score >= 80:
     - Generate handoff report per protocols
     - Mark task complete
     - Prepare context for strategic planning
  5. If score < 80:
     - Create fix tasks based on specific failures
     - Return to implementation phase
```

---

## **Troubleshooting**

### **Common Issues**

**Unity Path Not Found**:
- Edit `validate_implementation.bat`
- Update `UNITY_PATH` to match your Unity installation
- Common paths:
  - `C:\Program Files\Unity\Hub\Editor\[VERSION]\Editor\Unity.exe`
  - `C:\Program Files\Unity\Editor\Unity.exe`

**Permission Errors**:
- Run VS Code as Administrator if needed
- Check that batch file has execute permissions

**Validation Fails to Start**:
- Ensure Unity project compiles in Unity Editor first
- Check that `BuildValidationSystem.cs` exists in `Assets/scripts/Testing/`
- Verify all namespace references are correct

**Results Not Generated**:
- Check `validation_log.txt` for Unity console output
- Ensure `Assets/Docs/Technical Doc/` directory exists
- Verify write permissions to project directory

---

**Last Updated**: July 4, 2025  
**Document Version**: 1.0 - Initial VS Code integration  
**Usage**: Copy configurations to `.vscode/` directory in project root