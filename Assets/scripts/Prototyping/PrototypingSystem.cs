using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Main prototyping tools system using IMGUI.
/// Toggle with F12. Window is resizable by dragging edges.
/// </summary>
public class PrototypingSystem : MonoBehaviour
{
    #region Singleton
    public static PrototypingSystem Instance { get; private set; }
    #endregion
    
    #region Inspector Configuration
    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;
    [SerializeField] private bool showOnStart = false;
    #endregion
    
    #region Runtime State
    private bool isVisible = false;
    private List<IPrototypingPanel> panels = new List<IPrototypingPanel>();
    private int activePanelIndex = 0;
    
    // Window sizing - start at max size for full visibility
    private Rect windowRect = new Rect(10, 10, 480, 800);
    private const float MIN_WIDTH = 400f;
    private const float MAX_WIDTH = 700f;
    private const float MIN_HEIGHT = 500f;
    private const float MAX_HEIGHT = 1000f;
    private const float RESIZE_HANDLE = 18f;
    private bool isResizing = false;
    private Vector2 scrollPosition;
    
    // Styles
    private GUIStyle headerStyle;
    private GUIStyle tabActiveStyle;
    private GUIStyle tabInactiveStyle;
    private GUIStyle sectionStyle;
    private bool stylesInitialized = false;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        InitializePanels();
        isVisible = showOnStart;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            Debug.Log($"[PrototypingSystem] {(isVisible ? "Opened" : "Closed")}");
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion
    
    #region Initialization
    private void InitializePanels()
    {
        panels.Clear();
        
        // Quick Debug Panel - First for fast access
        panels.Add(new QuickDebugPanel());
        
        // Core panels
        panels.Add(new WavePrototyper());
        panels.Add(new CollisionPanel());
        panels.Add(new GridDesigner());
        panels.Add(new PlayerPanel());
        panels.Add(new StagePanel());
        panels.Add(new SystemPanel());
        
        // Console Panel - View logs in-game
        panels.Add(new ConsolePanel());
        
        foreach (var panel in panels)
        {
            panel.Initialize();
        }
        
        panels = panels.OrderBy(p => p.Priority).ToList();
        Debug.Log($"[PrototypingSystem] Initialized {panels.Count} panels");
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        headerStyle.normal.textColor = Color.white;
        
        tabActiveStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold
        };
        tabActiveStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.4f, 0.5f));
        
        tabInactiveStyle = new GUIStyle(GUI.skin.button);
        
        sectionStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(8, 8, 8, 8)
        };
        
        stylesInitialized = true;
    }
    #endregion
    
    #region OnGUI
    private void OnGUI()
    {
        if (!isVisible) return;
        
        InitStyles();
        
        // Handle resize before window draw
        HandleResize();
        
        // Make window draggable
        windowRect = GUI.Window(9999, windowRect, DrawWindow, "");
        
        // Keep window on screen
        windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - 100);
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - 100);
    }
    
    private void HandleResize()
    {
        // Resize handle area (bottom-right corner)
        Rect resizeRect = new Rect(
            windowRect.x + windowRect.width - RESIZE_HANDLE,
            windowRect.y + windowRect.height - RESIZE_HANDLE,
            RESIZE_HANDLE, RESIZE_HANDLE
        );
        
        // Change cursor when hovering resize area
        if (resizeRect.Contains(Event.current.mousePosition))
        {
            EditorCursorHint();
        }
        
        // Handle resize drag
        if (Event.current.type == EventType.MouseDown && resizeRect.Contains(Event.current.mousePosition))
        {
            isResizing = true;
            Event.current.Use();
        }
        
        if (Event.current.type == EventType.MouseUp)
        {
            isResizing = false;
        }
        
        if (isResizing && Event.current.type == EventType.MouseDrag)
        {
            windowRect.width = Mathf.Clamp(
                Event.current.mousePosition.x - windowRect.x + RESIZE_HANDLE / 2,
                MIN_WIDTH, MAX_WIDTH
            );
            windowRect.height = Mathf.Clamp(
                Event.current.mousePosition.y - windowRect.y + RESIZE_HANDLE / 2,
                MIN_HEIGHT, MAX_HEIGHT
            );
            Event.current.Use();
        }
    }
    
    private void EditorCursorHint()
    {
        // Visual hint for resize - cursor change not available in runtime IMGUI
    }
    
    private void DrawWindow(int windowID)
    {
        // Header
        GUILayout.BeginHorizontal();
        GUILayout.Label("PROTOTYPING", headerStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(22)))
        {
            isVisible = false;
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(3);
        
        // Tab Bar (simplified - text labels)
        DrawTabBar();
        
        GUILayout.Space(3);
        
        // Panel Content
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        
        if (activePanelIndex >= 0 && activePanelIndex < panels.Count)
        {
            var panel = panels[activePanelIndex];
            panel.DrawGUI();
        }
        
        GUILayout.EndScrollView();
        
        // Status Bar with resize hint
        GUILayout.BeginHorizontal();
        GUILayout.Label("F12 toggle | Drag corner to resize", GUILayout.Height(18));
        GUILayout.FlexibleSpace();
        // Resize grip visual
        GUILayout.Label("◢", GUILayout.Width(15), GUILayout.Height(18));
        GUILayout.EndHorizontal();
        
        // Make header draggable
        GUI.DragWindow(new Rect(0, 0, 10000, 25));
    }
    
    private void DrawTabBar()
    {
        // Two-row tab layout with uniform sizing
        GUILayout.BeginVertical();
        
        // First row of tabs
        GUILayout.BeginHorizontal();
        int halfCount = (panels.Count + 1) / 2;
        for (int i = 0; i < halfCount && i < panels.Count; i++)
        {
            DrawTab(i);
        }
        GUILayout.EndHorizontal();
        
        // Second row of tabs (if needed)
        if (panels.Count > halfCount)
        {
            GUILayout.BeginHorizontal();
            for (int i = halfCount; i < panels.Count; i++)
            {
                DrawTab(i);
            }
            GUILayout.EndHorizontal();
        }
        
        GUILayout.EndVertical();
    }
    
    private void DrawTab(int index)
    {
        var panel = panels[index];
        bool isActive = index == activePanelIndex;
        
        GUI.backgroundColor = isActive ? new Color(0.3f, 0.5f, 0.7f) : Color.white;
        var style = isActive ? tabActiveStyle : tabInactiveStyle;
        
        // Fixed width tabs for uniform appearance
        float tabWidth = (windowRect.width - 20) / 3f; // 3 tabs per row
        if (GUILayout.Button(panel.PanelName, style, GUILayout.Width(tabWidth), GUILayout.Height(26)))
        {
            activePanelIndex = index;
        }
        
        GUI.backgroundColor = Color.white;
    }
    #endregion
    
    #region Public API
    public void RefreshCurrentPanel()
    {
        // IMGUI refreshes automatically
    }
    
    public T GetPanel<T>() where T : class, IPrototypingPanel
    {
        return panels.OfType<T>().FirstOrDefault();
    }
    #endregion
    
    #region Utility
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    #endregion
}
