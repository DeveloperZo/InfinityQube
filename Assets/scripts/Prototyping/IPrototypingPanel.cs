using System.Collections.Generic;

/// <summary>
/// Interface for prototyping panels.
/// </summary>
public interface IPrototypingPanel
{
    string PanelName { get; }
    string PanelIcon { get; }
    PrototypingCategory Category { get; }
    int Priority { get; }
    bool IsVisible { get; }
    
    void Initialize();
    void DrawGUI();
    void OnShow();
    void OnHide();
    void Update();
    List<QuickAction> GetQuickActions();
}

public enum PrototypingCategory
{
    Wave,
    Grid,
    Player,
    Stage,
    System
}

/// <summary>
/// Quick action for the toolbar.
/// </summary>
public class QuickAction
{
    public string Label { get; set; }
    public string Icon { get; set; }
    public string Tooltip { get; set; }
    public System.Action OnClick { get; set; }
    public System.Func<bool> IsEnabled { get; set; }
    public System.Func<bool> IsHighlighted { get; set; }
    public QuickActionGroup Group { get; set; }
    public int Priority { get; set; }

    public QuickAction(string label, System.Action onClick, string icon = null)
    {
        Label = label;
        Icon = icon ?? "";
        OnClick = onClick;
        IsEnabled = () => true;
        IsHighlighted = () => false;
        Group = QuickActionGroup.General;
        Priority = 100;
    }
}

public enum QuickActionGroup
{
    Wave,
    Grid,
    Player,
    Stage,
    System,
    General
}
