public interface IDebugPanel
{
    string PanelName { get; }
    void Initialize();
    void Update();
    void DrawPanel();
}