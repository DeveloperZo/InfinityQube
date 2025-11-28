using UnityEngine;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Player Panel - Marker configuration and player control.
/// Allows adjusting marker cooldowns, counts, and types for rapid prototyping.
/// </summary>
public class PlayerPanel : PrototypingPanelBase
{
    public override string PanelName => "Player";
    public override string PanelIcon => "👤";
    public override PrototypingCategory Category => PrototypingCategory.Player;
    public override int Priority => 30;
    
    // Cached reference
    private PlayerActionManager actionManager;
    
    // Section toggles
    private bool showMarkerSettings = true;
    private bool showMarkerPlacement = true;
    private bool showPlayerControl = false;
    
    // Unlimited mode
    private bool unlimitedMode = false;
    
    // Stored values for restoring after unlimited mode
    private float storedLightCooldown;
    private float storedHeavyCooldown;
    private float storedPrimeCooldown;
    private int storedLightCharges;
    private int storedHeavyCharges;
    private int storedPrimeCharges;
    
    public override void Initialize()
    {
        base.Initialize();
        actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
    }
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>
        {
            new QuickAction("∞", ToggleUnlimitedMode) { Group = QuickActionGroup.Player, Priority = 30, Tooltip = "Unlimited markers", IsHighlighted = () => unlimitedMode }
        };
    }
    
    public override void DrawGUI()
    {
        if (actionManager == null)
        {
            actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
        }
        
        string info = actionManager != null 
            ? $"Mode: {actionManager.GetCurrentMode()} | Player: ({playerManager?.currentTilePosition.x}, {playerManager?.currentTilePosition.y})"
            : "PlayerActionManager not available";
        DrawStatus(info);
        
        GUILayout.Space(5);
        
        // Marker Settings
        showMarkerSettings = DrawToggleSection("MARKER SETTINGS", showMarkerSettings);
        if (showMarkerSettings)
        {
            DrawMarkerSettings();
        }
        
        // Marker Placement
        showMarkerPlacement = DrawToggleSection("MARKER PLACEMENT", showMarkerPlacement);
        if (showMarkerPlacement)
        {
            DrawMarkerPlacement();
        }
        
        // Player Control
        showPlayerControl = DrawToggleSection("PLAYER CONTROL", showPlayerControl);
        if (showPlayerControl)
        {
            DrawPlayerControl();
        }
    }
    
    #region Marker Settings
    private void DrawMarkerSettings()
    {
        if (actionManager == null)
        {
            GUILayout.Label("PlayerActionManager not found");
            return;
        }
        
        DrawSection("", () =>
        {
            // Current mode display
            GUILayout.Label($"Current Mode: {actionManager.GetCurrentMode()}");
            
            // Mode switch buttons (1-4 keys)
            GUILayout.BeginHorizontal();
            
            GUI.backgroundColor = actionManager.GetCurrentMode() == MarkerMode.Unit ? new Color(0.8f, 0.5f, 0.2f) : Color.white;
            if (GUILayout.Button("1:UnitMarker")) actionManager.SetMode(MarkerMode.Unit);
            
            GUI.backgroundColor = actionManager.GetCurrentMode() == MarkerMode.Prime ? new Color(0.2f, 0.5f, 0.8f) : Color.white;
            if (GUILayout.Button("2:PrimeMarker")) actionManager.SetMode(MarkerMode.Prime);
            
            GUI.backgroundColor = actionManager.GetCurrentMode() == MarkerMode.Recursion ? new Color(0.6f, 0.2f, 0.6f) : Color.white;
            if (GUILayout.Button("3:RecursionMarker")) actionManager.SetMode(MarkerMode.Recursion);
            
            GUI.backgroundColor = actionManager.GetCurrentMode() == MarkerMode.Infinity ? new Color(0.1f, 0.1f, 0.1f) : Color.white;
            GUI.contentColor = actionManager.GetCurrentMode() == MarkerMode.Infinity ? Color.white : Color.black;
            if (GUILayout.Button("4:InfinityMarker")) actionManager.SetMode(MarkerMode.Infinity);
            GUI.contentColor = Color.white;
            
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();;
            
            GUILayout.Space(10);
            
            // Unlimited mode toggle
            GUI.backgroundColor = unlimitedMode ? Color.green : Color.white;
            if (GUILayout.Button(unlimitedMode ? "✓ UNLIMITED MODE ON" : "Enable Unlimited Mode", GUILayout.Height(25)))
            {
                ToggleUnlimitedMode();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            // UnitMarker settings
            DrawMarkerTypeSettings("UnitMarker", 
                ref actionManager.maxLightMarkerCharges, 
                ref actionManager.lightMarkerCooldown,
                ref actionManager.maxUnitMarkers,
                actionManager.GetLightMarkerCooldownRemaining());
            
            // PrimeMarker settings
            DrawMarkerTypeSettings("PrimeMarker", 
                ref actionManager.maxPrimeMarkerCharges, 
                ref actionManager.primeMarkerCooldown,
                ref actionManager.maxPrimeMarkers,
                actionManager.GetPrimeMarkerCooldownRemaining());
            
            // RecursionMarker settings
            DrawMarkerTypeSettings("RecursionMarker", 
                ref actionManager.maxRecursionMarkerCharges, 
                ref actionManager.RecursionMarkerCooldown,
                ref actionManager.maxRecursionMarkers,
                actionManager.GetRecursionMarkerCooldownRemaining());
            
            // InfinityMarker settings
            DrawMarkerTypeSettings("InfinityMarker", 
                ref actionManager.maxInfinityMarkerCharges, 
                ref actionManager.infinityMarkerCooldown,
                ref actionManager.maxInfinityMarkers,
                actionManager.GetInfinityMarkerCooldownRemaining());
            
            GUILayout.Space(5);
            
            // Quick presets
            GUILayout.Label("Quick Presets:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Defaults"))
            {
                ResetToDefaults();
            }
            if (GUILayout.Button("No Cooldowns"))
            {
                SetAllCooldowns(0);
            }
            if (GUILayout.Button("Refill Charges"))
            {
                RefillAllCharges();
            }
            GUILayout.EndHorizontal();;
        });
    }
    
    private void DrawMarkerTypeSettings(string label, ref int maxCharges, ref float cooldown, ref int maxOnGrid, float cooldownRemaining)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}", GUILayout.Width(110));
        GUILayout.Label($"CD: {cooldownRemaining:F1}s", GUILayout.Width(70));
        GUILayout.EndHorizontal();
        
        // Max charges
        GUILayout.BeginHorizontal();
        GUILayout.Label("Charges:", GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(25)) && maxCharges > 0) maxCharges--;
        GUILayout.Label($"{maxCharges}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(25)) && maxCharges < 20) maxCharges++;
        GUILayout.EndHorizontal();
        
        // Cooldown
        GUILayout.BeginHorizontal();
        GUILayout.Label("Cooldown:", GUILayout.Width(60));
        cooldown = GUILayout.HorizontalSlider(cooldown, 0f, 10f, GUILayout.Width(100));
        GUILayout.Label($"{cooldown:F1}s", GUILayout.Width(40));
        if (GUILayout.Button("0", GUILayout.Width(25))) cooldown = 0;
        GUILayout.EndHorizontal();
        
        // Max on grid
        GUILayout.BeginHorizontal();
        GUILayout.Label("Max Grid:", GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(25)) && maxOnGrid > 1) maxOnGrid--;
        GUILayout.Label($"{maxOnGrid}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(25)) && maxOnGrid < 20) maxOnGrid++;
        GUILayout.EndHorizontal();;
        
        GUILayout.EndVertical();
    }
    
    private void ToggleUnlimitedMode()
    {
        if (actionManager == null) return;
        
        unlimitedMode = !unlimitedMode;
        
        if (unlimitedMode)
        {
            // Store current values
            storedLightCooldown = actionManager.lightMarkerCooldown;
            storedHeavyCooldown = actionManager.RecursionMarkerCooldown;
            storedPrimeCooldown = actionManager.primeMarkerCooldown;
            storedLightCharges = actionManager.maxLightMarkerCharges;
            storedHeavyCharges = actionManager.maxRecursionMarkerCharges;
            storedPrimeCharges = actionManager.maxPrimeMarkerCharges;
            
            // Set unlimited
            actionManager.lightMarkerCooldown = 0;
            actionManager.RecursionMarkerCooldown = 0;
            actionManager.primeMarkerCooldown = 0;
            actionManager.maxLightMarkerCharges = 99;
            actionManager.maxRecursionMarkerCharges = 99;
            actionManager.maxPrimeMarkerCharges = 99;
            actionManager.maxUnitMarkers = 99;
            actionManager.maxRecursionMarkers = 99;
            actionManager.maxPrimeMarkers = 99;
            
            RefillAllCharges();
            LogAction("Unlimited mode ON");
        }
        else
        {
            // Restore values
            actionManager.lightMarkerCooldown = storedLightCooldown;
            actionManager.RecursionMarkerCooldown = storedHeavyCooldown;
            actionManager.primeMarkerCooldown = storedPrimeCooldown;
            actionManager.maxLightMarkerCharges = storedLightCharges;
            actionManager.maxRecursionMarkerCharges = storedHeavyCharges;
            actionManager.maxPrimeMarkerCharges = storedPrimeCharges;
            
            LogAction("Unlimited mode OFF");
        }
    }
    
    private void SetAllCooldowns(float value)
    {
        if (actionManager == null) return;
        actionManager.lightMarkerCooldown = value;
        actionManager.RecursionMarkerCooldown = value;
        actionManager.primeMarkerCooldown = value;
        LogAction($"All cooldowns set to {value}");
    }
    
    private void RefillAllCharges()
    {
        if (actionManager == null) return;
        actionManager.RefillLightMarkerCharges();
        actionManager.RefillRecursionMarkerCharges();
        actionManager.RefillPrimeMarkerCharges();
        LogAction("All charges refilled");
    }
    
    private void ResetToDefaults()
    {
        if (actionManager == null) return;
        unlimitedMode = false;
        actionManager.lightMarkerCooldown = 5f;
        actionManager.RecursionMarkerCooldown = 5f;
        actionManager.primeMarkerCooldown = 5f;
        actionManager.maxLightMarkerCharges = 3;
        actionManager.maxRecursionMarkerCharges = 2;
        actionManager.maxPrimeMarkerCharges = 2;
        actionManager.maxUnitMarkers = 3;
        actionManager.maxRecursionMarkers = 2;
        actionManager.maxPrimeMarkers = 2;
        LogAction("Reset to defaults");
    }
    #endregion
    
    #region Marker Placement
    private void DrawMarkerPlacement()
    {
        DrawSection("", () =>
        {
            // Recorded markers info
            var markers = waveManager?.GetPreviousWaveMarkers();
            int totalMarkers = markers?.GetTotalMarkerCount() ?? 0;
            GUILayout.Label($"Recorded for Mirror: {totalMarkers} markers");
            
            GUILayout.Space(5);
            
            // Place marker at player position
            Vector2Int playerPos = playerManager?.currentTilePosition ?? Vector2Int.zero;
            GUILayout.Label($"Player Position: ({playerPos.x}, {playerPos.y})");
            
            GUILayout.Label("Place (F key):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("UnitMarker")) PlaceAndRecord(playerPos, MarkerMode.Unit);
            if (GUILayout.Button("PrimeMarker")) PlaceAndRecord(playerPos, MarkerMode.Prime);
            if (GUILayout.Button("RecursionMarker")) PlaceAndRecord(playerPos, MarkerMode.Recursion);
            if (GUILayout.Button("InfinityMarker")) PlaceAndRecord(playerPos, MarkerMode.Infinity);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Undo last marker
            GUILayout.Label("Undo:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("UnitMarker")) UndoLastMarker(MarkerMode.Unit);
            if (GUILayout.Button("PrimeMarker")) UndoLastMarker(MarkerMode.Prime);
            if (GUILayout.Button("RecursionMarker")) UndoLastMarker(MarkerMode.Recursion);
            if (GUILayout.Button("InfinityMarker")) UndoLastMarker(MarkerMode.Infinity);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Clear all
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Grid Markers"))
            {
                gridManager?.ClearAllMarkers();
            }
            if (GUILayout.Button("Clear Recorded"))
            {
                waveManager?.ClearPreviousWaveMarkers();
                LogAction("Cleared recorded markers");
            }
            GUILayout.EndHorizontal();;
            
            GUILayout.Space(5);
            
            // Quick place row
            GUILayout.Label("Quick Place (at player Y):");
            GUILayout.BeginHorizontal();
            int width = gridManager?.Width ?? 10;
            for (int x = 0; x < Mathf.Min(width, 12); x++)
            {
                int col = x;
                if (GUILayout.Button($"{x}", GUILayout.Width(28)))
                {
                    Vector2Int pos = new Vector2Int(col, playerPos.y);
                    PlaceAndRecord(pos, actionManager?.GetCurrentMode() ?? MarkerMode.Unit);
                }
            }
            GUILayout.EndHorizontal();;
        });
    }
    
    private void PlaceAndRecord(Vector2Int pos, MarkerMode mode)
    {
        gridManager?.PlaceMarker(pos.x, pos.y);
        waveManager?.RecordMarkerPosition(pos, mode);
        LogAction($"Placed & recorded {mode} at ({pos.x}, {pos.y})");
    }
    
    private void UndoLastMarker(MarkerMode mode)
    {
        var markers = waveManager?.GetPreviousWaveMarkers();
        if (markers == null) return;
        
        List<Vector2Int> markerList = null;
        switch (mode)
        {
            case MarkerMode.Unit: markerList = markers.lightMarkerPositions; break;
            case MarkerMode.Recursion: markerList = markers.RecursionMarkerPositions; break;
            case MarkerMode.Prime: markerList = markers.primeMarkerPositions; break;
        }
        
        if (markerList != null && markerList.Count > 0)
        {
            Vector2Int lastPos = markerList[markerList.Count - 1];
            waveManager?.UnrecordMarkerPosition(lastPos, mode);
            LogAction($"Undid {mode} marker at ({lastPos.x}, {lastPos.y})");
        }
    }
    #endregion
    
    #region Player Control
    private void DrawPlayerControl()
    {
        DrawSection("", () =>
        {
            GUILayout.Label("Teleport:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("←")) Teleport(-1, 0);
            if (GUILayout.Button("→")) Teleport(1, 0);
            if (GUILayout.Button("↓")) Teleport(0, -1);
            if (GUILayout.Button("↑")) Teleport(0, 1);
            if (GUILayout.Button("Center")) TeleportCenter();
            GUILayout.EndHorizontal();;
        });
    }
    
    private void Teleport(int dx, int dy)
    {
        if (playerManager == null || gridManager == null) return;
        var newPos = playerManager.currentTilePosition + new Vector2Int(dx, dy);
        newPos.x = Mathf.Clamp(newPos.x, 0, gridManager.Width - 1);
        newPos.y = Mathf.Clamp(newPos.y, 0, gridManager.Height - 1);
        
        playerManager.currentTilePosition = newPos;
        playerManager.transform.position = gridManager.GridToWorldPosition(newPos.x, newPos.y, 0);
    }
    
    private void TeleportCenter()
    {
        if (playerManager == null || gridManager == null) return;
        int x = gridManager.Width / 2;
        playerManager.currentTilePosition = new Vector2Int(x, 0);
        playerManager.transform.position = gridManager.GridToWorldPosition(x, 0, 0);
    }
    #endregion
}
