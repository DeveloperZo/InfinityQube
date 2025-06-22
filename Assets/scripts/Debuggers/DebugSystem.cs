using UnityEngine;
using System.Collections.Generic;

public class DebugSystem : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Debug Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private DebugDisplayMode displayMode = DebugDisplayMode.Tabbed;

    [Header("Window Settings")]
    [SerializeField] private Vector2 windowSize = new Vector2(600, 1200);
    [SerializeField] private Vector2 windowPosition = new Vector2(10, 10);
    #endregion

    #region Runtime State
    private bool isVisible = false;
    private Rect windowRect;
    private Vector2 scrollPosition;
    private int selectedTab = 0;

    // Debug Panels
    private List<IDebugPanel> debugPanels = new List<IDebugPanel>();
    private Dictionary<string, IDebugPanel> panelsByName = new Dictionary<string, IDebugPanel>();

    // Window management for windowed mode
    private Dictionary<IDebugPanel, Rect> windowRects = new Dictionary<IDebugPanel, Rect>();
    private Dictionary<IDebugPanel, Vector2> scrollPositions = new Dictionary<IDebugPanel, Vector2>();
    #endregion

    public enum DebugDisplayMode
    {
        Tabbed,      // Tabs at top, one panel visible
        Stacked,     // All panels stacked vertically
        Windowed     // Separate windows for each panel
    }

    #region Unity Lifecycle
    private void Awake()
    {
        // Debug system should work in both editor and builds for testing
        Debug.Log("DebugSystem: Awake() called");
        
        try
        {
            InitializeDebugPanels();
            windowRect = new Rect(windowPosition.x, windowPosition.y, windowSize.x, windowSize.y);
            Debug.Log($"DebugSystem: Initialized with {debugPanels.Count} panels");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DebugSystem: Failed to initialize - {e.Message}\n{e.StackTrace}");
        }
    }

    private void Start()
    {
        isVisible = showOnStart;
        
        // If starting visible, show all panels
        if (isVisible)
        {
            foreach (var panel in debugPanels)
            {
                panel.OnShow();
            }
        }
        
        // Show first panel in tabbed mode
        if (debugPanels.Count > 0 && displayMode == DebugDisplayMode.Tabbed)
        {
            debugPanels[0].OnShow();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"DebugSystem: F12 pressed, current visible: {isVisible}");
            ToggleDebugSystem();
        }

        // Update all panels
        if (isVisible)
        {
            foreach (var panel in debugPanels)
            {
                try
                {
                    panel.Update();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"DebugSystem: Error updating panel {panel.PanelName}: {e.Message}");
                }
            }
        }
    }

    private void OnGUI()
    {
        // Always show a simple indicator if debug system exists
        GUI.Label(new Rect(10, 10, 200, 20), $"Debug System Active (F12): {isVisible}");
        
        if (!isVisible) return;

        try
        {
            switch (displayMode)
            {
                case DebugDisplayMode.Tabbed:
                    DrawTabbedInterface();
                    break;
                case DebugDisplayMode.Stacked:
                    DrawStackedInterface();
                    break;
                case DebugDisplayMode.Windowed:
                    DrawWindowedInterface();
                    break;
            }
        }
        catch (System.Exception e)
        {
            // Fallback simple error display
            Rect errorRect = new Rect(50, 50, 500, 200);
            GUI.Box(errorRect, "");
            GUI.Label(new Rect(60, 60, 480, 180), $"DebugSystem Error:\n{e.Message}\n\nPanels: {debugPanels?.Count ?? 0}\nStack: {e.StackTrace}");
            Debug.LogError($"DebugSystem: OnGUI error - {e.Message}\n{e.StackTrace}");
        }
    }
    #endregion

    #region Initialization
    private void InitializeDebugPanels()
    {
        Debug.Log("DebugSystem: InitializeDebugPanels() called");
        
        try
        {
            // Initialize theme system
            DebugUIHelpers.InitializeTheme();
            Debug.Log("DebugSystem: Theme initialized");
            
            // Register debug panels in gameplay-focused order:
            // 1. Overall game state and progression
            RegisterPanel(new GameplayDebugPanel());  // StageManager + overall game state
            
            // 2. Wave system - core gameplay loop
            RegisterPanel(new WaveDebugPanel());      // WaveManager - wave control, cube spawning
            
            // 3. Grid and environment
            RegisterPanel(new GridDebugPanel());      // GridManager - grid state, tiles, markers
            
            // 4. Player and actions coordination
            RegisterPanel(new PlayerActionDebugPanel()); // PlayerActionManager + PlayerManager
            
            // 5. Cube behavior and interactions
            RegisterPanel(new CubeDebugPanel());      // CubeManager - cube behavior, face painting
            
            // 6. Cross-system testing and scenarios
            RegisterPanel(new TestingDebugPanel());   // Integration testing, scenarios
            
            Debug.Log($"DebugSystem: Successfully registered {debugPanels.Count} panels");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DebugSystem: Error in InitializeDebugPanels - {e.Message}\n{e.StackTrace}");
        }
    }

    private void RegisterPanel(IDebugPanel panel)
    {
        try
        {
            Debug.Log($"DebugSystem: Registering panel {panel.PanelName}");
            panel.Initialize();
            debugPanels.Add(panel);
            panelsByName[panel.PanelName] = panel;

            // Initialize window rect for windowed mode
            Vector2 pos = new Vector2(50 + (debugPanels.Count * 30), 50 + (debugPanels.Count * 30));
            windowRects[panel] = new Rect(pos.x, pos.y, 400, 400);
            scrollPositions[panel] = Vector2.zero;
            
            Debug.Log($"DebugSystem: Successfully registered panel {panel.PanelName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DebugSystem: Failed to register panel {panel?.PanelName ?? "unknown"}: {e.Message}\n{e.StackTrace}");
        }
    }

    public void ToggleDebugSystem()
    {
        bool wasVisible = isVisible;
        isVisible = !isVisible;
        
        // Manage panel visibility states
        if (isVisible && !wasVisible)
        {
            // Show all panels when opening debug system
            foreach (var panel in debugPanels)
            {
                panel.OnShow();
            }
        }
        else if (!isVisible && wasVisible)
        {
            // Hide all panels when closing debug system
            foreach (var panel in debugPanels)
            {
                panel.OnHide();
            }
        }
        
        Debug.Log($"Debug System: {(isVisible ? "Opened" : "Closed")}");
    }
    #endregion

    #region Drawing Methods
    private void DrawTabbedInterface()
    {
        windowRect = GUILayout.Window(999, windowRect, DrawTabbedWindow, "Debug System - F12 to toggle");
    }

    private void DrawTabbedWindow(int windowID)
    {
        try
        {
            // Tab buttons
            GUILayout.BeginHorizontal();
            for (int i = 0; i < debugPanels.Count; i++)
            {
                bool isSelected = selectedTab == i;
                
                // Simplified tab button without theme initially
                GUI.backgroundColor = isSelected ? Color.yellow : Color.white;
                if (GUILayout.Button(debugPanels[i].PanelName, GUILayout.Height(25)))
                {
                    // Hide previously selected panel
                    if (selectedTab >= 0 && selectedTab < debugPanels.Count)
                    {
                        debugPanels[selectedTab].OnHide();
                    }
                    
                    selectedTab = i;
                    
                    // Show newly selected panel
                    debugPanels[selectedTab].OnShow();
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Selected panel content
            if (selectedTab >= 0 && selectedTab < debugPanels.Count)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(windowSize.y - 100));
                try
                {
                    debugPanels[selectedTab].DrawPanel();
                }
                catch (System.Exception e)
                {
                    GUILayout.Label($"Error in panel {debugPanels[selectedTab].PanelName}: {e.Message}");
                    Debug.LogError($"Panel {debugPanels[selectedTab].PanelName} error: {e.Message}");
                }
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label($"Invalid tab selection: {selectedTab}/{debugPanels.Count}");
            }

            // Bottom controls (simplified)
            GUILayout.BeginHorizontal();
            
            // Display mode switcher
            if (GUILayout.Button("Tabbed", GUILayout.Width(60)))
                displayMode = DebugDisplayMode.Tabbed;
            if (GUILayout.Button("Stacked", GUILayout.Width(60)))
                displayMode = DebugDisplayMode.Stacked;
            if (GUILayout.Button("Windows", GUILayout.Width(60)))
                displayMode = DebugDisplayMode.Windowed;
                
            GUILayout.FlexibleSpace();
            
            // Simple theme toggle
            if (GUILayout.Button("Toggle Theme", GUILayout.Width(100)))
            {
                DebugTheme.ToggleTheme();
            }
            
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }
        catch (System.Exception e)
        {
            GUILayout.Label($"DrawTabbedWindow Error: {e.Message}");
            Debug.LogError($"DrawTabbedWindow error: {e.Message}\n{e.StackTrace}");
        }
    }

    private void DrawStackedInterface()
    {
        Vector2 stackedSize = new Vector2(windowSize.x, Mathf.Min(Screen.height - 100, windowSize.y * 1.5f));
        Rect stackedRect = new Rect(windowPosition.x, windowPosition.y, stackedSize.x, stackedSize.y);
        stackedRect = GUILayout.Window(999, stackedRect, DrawStackedWindow, "Debug System - F12 to toggle");
    }

    private void DrawStackedWindow(int windowID)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        foreach (var panel in debugPanels)
        {
            DebugUIHelpers.DrawSection(panel.PanelName, () => {
                panel.DrawPanel();
            });
            DebugUIHelpers.Space(5);
        }

        GUILayout.EndScrollView();

        // Bottom controls with theme toggle
        GUILayout.BeginHorizontal();
        
        // Display mode switcher
        if (GUILayout.Button("Tabbed", DebugTheme.GetSmallButtonStyle(), GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Tabbed;
        if (GUILayout.Button("Stacked", DebugTheme.GetSmallButtonStyle(), GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Stacked;
        if (GUILayout.Button("Windows", DebugTheme.GetSmallButtonStyle(), GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Windowed;
            
        GUILayout.FlexibleSpace();
        
        // Theme toggle
        DebugUIHelpers.DrawThemeToggle();
        
        GUILayout.EndHorizontal();

        GUI.DragWindow();
    }

    private void DrawWindowedInterface()
    {
        for (int i = 0; i < debugPanels.Count; i++)
        {
            var panel = debugPanels[i];

            windowRects[panel] = GUILayout.Window(1000 + i, windowRects[panel], (windowID) => {
                scrollPositions[panel] = GUILayout.BeginScrollView(scrollPositions[panel]);
                panel.DrawPanel();
                GUILayout.EndScrollView();
                GUI.DragWindow();
            }, $"{panel.PanelName} - F12 to toggle");
        }

        // Control panel with theme integration
        Rect controlRect = new Rect(10, Screen.height - 120, 250, 100);
        GUILayout.Window(1100, controlRect, (windowID) => {
            GUILayout.Label("Debug System Control", DebugTheme.GetHeaderStyle());
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tabbed", DebugTheme.GetSmallButtonStyle()))
                displayMode = DebugDisplayMode.Tabbed;
            if (GUILayout.Button("Stacked", DebugTheme.GetSmallButtonStyle()))
                displayMode = DebugDisplayMode.Stacked;
            if (GUILayout.Button("Windows", DebugTheme.GetSmallButtonStyle()))
                displayMode = DebugDisplayMode.Windowed;
            GUILayout.EndHorizontal();
            
            DebugUIHelpers.DrawThemeToggle();
        }, "Display Mode");
    }
    #endregion

    #region Public Interface
    public T GetPanel<T>() where T : class, IDebugPanel
    {
        return debugPanels.Find(p => p is T) as T;
    }

    public IDebugPanel GetPanel(string name)
    {
        return panelsByName.ContainsKey(name) ? panelsByName[name] : null;
    }
    #endregion
}