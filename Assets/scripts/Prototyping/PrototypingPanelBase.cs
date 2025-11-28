using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Base class for prototyping panels using IMGUI.
/// </summary>
public abstract class PrototypingPanelBase : IPrototypingPanel
{
    #region Abstract Properties
    public abstract string PanelName { get; }
    public abstract string PanelIcon { get; }
    public abstract PrototypingCategory Category { get; }
    public virtual int Priority => 100;
    #endregion
    
    #region State
    public bool IsVisible { get; protected set; }
    protected bool isInitialized = false;
    #endregion
    
    #region Manager References
    protected WaveManager waveManager;
    protected GridManager gridManager;
    protected PlayerManager playerManager;
    protected StageManager stageManager;
    #endregion
    
    #region Lifecycle
    public virtual void Initialize()
    {
        if (isInitialized) return;
        CacheManagerReferences();
        isInitialized = true;
    }
    
    protected virtual void CacheManagerReferences()
    {
        gridManager = GridManager.Instance;
        waveManager = Object.FindFirstObjectByType<WaveManager>();
        playerManager = Object.FindFirstObjectByType<PlayerManager>();
        stageManager = Object.FindFirstObjectByType<StageManager>();
    }
    
    public virtual void OnShow() { IsVisible = true; }
    public virtual void OnHide() { IsVisible = false; }
    public virtual void Update() { }
    
    public virtual List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>();
    }
    #endregion
    
    #region IMGUI Drawing
    public abstract void DrawGUI();
    
    protected void DrawSection(string title, System.Action content)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(title, GUI.skin.box);
        content?.Invoke();
        GUILayout.EndVertical();
        GUILayout.Space(5);
    }
    
    protected bool DrawToggleSection(string title, bool isExpanded)
    {
        GUILayout.BeginHorizontal();
        string prefix = isExpanded ? "▼" : "▶";
        if (GUILayout.Button($"{prefix} {title}", GUILayout.Height(22)))
        {
            isExpanded = !isExpanded;
        }
        GUILayout.EndHorizontal();
        return isExpanded;
    }
    
    protected void DrawButtonRow(params (string label, System.Action onClick)[] buttons)
    {
        GUILayout.BeginHorizontal();
        foreach (var (label, onClick) in buttons)
        {
            if (GUILayout.Button(label))
            {
                onClick?.Invoke();
            }
        }
        GUILayout.EndHorizontal();
    }
    
    protected int DrawIntStepper(string label, int value, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(25)) && value > min) value--;
        GUILayout.Label(value.ToString(), GUILayout.Width(30));
        if (GUILayout.Button("+", GUILayout.Width(25)) && value < max) value++;
        GUILayout.EndHorizontal();
        return value;
    }
    
    protected float DrawSlider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(60));
        value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(150));
        GUILayout.Label($"{value:F1}", GUILayout.Width(40));
        GUILayout.EndHorizontal();
        return value;
    }
    
    protected void DrawStatus(string text)
    {
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.gray;
        GUILayout.Label(text, style);
    }
    #endregion
    
    #region Utility
    protected void LogAction(string action)
    {
        Debug.Log($"[{PanelName}] {action}");
    }
    
    protected bool ValidateManager<T>(T manager, string name) where T : class
    {
        if (manager == null)
        {
            Debug.LogWarning($"[{PanelName}] {name} not available");
            return false;
        }
        return true;
    }
    #endregion
}
