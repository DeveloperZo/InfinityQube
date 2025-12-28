using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Creates a simple square beam visual effect for cube markers.
/// Beam is tile-sized (1x1) and 3x cube height, with a strobe effect.
/// </summary>
public class CubeMarkerStrobeEffect : MonoBehaviour
{
    #region Configuration
    
    [Header("Strobe Settings")]
    [SerializeField] private float strobeSpeed = 2f; // Cycles per second
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private bool useLessDramaticEffect = false; // If true, uses lower alpha and slower strobe
    
    [Header("Beam Settings")]
    [SerializeField] private int markerSize = 1; // Size of the marker area (1 = single tile, 2 = 2x2, 3 = 3x3)
    
    #endregion
    
    #region Runtime State
    
    private GameObject beamObject;
    private GameObject iconObject;
    private Material beamMaterial;
    private Color baseColor;
    private float strobeTimer = 0f;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        CreateBeam();
    }
    
    private void Update()
    {
        UpdateStrobe();
    }
    
    private void OnDestroy()
    {
        if (beamMaterial != null)
        {
            Destroy(beamMaterial);
        }
        if (iconObject != null)
        {
            Destroy(iconObject);
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Initializes the strobe effect with a color and marker size
    /// </summary>
    public void Initialize(Color color, int size = 1)
    {
        baseColor = color;
        markerSize = size;
        
        // Recreate beam with new size if already created
        if (beamObject != null)
        {
            Destroy(beamObject);
            CreateBeam();
        }
        
        if (beamMaterial != null)
        {
            float alpha = useLessDramaticEffect ? 0.3f : 0.6f;
            beamMaterial.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            beamMaterial.SetColor("_EmissionColor", baseColor * (useLessDramaticEffect ? 1.0f : 1.5f));
        }
    }
    
    /// <summary>
    /// Enables or disables the beam effect
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (beamObject != null)
        {
            beamObject.SetActive(enabled);
        }
        this.enabled = enabled;
    }
    
    /// <summary>
    /// Shows an icon above this tile (for triggered markers)
    /// </summary>
    public void ShowIcon(Color iconColor)
    {
        // Disable beam when showing icon
        SetEnabled(false);
        
        // Destroy existing icon if any
        if (iconObject != null)
        {
            Destroy(iconObject);
        }
        
        // Create simple visible icon above tile (quad facing up)
        iconObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        iconObject.name = "CubeMarkerIcon";
        iconObject.transform.SetParent(transform);
        iconObject.transform.localPosition = new Vector3(0, 5f, 0); // Higher above tile for better visibility
        iconObject.transform.localRotation = Quaternion.Euler(90, 0, 0); // Face up
        iconObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f); // Visible size
        
        // Remove collider
        Destroy(iconObject.GetComponent<Collider>());
        
        Material iconMat = new Material(Shader.Find("Standard"));
        iconMat.EnableKeyword("_EMISSION");
        iconMat.SetFloat("_Metallic", 0.2f);
        iconMat.SetFloat("_Smoothness", 0.8f);
        iconMat.SetColor("_EmissionColor", iconColor * 2f);
        iconMat.color = iconColor;
        
        Renderer renderer = iconObject.GetComponent<Renderer>();
        renderer.material = iconMat;
    }
    
    /// <summary>
    /// Hides the icon
    /// </summary>
    public void HideIcon()
    {
        if (iconObject != null)
        {
            Destroy(iconObject);
            iconObject = null;
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private void CreateBeam()
    {
        // Create square beam mesh - single tile size only
        beamObject = new GameObject("CubeMarkerBeam");
        beamObject.transform.SetParent(transform);
        beamObject.transform.localPosition = Vector3.zero;
        // Single tile size (accounting for parent scale ~0.333)
        beamObject.transform.localScale = new Vector3(1f, 1f, 1f);
        MeshFilter meshFilter = beamObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = beamObject.AddComponent<MeshRenderer>();
        
        Mesh beamMesh = CreateSquareBeamMesh();
        meshFilter.mesh = beamMesh;
        
        // Create material
        beamMaterial = new Material(Shader.Find("Standard"));
        beamMaterial.EnableKeyword("_EMISSION");
        beamMaterial.SetFloat("_Metallic", 0.1f);
        beamMaterial.SetFloat("_Smoothness", 0.9f);
        beamMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        beamMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        beamMaterial.SetInt("_ZWrite", 0);
        beamMaterial.DisableKeyword("_ALPHATEST_ON");
        beamMaterial.EnableKeyword("_ALPHABLEND_ON");
        beamMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        beamMaterial.renderQueue = 3000; // Transparent queue
        
        Color colorToUse = (baseColor != default(Color)) ? baseColor : Color.cyan;
        beamMaterial.SetColor("_EmissionColor", colorToUse * 1.5f);
        beamMaterial.color = new Color(colorToUse.r, colorToUse.g, colorToUse.b, 0.6f);
        meshRenderer.material = beamMaterial;
    }
    
    private Mesh CreateSquareBeamMesh()
    {
        Mesh mesh = new Mesh();
        // Build mesh: single tile-sized (1x1) width/depth, 3x cube height (3 units tall)
        // Parent tile scale is ~0.333 on X/Z, so we need to build mesh at 3x to get 1x final size
        float halfSize = 0.5f; // Single tile is 1 unit, so half is 0.5 (will be scaled by parent)
        float topY = 3.0f; // 3x cube height
        float bottomY = 0.1f; // Slightly above tile surface
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Bottom square (at tile level)
        int bottomStart = vertices.Count;
        vertices.Add(new Vector3(-halfSize, bottomY, -halfSize));
        vertices.Add(new Vector3(halfSize, bottomY, -halfSize));
        vertices.Add(new Vector3(halfSize, bottomY, halfSize));
        vertices.Add(new Vector3(-halfSize, bottomY, halfSize));
        uvs.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
        
        // Top square
        int topStart = vertices.Count;
        vertices.Add(new Vector3(-halfSize, topY, -halfSize));
        vertices.Add(new Vector3(halfSize, topY, -halfSize));
        vertices.Add(new Vector3(halfSize, topY, halfSize));
        vertices.Add(new Vector3(-halfSize, topY, halfSize));
        uvs.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
        
        // Front face (positive Z)
        int frontStart = vertices.Count;
        vertices.Add(new Vector3(-halfSize, bottomY, halfSize));
        vertices.Add(new Vector3(halfSize, bottomY, halfSize));
        vertices.Add(new Vector3(halfSize, topY, halfSize));
        vertices.Add(new Vector3(-halfSize, topY, halfSize));
        uvs.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
        
        // Back face (negative Z)
        int backStart = vertices.Count;
        vertices.Add(new Vector3(halfSize, bottomY, -halfSize));
        vertices.Add(new Vector3(-halfSize, bottomY, -halfSize));
        vertices.Add(new Vector3(-halfSize, topY, -halfSize));
        vertices.Add(new Vector3(halfSize, topY, -halfSize));
        uvs.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
        
        // Right face (positive X)
        int rightStart = vertices.Count;
        vertices.Add(new Vector3(halfSize, bottomY, halfSize));
        vertices.Add(new Vector3(halfSize, bottomY, -halfSize));
        vertices.Add(new Vector3(halfSize, topY, -halfSize));
        vertices.Add(new Vector3(halfSize, topY, halfSize));
        uvs.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
        
        // Left face (negative X)
        int leftStart = vertices.Count;
        vertices.Add(new Vector3(-halfSize, bottomY, -halfSize));
        vertices.Add(new Vector3(-halfSize, bottomY, halfSize));
        vertices.Add(new Vector3(-halfSize, topY, halfSize));
        vertices.Add(new Vector3(-halfSize, topY, -halfSize));
        uvs.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
        
        // Create triangles for each face
        // Bottom face
        triangles.AddRange(new int[] { bottomStart, bottomStart + 1, bottomStart + 2, bottomStart, bottomStart + 2, bottomStart + 3 });
        
        // Top face
        triangles.AddRange(new int[] { topStart, topStart + 2, topStart + 1, topStart, topStart + 3, topStart + 2 });
        
        // Front face
        triangles.AddRange(new int[] { frontStart, frontStart + 1, frontStart + 2, frontStart, frontStart + 2, frontStart + 3 });
        
        // Back face
        triangles.AddRange(new int[] { backStart, backStart + 1, backStart + 2, backStart, backStart + 2, backStart + 3 });
        
        // Right face
        triangles.AddRange(new int[] { rightStart, rightStart + 1, rightStart + 2, rightStart, rightStart + 2, rightStart + 3 });
        
        // Left face
        triangles.AddRange(new int[] { leftStart, leftStart + 1, leftStart + 2, leftStart, leftStart + 2, leftStart + 3 });
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private void UpdateStrobe()
    {
        float effectiveSpeed = useLessDramaticEffect ? strobeSpeed * 0.5f : strobeSpeed;
        strobeTimer += Time.deltaTime * effectiveSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(strobeTimer * Mathf.PI * 2f) + 1f) / 2f);
        
        Color currentColor = baseColor;
        float baseAlpha = useLessDramaticEffect ? 0.3f : 0.6f;
        currentColor.a = alpha * baseAlpha;
        
        float emissionMultiplier = useLessDramaticEffect ? 1.0f : 1.5f;
        if (beamMaterial != null)
        {
            beamMaterial.color = currentColor;
            beamMaterial.SetColor("_EmissionColor", currentColor * (emissionMultiplier + alpha * 0.5f));
        }
    }
    
    #endregion
}
