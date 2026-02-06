using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Enumerations;

/// <summary>
/// Attunement panel with pre-placed items.
/// 
/// Setup in prefab:
/// 1. Create 7 item GameObjects in the content area (prune all others)
/// 2. Assign each to its slot below
/// 3. Assign tab buttons
/// 
/// Items show/hide based on selected tab.
/// Locked items are greyed out but still visible.
/// </summary>
public class AttunementPanel : MonoBehaviour
{
    #region Tab References
    
    [Header("Close Button")]
    [SerializeField] private Button closeButton;
    
    [Header("Tab Buttons")]
    [SerializeField] private Button allTabButton;
    [SerializeField] private Button matrixTabButton;
    [SerializeField] private Button recursionTabButton;
    [SerializeField] private Button infinityTabButton;
    
    [Header("Tab Focus Indicators (optional)")]
    [SerializeField] private GameObject allTabFocus;
    [SerializeField] private GameObject matrixTabFocus;
    [SerializeField] private GameObject recursionTabFocus;
    [SerializeField] private GameObject infinityTabFocus;
    
    #endregion
    
    #region Attunement Items (assign the 7 pre-placed items)
    
    [Header("Matrix Attunements (3 items)")]
    [SerializeField] private GameObject matrixMasteryItem;
    [SerializeField] private GameObject matrixAbundanceItem;
    [SerializeField] private GameObject infinityForgeItem;
    
    [Header("Recursion Attunements (3 items)")]
    [SerializeField] private GameObject recursionCloneItem;
    [SerializeField] private GameObject recursionAbundanceItem;
    [SerializeField] private GameObject infinityGatewayItem;
    
    [Header("Infinity Attunements (1 TBD item)")]
    [SerializeField] private GameObject infinityTBDItem;
    
    #endregion
    
    #region Info Display
    
    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI shardsText;
    
    #endregion
    
    #region Attunement Data
    
    private struct AttunementInfo
    {
        public string id;
        public string displayName;
        public string description;
        public MarkerMode mode;
        public GameObject item;
    }
    
    private AttunementInfo[] attunements;
    
    #endregion
    
    #region State
    
