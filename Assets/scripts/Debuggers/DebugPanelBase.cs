using static Enumerations;

public abstract class DebugPanelBase : IDebugPanel
{
    public abstract string PanelName { get; }
    public abstract DebugPanelGroup Group { get; }

    public virtual void Initialize() { }
    public virtual void Update() { }
    public abstract void DrawPanel();
}

