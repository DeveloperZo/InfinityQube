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
    [SerializeField] private Vector2 windowSize = new Vector2(400, 600);
    [SerializeField] private Vector2 windowPosition = new Vector2(10, 10);
    #endregion

    #region Runtime State
    private bool isVisible = false;
    private Rect windowRect;
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private int selectedGroup = 0;

    // Debug Panels
    private List<IDebugPanel> debugPanels = new List<IDebugPanel>();
    private Dictionary<string, IDebugPanel> panelsByName = new Dictionary<string, IDebugPanel>();
    private Dictionary<DebugPanelGroup, List<IDebugPanel>> panelsByGroup = new Dictionary<DebugPanelGroup, List<IDebugPanel>>();
    private List<DebugPanelGroup> groupOrder = new List<DebugPanelGroup>();

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
#if UNITY_EDITOR
        InitializeDebugPanels();
        windowRect = new Rect(windowPosition.x, windowPosition.y, windowSize.x, windowSize.y);
#endif
    }

    private void Start()
    {
        isVisible = showOnStart;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDebugSystem();
        }

        // Update all panels
        if (isVisible)
        {
            foreach (var panel in debugPanels)
            {
                panel.Update();
            }
        }
    }

    private void OnGUI()
    {
        if (!isVisible) return;

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
    #endregion

    #region Initialization
    private void InitializeDebugPanels()
    {

        // Register all debug panels
        RegisterPanel(new GameControlPanel());
        RegisterPanel(new StageDebugPanel());
        RegisterPanel(new WaveDebugPanel());
        RegisterPanel(new PlayerDebugPanel());
        RegisterPanel(new PlayerActionDebugPanel());
        RegisterPanel(new TileActionPanel());
        RegisterPanel(new SystemDebugPanel());
    }

    private void RegisterPanel(IDebugPanel panel)
    {
        panel.Initialize();
        debugPanels.Add(panel);
        panelsByName[panel.PanelName] = panel;

        if (!panelsByGroup.TryGetValue(panel.PanelGroup, out var list))
        {
            list = new List<IDebugPanel>();
            panelsByGroup[panel.PanelGroup] = list;
            groupOrder.Add(panel.PanelGroup);
        }
        list.Add(panel);

        // Initialize window rect for windowed mode
        Vector2 pos = new Vector2(50 + (debugPanels.Count * 30), 50 + (debugPanels.Count * 30));
        windowRects[panel] = new Rect(pos.x, pos.y, 350, 400);
        scrollPositions[panel] = Vector2.zero;
    }

    public void ToggleDebugSystem()
    {
        isVisible = !isVisible;
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
        // Group buttons
        GUILayout.BeginHorizontal();
        for (int i = 0; i < groupOrder.Count; i++)
        {
            bool gSelected = selectedGroup == i;
            GUI.backgroundColor = gSelected ? Color.cyan : Color.white;

            if (GUILayout.Button(groupOrder[i].ToString(), GUILayout.Height(25)))
            {
                selectedGroup = i;
                selectedTab = 0;
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Tab buttons
        GUILayout.BeginHorizontal();
        var groupPanels = panelsByGroup[groupOrder[selectedGroup]];
        for (int i = 0; i < groupPanels.Count; i++)
        {
            bool isSelected = selectedTab == i;
            GUI.backgroundColor = isSelected ? Color.yellow : Color.white;

            if (GUILayout.Button(groupPanels[i].PanelName, GUILayout.Height(25)))
            {
                selectedTab = i;
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Selected panel content
        if (groupPanels.Count > 0)
        {
            selectedTab = Mathf.Clamp(selectedTab, 0, groupPanels.Count - 1);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(windowSize.y - 100));
            try
            {
                groupPanels[selectedTab].DrawPanel();
            }
            catch (System.Exception e)
            {
                GUILayout.Label($"Error in panel: {e.Message}");
            }
            GUILayout.EndScrollView();
        }

        // Display mode switcher
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Tabbed", GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Tabbed;
        if (GUILayout.Button("Stacked", GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Stacked;
        if (GUILayout.Button("Windows", GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Windowed;
        GUILayout.EndHorizontal();

        GUI.DragWindow();
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

        foreach (var group in groupOrder)
        {
            GUILayout.Label(group.ToString(), GUI.skin.box);
            foreach (var panel in panelsByGroup[group])
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(panel.PanelName, GUI.skin.box);
                panel.DrawPanel();
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        GUILayout.EndScrollView();

        // Display mode switcher
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Tabbed", GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Tabbed;
        if (GUILayout.Button("Stacked", GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Stacked;
        if (GUILayout.Button("Windows", GUILayout.Width(60)))
            displayMode = DebugDisplayMode.Windowed;
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

        // Control panel
        Rect controlRect = new Rect(10, Screen.height - 100, 220, 80);
        GUILayout.Window(1100, controlRect, (windowID) => {
            GUILayout.Label("Debug System Control");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Tabbed"))
                displayMode = DebugDisplayMode.Tabbed;
            if (GUILayout.Button("Stacked"))
                displayMode = DebugDisplayMode.Stacked;
            if (GUILayout.Button("Windows"))
                displayMode = DebugDisplayMode.Windowed;
            GUILayout.EndHorizontal();
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