    private enum TabType { All, Matrix, Recursion, Infinity }
    private TabType currentTab = TabType.Matrix;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        BuildAttunementList();
        AutoFindTabButtons();
    }
    
    private void OnEnable()
    {
        SetupListeners();
        ShowTab(TabType.Matrix);
        RefreshShards();
    }
    
    private void OnDisable()
    {
        RemoveListeners();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }
    
    #endregion
    
    #region Setup
    
    private void BuildAttunementList()
    {
        attunements = new AttunementInfo[]
        {
            // Matrix
            new AttunementInfo
            {
                id = "matrix_mastery",
                displayName = "Matrix Mastery",
                description = "All Matrix areas are 3x3 instead of 2x2.\n\n<color=#FFA500>Power</color> - Bigger captures.",
                mode = MarkerMode.Matrix,
                item = matrixMasteryItem
            },
            new AttunementInfo
            {
                id = "matrix_abundance",
                displayName = "Matrix Abundance",
                description = "+2 Matrix markers per stage.\n\n<color=#00FF00>Resource</color> - Use Matrix more often.",
                mode = MarkerMode.Matrix,
                item = matrixAbundanceItem
            },
            new AttunementInfo
            {
                id = "infinity_forge",
                displayName = "Infinity Forge",
                description = "Matrix + ∞ collision creates area marker.\n\n<color=#8844FF>Utility</color> - ∞ cubes become opportunities.",
                mode = MarkerMode.Matrix,
                item = infinityForgeItem
            },
            
            // Recursion
            new AttunementInfo
            {
                id = "recursion_clone",
                displayName = "Recursion Clone",
                description = "R+R becomes clone+swap instead of capture+swap.\n\n<color=#FF6666>Power</color> - Multiply cubes.",
                mode = MarkerMode.Recursion,
                item = recursionCloneItem
            },
            new AttunementInfo
            {
                id = "recursion_abundance",
                displayName = "Recursion Abundance",
                description = "+2 Recursion markers per stage.\n\n<color=#00AA88>Resource</color> - Reposition more often.",
                mode = MarkerMode.Recursion,
                item = recursionAbundanceItem
            },
            new AttunementInfo
            {
                id = "infinity_gateway",
                displayName = "Infinity Gateway",
                description = "Recursion + ∞ collision creates swap marker.\n\n<color=#6666FF>Utility</color> - ∞ walls become opportunities.",
                mode = MarkerMode.Recursion,
                item = infinityGatewayItem
            },
            
            // Infinity
            new AttunementInfo
            {
                id = "infinity_tbd",
                displayName = "Coming Soon",
                description = "Infinity attunements pending playtesting.",
                mode = MarkerMode.Infinity,
                item = infinityTBDItem
            }
        };
        
        // Set up click handlers for each item
        foreach (var att in attunements)
        {
            if (att.item == null) continue;
            
            var info = att; // Capture for closure
            var button = att.item.GetComponent<Button>();
            if (button == null) button = att.item.AddComponent<Button>();
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnAttunementClicked(info));
        }
    }
    
    private void AutoFindTabButtons()
    {
        if (closeButton == null)
            closeButton = FindButton("Back", "Close", "Exit");
        if (allTabButton == null)
            allTabButton = FindButton("Tab");
        if (matrixTabButton == null)
            matrixTabButton = FindButton("MatrixTab");
        if (recursionTabButton == null)
            recursionTabButton = FindButton("RecursionTab");
        if (infinityTabButton == null)
            infinityTabButton = FindButton("InfinityTab");
        
        // Find focus indicators
        if (allTabFocus == null)
            allTabFocus = FindChild("Tab", "TabFocus") ?? FindChild("Tab", "IconFocus");
        if (matrixTabFocus == null)
            matrixTabFocus = FindChild("MatrixTab", "TabFocus") ?? FindChild("MatrixTab", "IconFocus");
        if (recursionTabFocus == null)
            recursionTabFocus = FindChild("RecursionTab", "TabFocus") ?? FindChild("RecursionTab", "IconFocus");
        if (infinityTabFocus == null)
            infinityTabFocus = FindChild("InfinityTab", "TabFocus") ?? FindChild("InfinityTab", "IconFocus");
    }
    
    private Button FindButton(params string[] names)
    {
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            foreach (var name in names)
            {
                if (btn.name == name || btn.name.Contains(name))
                    return btn;
            }
        }
        return null;
    }
    
    private GameObject FindChild(string parentName, string childName)
    {
        var parent = FindRecursive(transform, parentName);
        if (parent == null) return null;
        var child = FindRecursive(parent, childName);
        return child?.gameObject;
    }
    
    private Transform FindRecursive(Transform t, string name)
    {
        foreach (Transform child in t)
        {
            if (child.name == name) return child;
            var found = FindRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
    
    private void SetupListeners()
    {
        closeButton?.onClick.AddListener(Close);
        allTabButton?.onClick.AddListener(() => ShowTab(TabType.All));
        matrixTabButton?.onClick.AddListener(() => ShowTab(TabType.Matrix));
        recursionTabButton?.onClick.AddListener(() => ShowTab(TabType.Recursion));
        infinityTabButton?.onClick.AddListener(() => ShowTab(TabType.Infinity));
    }
    
    private void RemoveListeners()
    {
        closeButton?.onClick.RemoveAllListeners();
        allTabButton?.onClick.RemoveAllListeners();
        matrixTabButton?.onClick.RemoveAllListeners();
        recursionTabButton?.onClick.RemoveAllListeners();
        infinityTabButton?.onClick.RemoveAllListeners();
    }
    
    #endregion
    
    #region Tab Switching
    
    private void ShowTab(TabType tab)
    {
        currentTab = tab;
        Debug.Log($"[AttunementPanel] ShowTab called: {tab}");
        
        // Update tab focus visuals
        allTabFocus?.SetActive(tab == TabType.All);
        matrixTabFocus?.SetActive(tab == TabType.Matrix);
        recursionTabFocus?.SetActive(tab == TabType.Recursion);
        infinityTabFocus?.SetActive(tab == TabType.Infinity);
        
        // Count items
        int totalItems = attunements?.Length ?? 0;
        int shownCount = 0;
        int nullCount = 0;
        
        Debug.Log($"[AttunementPanel] Total attunements in array: {totalItems}");
        
        // Show/hide items based on tab
        foreach (var att in attunements)
        {
            if (att.item == null)
            {
                nullCount++;
                Debug.LogWarning($"[AttunementPanel] Item is NULL for: {att.id}");
                continue;
            }
            
            bool show = tab == TabType.All ||
                (tab == TabType.Matrix && att.mode == MarkerMode.Matrix) ||
                (tab == TabType.Recursion && att.mode == MarkerMode.Recursion) ||
                (tab == TabType.Infinity && att.mode == MarkerMode.Infinity);
            
            att.item.SetActive(show);
            
            if (show)
            {
                shownCount++;
                Debug.Log($"[AttunementPanel] SHOWING: {att.id} ({att.item.name}) - active: {att.item.activeInHierarchy}, pos: {att.item.transform.position}");
                UpdateItemVisual(att);
            }
        }
        
        Debug.Log($"[AttunementPanel] Tab {tab}: Showing {shownCount} items, {nullCount} null references");
        
        // Update info panel
        ShowTabInfo();
    }
    
    private void UpdateItemVisual(AttunementInfo att)
    {
        if (att.item == null) return;
        
        bool unlocked = IsUnlocked(att.id);
        bool equipped = IsEquipped(att.id);
        
        // Grey out if locked
        var images = att.item.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            img.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.7f);
        }
        
        // Highlight border if equipped
        var border = FindRecursive(att.item.transform, "Border") ?? FindRecursive(att.item.transform, "GradeFrame");
        if (border != null)
        {
            var img = border.GetComponent<Image>();
            if (img != null)
            {
                img.color = equipped ? Color.yellow : (unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f));
            }
        }
    }
    
    private bool IsUnlocked(string id)
    {
        if (!SaveManager.IsInitialized) return true; // Default unlocked for testing
        return SaveManager.Instance.Progression.IsAttunementUnlocked(id);
    }
    
    private bool IsEquipped(string id)
    {
        if (!SaveManager.IsInitialized) return false;
        foreach (var mode in new[] { MarkerMode.Matrix, MarkerMode.Recursion, MarkerMode.Infinity })
        {
            if (SaveManager.Instance.GetEquippedAttunement(mode) == id) return true;
        }
        return false;
    }
    
    #endregion
    
    #region Info Display
    
    private void ShowTabInfo()
    {
        string title = currentTab switch
        {
            TabType.All => "All Attunements",
            TabType.Matrix => "Matrix Attunements",
            TabType.Recursion => "Recursion Attunements",
            TabType.Infinity => "Infinity Attunements",
            _ => "Attunements"
        };
        
        string desc = currentTab switch
        {
            TabType.All => "View all attunements.\nSelect one for details.",
            TabType.Matrix => "Enhance Matrix markers.\nOne active at a time.",
            TabType.Recursion => "Enhance Recursion markers.\nOne active at a time.",
            TabType.Infinity => "Coming soon.",
            _ => ""
        };
        
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = desc;
    }
    
    private void OnAttunementClicked(AttunementInfo att)
    {
        if (titleText != null) titleText.text = att.displayName;
        if (descriptionText != null) descriptionText.text = att.description;
        
        Debug.Log($"[AttunementPanel] Clicked: {att.displayName} (unlocked: {IsUnlocked(att.id)}, equipped: {IsEquipped(att.id)})");
    }
    
    private void RefreshShards()
    {
        if (shardsText == null) return;
        int shards = SaveManager.IsInitialized ? SaveManager.Instance.AxiomShards : 0;
        shardsText.text = $"{shards:N0}";
    }
    
    #endregion
    
    #region Actions
    
    private void Close()
    {
        if (HubUIManager.Instance != null)
            HubUIManager.Instance.CloseAllPanels();
        else
            gameObject.SetActive(false);
    }
    
    #endregion
}
