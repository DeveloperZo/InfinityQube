public enum DebugPanelGroup
{
    Gameplay,
    Stage,
    Player,
    Grid,
    System,
    Misc
}

public interface IDebugPanel
{
    string PanelName { get; }
    DebugPanelGroup PanelGroup { get; }
    void Initialize();
    void Update();
    void DrawPanel();
}