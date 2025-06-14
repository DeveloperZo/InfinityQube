using UnityEngine;

public abstract class DebugPanelBase : IDebugPanel
{
    public abstract string PanelName { get; }
    public virtual DebugPanelGroup PanelGroup => DebugPanelGroup.Misc;

    // Optional initialization for derived panels
    public virtual void Initialize() { }

    // Optional per-frame update
    public virtual void Update() { }

    // Main draw call for the panel
    public abstract void DrawPanel();
}
