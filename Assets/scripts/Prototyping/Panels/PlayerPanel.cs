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
    public override string PanelIcon => "P";
    public override PrototypingCategory Category => PrototypingCategory.Player;
    public override int Priority => 30;
    
    // Cached reference
    private PlayerActionManager actionManager;
    
    // Section toggles
    private bool showMarkerSettings = true;
    private bool showMarkerPlacement = true;
    private bool showPlayerControl = false;
    private bool showAttunements = true;
    
    // Unlimited mode
    private bool unlimitedMode = false;
    
    // Stored values for restoring after unlimited mode
    private int storedUnitRechargeRate;
    private int storedUnitCharges;
    private int storedRecursionCharges;
    private int storedMatrixCharges;
    private int storedInfinityCharges;
    private int storedMatrixMarkerOnGridLimit;
    
    public override void Initialize()
    {
        base.Initialize();
        actionManager = Object.FindFirstObjectByType<PlayerActionManager>();
    }
    
    public override List<QuickAction> GetQuickActions()
    {
        return new List<QuickAction>();
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
        
        // Attunements
        showAttunements = DrawToggleSection("ATTUNEMENTS", showAttunements);
        if (showAttunements)
        {
            DrawAttunements();
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
            
            GUI.backgroundColor = actionManager.GetCurrentMode() == MarkerMode.Matrix ? new Color(0.2f, 0.5f, 0.8f) : Color.white;
            if (GUILayout.Button("2:MatrixMarker")) actionManager.SetMode(MarkerMode.Matrix);
            
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
            
            GUILayout.Space(5);
            
            // Marker Economy toggle
            GUI.backgroundColor = actionManager.useMarkerEconomy ? Color.cyan : Color.gray;
            if (GUILayout.Button(actionManager.useMarkerEconomy ? "✓ MARKER ECONOMY ON" : "✗ Marker Economy OFF"))
            {
                actionManager.useMarkerEconomy = !actionManager.useMarkerEconomy;
                LogAction($"Marker Economy: {(actionManager.useMarkerEconomy ? "ON" : "OFF")}");
            }
            GUI.backgroundColor = Color.white;
            
            // Manual grant buttons (when economy is enabled)
            if (actionManager.useMarkerEconomy)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply Stage Grants"))
                {
                    actionManager.ApplyStageGrants();
                    LogAction("Applied stage grants");
                }
                if (GUILayout.Button("Apply Wave Grants"))
                {
                    actionManager.ApplyWaveGrants();
                    LogAction("Applied wave grants");
                }
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(10);
            
            // UnitMarker settings (move-based recharge)
            DrawUnitMarkerSettings("UnitMarker", 
                ref actionManager.maxUnitMarkerCharges, 
                ref actionManager.unitMarkerRechargeRate,
                ref actionManager.maxUnitMarkers,
                actionManager.GetUnitMarkerCooldownRemaining());
            
            // MatrixMarker settings (inventory-based)
            DrawMarkerTypeSettings("MatrixMarker", 
                ref actionManager.maxMatrixMarkerCharges, 
                ref actionManager.maxMatrixMarkers);
            
            // RecursionMarker settings (inventory-based)
            DrawMarkerTypeSettings("RecursionMarker", 
                ref actionManager.maxRecursionMarkerCharges, 
                ref actionManager.maxRecursionMarkers);
            
            // InfinityMarker settings (inventory-based)
            DrawMarkerTypeSettings("InfinityMarker", 
                ref actionManager.maxInfinityMarkerCharges, 
                ref actionManager.maxInfinityMarkers);
            
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
    
    /// <summary>
    /// Draw marker settings for inventory-based markers (Recursion, Matrix, Infinity)
    /// These markers use inventory grants only - no cooldown regeneration
    /// </summary>
    private void DrawMarkerTypeSettings(string label, ref int maxCharges, ref int maxOnGrid)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.Label($"{label} (Inventory-based)", GUILayout.Width(180));
        
        // Max charges (inventory cap)
        GUILayout.BeginHorizontal();
        GUILayout.Label("Inventory:", GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(25)) && maxCharges > 0) maxCharges--;
        GUILayout.Label($"{maxCharges}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(25)) && maxCharges < 20) maxCharges++;
        GUILayout.EndHorizontal();
        
        // Max on grid
        GUILayout.BeginHorizontal();
        GUILayout.Label("Max Grid:", GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(25)) && maxOnGrid > 1) maxOnGrid--;
        GUILayout.Label($"{maxOnGrid}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(25)) && maxOnGrid < 20) maxOnGrid++;
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
    }
    
    /// <summary>
    /// Draw Unit marker settings with move-based recharge rate instead of time-based cooldown
    /// </summary>
    private void DrawUnitMarkerSettings(string label, ref int maxCharges, ref int rechargeRate, ref int maxOnGrid, float movesRemaining)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}", GUILayout.Width(110));
        GUILayout.Label($"Moves: {movesRemaining:F0}", GUILayout.Width(70));
        GUILayout.EndHorizontal();
        
        // Max charges
        GUILayout.BeginHorizontal();
        GUILayout.Label("Charges:", GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(25)) && maxCharges > 0) maxCharges--;
        GUILayout.Label($"{maxCharges}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(25)) && maxCharges < 20) maxCharges++;
        GUILayout.EndHorizontal();
        
        // Recharge rate (moves per charge)
        GUILayout.BeginHorizontal();
        GUILayout.Label("Recharge:", GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(25)) && rechargeRate > 1) rechargeRate--;
        GUILayout.Label($"{rechargeRate}m", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(25)) && rechargeRate < 10) rechargeRate++;
        GUILayout.EndHorizontal();
        
        // Max on grid
        GUILayout.BeginHorizontal();
        GUILayout.Label("Max Grid:", GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(25)) && maxOnGrid > 1) maxOnGrid--;
        GUILayout.Label($"{maxOnGrid}", GUILayout.Width(25));
        if (GUILayout.Button("+", GUILayout.Width(25)) && maxOnGrid < 20) maxOnGrid++;
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
    }
    
    private void ToggleUnlimitedMode()
    {
        if (actionManager == null) return;
        
        unlimitedMode = !unlimitedMode;
        
        if (unlimitedMode)
        {
            // Store current values
            storedUnitRechargeRate = actionManager.unitMarkerRechargeRate;
            storedUnitCharges = actionManager.maxUnitMarkerCharges;
            storedRecursionCharges = actionManager.maxRecursionMarkerCharges;
            storedMatrixCharges = actionManager.maxMatrixMarkerCharges;
            storedInfinityCharges = actionManager.maxInfinityMarkerCharges;
            storedMatrixMarkerOnGridLimit = actionManager.matrixMarkerOnGridLimit;
            
            // Set unlimited for all marker types
            actionManager.unitMarkerRechargeRate = 1; // Recharge every move
            actionManager.maxUnitMarkerCharges = 99;
            actionManager.maxRecursionMarkerCharges = 99;
            actionManager.maxMatrixMarkerCharges = 99;
            actionManager.maxInfinityMarkerCharges = 99;
            actionManager.maxUnitMarkers = 99;
            actionManager.maxRecursionMarkers = 99;
            actionManager.maxMatrixMarkers = 99;
            actionManager.maxInfinityMarkers = 99;
            actionManager.matrixMarkerOnGridLimit = 99;
            
            RefillAllCharges();
            LogAction("Unlimited mode ON - All marker types enabled");
        }
        else
        {
            // Restore values
            actionManager.unitMarkerRechargeRate = storedUnitRechargeRate;
            actionManager.maxUnitMarkerCharges = storedUnitCharges;
            actionManager.maxRecursionMarkerCharges = storedRecursionCharges;
            actionManager.maxMatrixMarkerCharges = storedMatrixCharges;
            actionManager.maxInfinityMarkerCharges = storedInfinityCharges;
            actionManager.matrixMarkerOnGridLimit = storedMatrixMarkerOnGridLimit;
            
            LogAction("Unlimited mode OFF");
        }
    }
    
    private void SetAllCooldowns(float value)
    {
        if (actionManager == null) return;
        // Unit marker uses move-based recharge - set recharge rate
        // Non-Unit markers use inventory grants only (no cooldowns)
        actionManager.unitMarkerRechargeRate = value <= 0 ? 1 : (int)value;
        LogAction($"Unit recharge rate set to {actionManager.unitMarkerRechargeRate} moves");
    }
    
    private void RefillAllCharges()
    {
        if (actionManager == null) return;
        actionManager.RefillUnitMarkerCharges();
        actionManager.RefillRecursionMarkerCharges();
        actionManager.RefillMatrixMarkerCharges();
        actionManager.RefillInfinityMarkerCharges();
        LogAction("All charges refilled");
    }
    
    private void ResetToDefaults()
    {
        if (actionManager == null) return;
        unlimitedMode = false;
        // Unit marker settings (move-based regeneration)
        actionManager.unitMarkerRechargeRate = 3; // 3 moves per charge
        actionManager.maxUnitMarkerCharges = 3;
        actionManager.maxUnitMarkers = 3;
        // Non-Unit markers (inventory-based, no cooldowns)
        actionManager.maxRecursionMarkerCharges = 2;
        actionManager.maxMatrixMarkerCharges = 2;
        actionManager.maxInfinityMarkerCharges = 1;
        actionManager.maxRecursionMarkers = 2;
        actionManager.maxMatrixMarkers = 2;
        actionManager.maxInfinityMarkers = 2;
        LogAction("Reset to defaults");
    }
    #endregion
    
    #region Marker Placement
    private void DrawMarkerPlacement()
    {
        DrawSection("", () =>
        {
            Vector2Int playerPos = playerManager?.currentTilePosition ?? Vector2Int.zero;
            GUILayout.Label($"Player Position: ({playerPos.x}, {playerPos.y})");
            
            GUILayout.Space(3);
            
            // Place markers at player position using current mode
            GUILayout.Label("Place at player position:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unit")) PlaceMarkerAtPlayer(MarkerMode.Unit);
            if (GUILayout.Button("Matrix")) PlaceMarkerAtPlayer(MarkerMode.Matrix);
            if (GUILayout.Button("Recursion")) PlaceMarkerAtPlayer(MarkerMode.Recursion);
            if (GUILayout.Button("Infinity")) PlaceMarkerAtPlayer(MarkerMode.Infinity);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Clear markers
            if (GUILayout.Button("Clear All Markers"))
            {
                gridManager?.ClearAllMarkers();
                actionManager?.MarkerSystem?.ClearAllActions();
                LogAction("Cleared all markers");
            }
        });
    }
    
    private void PlaceMarkerAtPlayer(MarkerMode mode)
    {
        Vector2Int pos = playerManager?.currentTilePosition ?? Vector2Int.zero;
        gridManager?.PlaceMarker(pos.x, pos.y);
        LogAction($"Placed {mode} marker at ({pos.x}, {pos.y})");
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
    
    #region Attunements
    private void DrawAttunements()
    {
        DrawSection("", () =>
        {
            // Check if managers are available
            if (!SaveManager.IsInitialized)
            {
                GUILayout.Label("SaveManager not initialized");
                if (GUILayout.Button("Add SaveManager to Scene"))
                {
                    var go = new GameObject("SaveManager");
                    go.AddComponent<SaveManager>();
                    LogAction("Created SaveManager");
                }
                return;
            }
            
            if (!AttunementManager.IsInitialized)
            {
                GUILayout.Label("AttunementManager not initialized");
                if (GUILayout.Button("Add AttunementManager to Scene"))
                {
                    var go = new GameObject("AttunementManager");
                    go.AddComponent<AttunementManager>();
                    LogAction("Created AttunementManager");
                }
                return;
            }
            
            // Currency display and controls
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Axiom Shards: {SaveManager.Instance.AxiomShards}", GUILayout.Width(150));
            if (GUILayout.Button("+100")) 
            {
                SaveManager.Instance.AwardShards(100, "Debug");
                LogAction("Awarded 100 shards");
            }
            if (GUILayout.Button("+1000")) 
            {
                SaveManager.Instance.AwardShards(1000, "Debug");
                LogAction("Awarded 1000 shards");
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Matrix Attunements
            DrawAttunementRow("Matrix", MarkerMode.Matrix);
            
            // Recursion Attunements
            DrawAttunementRow("Recursion", MarkerMode.Recursion);
            
            // Infinity Attunements
            DrawAttunementRow("Infinity", MarkerMode.Infinity);
            
            GUILayout.Space(5);
            
            // Quick actions
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock All"))
            {
                UnlockAllAttunements();
            }
            if (GUILayout.Button("Unequip All"))
            {
                UnequipAllAttunements();
            }
            if (GUILayout.Button("Reset Save"))
            {
                SaveManager.Instance.DeleteSave();
                LogAction("Save reset");
            }
            GUILayout.EndHorizontal();
        });
    }
    
    private void DrawAttunementRow(string label, MarkerMode mode)
    {
        var attunements = AttunementManager.Instance.GetAttunmentsForMarker(mode);
        string equipped = AttunementManager.Instance.GetEquippedAttunementName(mode);
        
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label($"{label}: {equipped}");
        
        GUILayout.BeginHorizontal();
        
        // None button (unequip)
        GUI.backgroundColor = string.IsNullOrEmpty(SaveManager.Instance.GetEquippedAttunement(mode)) ? Color.green : Color.white;
        if (GUILayout.Button("None", GUILayout.Width(50)))
        {
            SaveManager.Instance.EquipAttunement(mode, "");
            LogAction($"Unequipped {mode} attunement");
        }
        
        // Attunement buttons
        foreach (var att in attunements)
        {
            bool isUnlocked = AttunementManager.Instance.IsUnlocked(att.id);
            bool isEquipped = SaveManager.Instance.GetEquippedAttunement(mode) == att.id;
            
            GUI.backgroundColor = isEquipped ? Color.green : (isUnlocked ? Color.cyan : Color.gray);
            
            string buttonText = isUnlocked ? att.displayName.Split(' ')[0] : $"🔒{att.unlockCost}";
            
            if (GUILayout.Button(buttonText, GUILayout.MinWidth(60)))
            {
                if (isUnlocked)
                {
                    SaveManager.Instance.EquipAttunement(mode, att.id);
                    LogAction($"Equipped {att.displayName}");
                }
                else
                {
                    if (SaveManager.Instance.TryUnlockAttunement(att.id, att.unlockCost))
                    {
                        LogAction($"Unlocked {att.displayName}");
                    }
                    else
                    {
                        LogAction($"Not enough shards for {att.displayName}");
                    }
                }
            }
        }
        
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    
    private void UnlockAllAttunements()
    {
        if (!AttunementManager.IsInitialized) return;
        
        foreach (var def in AttunementManager.Instance.Definitions.Values)
        {
            if (!SaveManager.Instance.Progression.IsAttunementUnlocked(def.id))
            {
                SaveManager.Instance.Progression.UnlockAttunement(def.id);
            }
        }
        SaveManager.Instance.Save();
        LogAction("Unlocked all attunements");
    }
    
    private void UnequipAllAttunements()
    {
        SaveManager.Instance.EquipAttunement(MarkerMode.Matrix, "");
        SaveManager.Instance.EquipAttunement(MarkerMode.Recursion, "");
        SaveManager.Instance.EquipAttunement(MarkerMode.Infinity, "");
        LogAction("Unequipped all attunements");
    }
    #endregion
}
