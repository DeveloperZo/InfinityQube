using UnityEngine;
using System.Collections;
using static Enumerations;

/// <summary>
/// Handles face painting functionality for tiles that can paint cube faces
/// </summary>
public class TileFacePainting
{
    #region Configuration
    private bool canPaintCubes = false;
    private FaceStatus paintStatus = FaceStatus.None;
    private Color paintColor = Color.red;
    private int paintDuration = 3; // -1 for permanent
    private bool paintOnLanding = true;
    private bool paintOnExit = false;
    #endregion

    #region Runtime State
    private Transform parentTransform;
    private Tile parentTile;
    private bool enableDebugLogs;
    #endregion

    #region Properties
    public bool CanPaintCubes => canPaintCubes;
    public FaceStatus PaintStatus => paintStatus;
    public Color PaintColor => paintColor;
    public int PaintDuration => paintDuration;
    public bool PaintOnLanding => paintOnLanding;
    public bool PaintOnExit => paintOnExit;
    #endregion

    #region Constructor
    public TileFacePainting(Transform tileTransform, Tile tile, bool debugLogs = false)
    {
        parentTransform = tileTransform;
        parentTile = tile;
        enableDebugLogs = debugLogs;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Sets up the tile to paint cubes with specified parameters
    /// </summary>
    public void SetupFacePainting(FaceStatus status, Color color, int duration = -1, bool onLanding = true, bool onExit = false)
    {
        canPaintCubes = true;
        paintStatus = status;
        paintColor = color;
        paintDuration = duration;
        paintOnLanding = onLanding;
        paintOnExit = onExit;

        // Register with FacePaintingManager
        FacePaintingManager facePaintingManager = Object.FindFirstObjectByType<FacePaintingManager>();
        if (facePaintingManager != null)
        {
            facePaintingManager.RegisterFacePaintingTile(parentTile);
        }

        DebugLog($"Tile {parentTransform.name} set up to paint cubes with {status} status");
    }

    /// <summary>
    /// Tries to paint a cube when it lands on this tile
    /// </summary>
    public void TryPaintCube(CubeManager cube)
    {
        if (!canPaintCubes || cube == null || paintStatus == FaceStatus.None) return;

        if (paintOnLanding)
        {
            PaintCube(cube);
        }
    }

    /// <summary>
    /// Tries to paint a cube when it exits this tile
    /// </summary>
    public void TryPaintCubeOnExit(CubeManager cube)
    {
        if (!canPaintCubes || cube == null || paintStatus == FaceStatus.None) return;

        if (paintOnExit)
        {
            PaintCube(cube);
        }
    }

    /// <summary>
    /// Quick setup for corruption painting
    /// </summary>
    public void SetupCorruptionPainting(int duration = 3)
    {
        SetupFacePainting(FaceStatus.InfinityFace, Color.black, duration);
    }

    /// <summary>
    /// Quick setup for enhancement painting
    /// </summary>
    public void SetupEnhancementPainting(int duration = 3)
    {
        SetupFacePainting(FaceStatus.MatrixFace, Color.blue, duration);
    }

    /// <summary>
    /// Disables face painting functionality
    /// </summary>
    public void DisableFacePainting()
    {
        canPaintCubes = false;
        paintStatus = FaceStatus.None;
        
        // Unregister from FacePaintingManager
        FacePaintingManager facePaintingManager = Object.FindFirstObjectByType<FacePaintingManager>();
        if (facePaintingManager != null)
        {
            facePaintingManager.UnregisterFacePaintingTile(parentTile);
        }
    }

    /// <summary>
    /// Notify FacePaintingManager of cube movement coordination
    /// </summary>
    public void NotifyFacePaintingManager(CubeManager cube, Vector2Int pos)
    {
        if (!canPaintCubes) return;
        
        FacePaintingManager facePaintingManager = Object.FindFirstObjectByType<FacePaintingManager>();
        if (facePaintingManager != null)
        {
            facePaintingManager.OnCubeMoved(cube, pos, pos); // Update tracking
        }
    }

    /// <summary>
    /// Updates configuration for transformed tiles
    /// </summary>
    public void UpdateForTransformedState(bool isBlackened)
    {
        if (isBlackened)
        {
            // Corrupted tiles paint cubes black
            paintColor = Color.black;
            paintStatus = FaceStatus.InfinityFace;
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Paints the cube's down-facing face
    /// </summary>
    private void PaintCube(CubeManager cube)
    {
        // Paint the cube's currently down-facing face
        cube.PaintCurrentDownFace(paintStatus, paintColor, paintDuration);

        // Visual feedback effect
        CreatePaintEffect(cube.transform.position);

        DebugLog($"Tile {parentTransform.name} painted {cube.name} with {paintStatus} status");
    }

    /// <summary>
    /// Creates visual paint effect
    /// </summary>
    private void CreatePaintEffect(Vector3 position)
    {
        // Create a simple particle effect or visual feedback
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "PaintEffect";
        effect.transform.position = position + Vector3.up * 0.5f;
        effect.transform.localScale = Vector3.one * 0.3f;

        // Remove collider
        Object.Destroy(effect.GetComponent<Collider>());

        // Set color and make it fade
        Renderer renderer = effect.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = paintColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", paintColor * 0.5f);
        renderer.material = mat;

        // Animate and destroy
        parentTile.StartCoroutine(AnimatePaintEffect(effect));
    }

    /// <summary>
    /// Animates the paint effect
    /// </summary>
    private IEnumerator AnimatePaintEffect(GameObject effect)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 startPos = effect.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Scale up and fade out
            effect.transform.localScale = Vector3.Lerp(startScale, startScale * 2f, t);
            effect.transform.position = Vector3.Lerp(startPos, startPos + Vector3.up * 0.5f, t);

            // Fade material
            Renderer renderer = effect.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1f - t;
                renderer.material.color = color;
            }

            yield return null;
        }

        Object.Destroy(effect);
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TileFacePainting] {message}");
        }
    }
    #endregion
}
