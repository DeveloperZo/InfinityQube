using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Main prototyping tools system using IMGUI.
/// Toggle with F12.
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
    
    // Window
    private Rect windowRect = new Rect(10, 10, 420, 600);
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
        
        panels.Add(new WavePrototyper());
        panels.Add(new GridDesigner());
        panels.Add(new PlayerPanel());
        panels.Add(new StagePanel());
        panels.Add(new SystemPanel());
        
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
        
        // Make window draggable
        windowRect = GUI.Window(9999, windowRect, DrawWindow, "");
        
        // Keep window on screen
        windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - 100);
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - 100);
    }
    
    private void DrawWindow(int windowID)
    {
        // Header
        GUILayout.BeginHorizontal();
        GUILayout.Label("🔧 PROTOTYPING TOOLS", headerStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(25)))
        {
            isVisible = false;
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Quick Actions Bar
        DrawQuickActions();
        
        GUILayout.Space(5);
        
        // Tab Bar
        DrawTabBar();
        
        GUILayout.Space(5);
        
        // Panel Content
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        
        if (activePanelIndex >= 0 && activePanelIndex < panels.Count)
        {
            var panel = panels[activePanelIndex];
            panel.DrawGUI();
        }
        
        GUILayout.EndScrollView();
        
        // Status Bar
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Panel: {panels[activePanelIndex].PanelName}", GUILayout.Height(20));
        GUILayout.FlexibleSpace();
        GUILayout.Label("F12 to toggle", GUILayout.Height(20));
        GUILayout.EndHorizontal();
        
        // Make draggable
        GUI.DragWindow(new Rect(0, 0, 10000, 30));
    }
    
    private void DrawQuickActions()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.BeginHorizontal();
        
        var allActions = new List<QuickAction>();
        foreach (var panel in panels)
        {
            allActions.AddRange(panel.GetQuickActions());
        }
        
        var topActions = allActions.OrderBy(a => a.Priority).Take(10).ToList();
        
        foreach (var action in topActions)
        {
            GUI.enabled = action.IsEnabled?.Invoke() ?? true;
            
            var style = GUI.skin.button;
            if (action.IsHighlighted?.Invoke() ?? false)
            {
                GUI.backgroundColor = Color.green;
            }
            
            string label = string.IsNullOrEmpty(action.Icon) ? action.Label : $"{action.Icon}";
            if (GUILayout.Button(new GUIContent(label, action.Tooltip), GUILayout.Height(25), GUILayout.MinWidth(30)))
            {
                action.OnClick?.Invoke();
            }
            
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }
        
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    
    private void DrawTabBar()
    {
        GUILayout.BeginHorizontal();
        
        for (int i = 0; i < panels.Count; i++)
        {
            var panel = panels[i];
            var style = (i == activePanelIndex) ? tabActiveStyle : tabInactiveStyle;
            
            if (GUILayout.Button($"{panel.PanelIcon} {panel.PanelName}", style, GUILayout.Height(28)))
            {
                activePanelIndex = i;
            }
        }
        
        GUILayout.EndHorizontal();
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
