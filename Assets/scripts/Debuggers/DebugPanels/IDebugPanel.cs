using static Enumerations;

public interface IDebugPanel
{
    string PanelName { get; }
    DebugPanelGroup Group { get; }
    void Initialize();
    void Update();
    void DrawPanel();
}
