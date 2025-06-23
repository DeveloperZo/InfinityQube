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


    #endregion

    public enum DebugDisplayMode
    {
        Tabbed      // Tabs at top, one panel visible - only mode supported for now
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
        
        // Ensure we have a valid tab selection
        if (debugPanels.Count > 0)
        {
            selectedTab = 0; // Start with first panel
        }
        
        // If starting visible, show all panels
        if (isVisible)
        {
            foreach (var panel in debugPanels)
            {
                panel.OnShow();
                
                // Also mark first panel as dirty for immediate display
                if (panel is DebugPanelBase basePanel)
                {
                    basePanel.MarkDirty();
                }
            }
        }
        
        // Ensure first panel is dirty even if not starting visible
        if (debugPanels.Count > 0 && debugPanels[0] is DebugPanelBase firstPanel)
        {
            firstPanel.MarkDirty();
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
        // Simple debug system - no complex theme initialization needed
        
        // Always show a simple indicator if debug system exists
        GUI.Label(new Rect(10, 10, 200, 20), $"Debug System Active (F12): {isVisible}");
        
        if (!isVisible) return;

        try
        {
            DrawTabbedInterface();
        }
        catch (System.Exception e)
        {
            // Simple error display with basic Unity styling
            Rect errorRect = new Rect(50, 50, 500, 200);
            GUI.Box(errorRect, "");
            GUI.Label(new Rect(60, 60, 480, 180), $"DebugSystem Error:\n{e.Message}\n\nPanels: {debugPanels?.Count ?? 0}");
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
            // Simple debug system - no theme initialization needed
            Debug.Log("DebugSystem: Using basic Unity GUI styling");
            
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

            // Basic registration only - no windowed mode support
            
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
            
            // Force all panels to be dirty for immediate content display
            foreach (var panel in debugPanels)
            {
                if (panel is DebugPanelBase basePanel)
                {
                    basePanel.MarkDirty();
                }
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
                    
                    // Show newly selected panel and mark it dirty
                    debugPanels[selectedTab].OnShow();
                    if (debugPanels[selectedTab] is DebugPanelBase basePanel)
                    {
                        basePanel.MarkDirty();
                    }
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
                    var currentPanel = debugPanels[selectedTab];
                    Debug.Log($"Drawing panel: {currentPanel.PanelName}, IsVisible: {currentPanel.IsVisible}");
                    
                    // Add basic content test
                    GUILayout.Label($"Panel: {currentPanel.PanelName}");
                    GUILayout.Label($"Group: {currentPanel.Group}");
                    GUILayout.Label($"Visible: {currentPanel.IsVisible}");
                    GUILayout.Space(10);
                    
                    currentPanel.DrawPanel();
                }
                catch (System.Exception e)
                {
                    GUILayout.Label($"Error in panel {debugPanels[selectedTab].PanelName}: {e.Message}");
                    GUILayout.Label($"Stack: {e.StackTrace}");
                    Debug.LogError($"Panel {debugPanels[selectedTab].PanelName} error: {e.Message}\n{e.StackTrace}");
                }
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label($"Invalid tab selection: {selectedTab}/{debugPanels.Count}");
            }

            // Simple bottom info
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Mode: Tabbed | Panels: {debugPanels.Count}");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }
        catch (System.Exception e)
        {
            GUILayout.Label($"DrawTabbedWindow Error: {e.Message}");
            Debug.LogError($"DrawTabbedWindow error: {e.Message}\n{e.StackTrace}");
        }
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