using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Handles face painting system for cubes.
/// Extracted from CubeManager as part of SRP refactoring.
/// Manages face statuses, colors, durations, charges, and visual indicators.
/// </summary>
public class CubeFacePainter
{
    #region References
    private readonly CubeManager cube;
    private readonly FaceStatus[] faceStatuses;
    private readonly Color[] faceColors;
    private readonly int[] faceDurations;
    private readonly int[] faceCharges;
    private readonly GameObject[] faceIndicators;
    private readonly CubeFace[] currentFaceMapping;
    private readonly bool showFaceIndicators;
    private bool enableDebugLogs;
    #endregion

    #region Constructor
    public CubeFacePainter(
        CubeManager cubeManager,
        FaceStatus[] statuses,
        Color[] colors,
        int[] durations,
        int[] charges,
        GameObject[] indicators,
        CubeFace[] faceMapping,
        bool showIndicators,
        bool debugLogs)
    {
        cube = cubeManager;
        faceStatuses = statuses;
        faceColors = colors;
        faceDurations = durations;
        faceCharges = charges;
        faceIndicators = indicators;
        currentFaceMapping = faceMapping;
        showFaceIndicators = showIndicators;
        enableDebugLogs = debugLogs;
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes face system with default values.
    /// </summary>
    public void InitializeFaceSystem()
    {
        for (int i = 0; i < 4; i++)
        {
            faceStatuses[i] = FaceStatus.None;
            faceColors[i] = Color.white;
            faceDurations[i] = 0;
            faceCharges[i] = 0;
        }

        if (showFaceIndicators)
        {
            CreateFaceIndicators();
        }
    }

    /// <summary>
    /// Initializes face mapping to default positions.
    /// </summary>
    public void InitializeFaceMapping()
    {
        currentFaceMapping[0] = CubeFace.Bottom;
        currentFaceMapping[1] = CubeFace.Top;
        currentFaceMapping[2] = CubeFace.Front;
        currentFaceMapping[3] = CubeFace.Back;

        DebugLog($"Face mapping initialized for cube at ({cube.position.x}, {cube.position.y})");
    }
    #endregion

    #region Face Queries
    /// <summary>
    /// Gets the face currently in the down position.
    /// </summary>
    public CubeFace GetCurrentDownFace()
    {
        return currentFaceMapping[0];
    }

    /// <summary>
    /// Gets the face currently in the top position.
    /// </summary>
    public CubeFace GetTopFace()
    {
        return currentFaceMapping[1];
    }

    /// <summary>
    /// Gets the status of the currently active (down) face.
    /// </summary>
    public FaceStatus GetActiveFaceStatus()
    {
        CubeFace downFace = GetCurrentDownFace();
        return faceStatuses[(int)downFace];
    }

    /// <summary>
    /// Predicts which face status will be active after N moves.
    /// </summary>
    public FaceStatus GetPredictedFaceStatus(int movesAhead = 1)
    {
        if (movesAhead <= 0) return GetActiveFaceStatus();
        
        int[] sourceIndices = { 0, 3, 1, 2 };
        int sourceIndex = sourceIndices[movesAhead % 4];
        
        CubeFace sourceFace = currentFaceMapping[sourceIndex];
        return faceStatuses[(int)sourceFace];
    }

    /// <summary>
    /// Checks if a painted face will touch the grid in the specified number of moves.
    /// </summary>
    public bool WillPaintedFaceTouchGrid(int movesAhead = 1)
    {
        FaceStatus predictedStatus = GetPredictedFaceStatus(movesAhead);
        return predictedStatus != FaceStatus.None;
    }

    /// <summary>
    /// Checks if the active face has a specific status.
    /// </summary>
    public bool HasActiveFaceStatus(FaceStatus status)
    {
        return GetActiveFaceStatus() == status;
    }

    /// <summary>
    /// Gets status for a specific face.
    /// </summary>
    public FaceStatus GetFaceStatus(CubeFace face)
    {
        return faceStatuses[(int)face];
    }

    /// <summary>
    /// Gets remaining duration for a specific face.
    /// </summary>
    public int GetFaceDuration(CubeFace face)
    {
        return faceDurations[(int)face];
    }

    /// <summary>
    /// Gets remaining charges for the active (down) face.
    /// </summary>
    public int GetActiveFaceCharges()
    {
        CubeFace downFace = GetCurrentDownFace();
        return faceCharges[(int)downFace];
    }

    /// <summary>
    /// Gets remaining charges for a specific face.
    /// </summary>
    public int GetFaceCharges(CubeFace face)
    {
        return faceCharges[(int)face];
    }

    /// <summary>
    /// Gets the effective cube type based on active face status.
    /// </summary>
    public CubeType GetEffectiveType()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();

        switch (activeStatus)
        {
            case FaceStatus.InfinityFace:
                return CubeType.Infinity;
            case FaceStatus.MatrixFace:
                return CubeType.Matrix;
            case FaceStatus.RecursionFace:
                return CubeType.Recursion;
            default:
                return cube.type;
        }
    }

    /// <summary>
    /// Checks if the cube can be captured based on face status.
    /// </summary>
    public bool CanBeCaptured()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();
        switch (activeStatus)
        {
            case FaceStatus.InfinityFace:
                return false;
            default:
                return cube.type != CubeType.Infinity;
        }
    }

    /// <summary>
    /// Checks if cube should create a detonation effect.
    /// </summary>
    public bool ShouldCreateDetonation()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();
        return activeStatus == FaceStatus.MatrixFace || cube.type == CubeType.Matrix;
    }

    /// <summary>
    /// Checks if the current down face has corrupted (Infinity) status.
    /// </summary>
    public bool HasCorruptedDownFace()
    {
        CubeFace downFace = GetCurrentDownFace();
        return faceStatuses[(int)downFace] == FaceStatus.InfinityFace;
    }
    #endregion

    #region Face Painting
    /// <summary>
    /// Paints a specific face with status, color, duration, and charges.
    /// </summary>
    public void PaintFace(CubeFace face, FaceStatus status, Color color, int duration = -1, int charges = 1)
    {
        int faceIndex = (int)face;
        faceStatuses[faceIndex] = status;
        faceColors[faceIndex] = color;
        faceDurations[faceIndex] = duration;
        faceCharges[faceIndex] = charges;
        
        if (faceIndicators[faceIndex] != null)
            faceIndicators[faceIndex].SetActive(true);
            
        UpdateFaceVisuals();
        
        DebugLog($"Painted {face} of cube at ({cube.position.x}, {cube.position.y}) with {status} status, duration: {duration}, charges: {charges}");
    }

    /// <summary>
    /// Paints the current down face.
    /// </summary>
    public void PaintCurrentDownFace(FaceStatus status, Color color, int duration = -1)
    {
        CubeFace downFace = GetCurrentDownFace();
        PaintFace(downFace, status, color, duration);
    }

    /// <summary>
    /// Sets face status with automatic color selection.
    /// </summary>
    public void SetFaceStatus(CubeFace face, FaceStatus status, int duration = -1)
    {
        Color color = status == FaceStatus.InfinityFace ? Color.red :
                     status == FaceStatus.MatrixFace ? Color.blue : Color.white;
        PaintFace(face, status, color, duration);
    }

    /// <summary>
    /// Consumes one charge from the active face.
    /// Returns true if charge was consumed.
    /// </summary>
    public bool ConsumeActiveFaceCharge()
    {
        CubeFace downFace = GetCurrentDownFace();
        int faceIndex = (int)downFace;
        
        if (faceStatuses[faceIndex] == FaceStatus.None)
            return false;
        
        if (faceCharges[faceIndex] <= 0)
            return false;
        
        faceCharges[faceIndex]--;
        DebugLog($"Face {downFace} charge consumed. Remaining: {faceCharges[faceIndex]}");
        
        if (faceCharges[faceIndex] <= 0)
        {
            FaceStatus oldStatus = faceStatuses[faceIndex];
            faceStatuses[faceIndex] = FaceStatus.None;
            faceColors[faceIndex] = Color.white;
            if (faceIndicators[faceIndex] != null)
                faceIndicators[faceIndex].SetActive(false);
            UpdateFaceVisuals();
            DebugLog($"Face {downFace} unpainted - charges exhausted (was {oldStatus})");
        }
        
        return true;
    }

    /// <summary>
    /// Clears all face statuses.
    /// </summary>
    public void ClearAllFaces()
    {
        for (int i = 0; i < 4; i++)
        {
            faceStatuses[i] = FaceStatus.None;
            faceColors[i] = Color.white;
            faceDurations[i] = 0;
            faceCharges[i] = 0;
        }
        UpdateFaceVisuals();
        DebugLog($"Cleared all face statuses");
    }
    #endregion

    #region Face Rotation
    /// <summary>
    /// Rotates face mapping after cube moves forward.
    /// </summary>
    public void RotateFaceMapping()
    {
        CubeFace temp = currentFaceMapping[0];
        currentFaceMapping[0] = currentFaceMapping[3];
        currentFaceMapping[3] = currentFaceMapping[1];
        currentFaceMapping[1] = currentFaceMapping[2];
        currentFaceMapping[2] = temp;

        DebugLog($"Face mapping rotated: Bottom={currentFaceMapping[0]}, Top={currentFaceMapping[1]}, Front={currentFaceMapping[2]}, Back={currentFaceMapping[3]}");
        UpdateFaceVisuals();
    }

    /// <summary>
    /// Processes face durations, removing expired statuses.
    /// </summary>
    public void ProcessFaceDurations()
    {
        bool anyChanged = false;
        for (int i = 0; i < 4; i++)
        {
            if (faceDurations[i] > 0)
            {
                faceDurations[i]--;
                if (faceDurations[i] == 0)
                {
                    faceStatuses[i] = FaceStatus.None;
                    faceColors[i] = Color.white;
                    anyChanged = true;
                    if (faceIndicators[i] != null)
                        faceIndicators[i].SetActive(false);
                    DebugLog($"Face {(CubeFace)i} paint status expired");
                }
            }
        }

        if (anyChanged)
        {
            UpdateFaceVisuals();
        }
    }
    #endregion

    #region Face Indicators
    private void CreateFaceIndicators()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = $"FaceIndicator_{(CubeFace)i}_{cube.position.x}_{cube.position.y}";
            indicator.transform.SetParent(cube.transform);

            PositionFaceIndicator(indicator, (CubeFace)i);

            Renderer renderer = indicator.GetComponent<Renderer>();
            Material mat = CreateFaceIndicatorMaterial();
            renderer.material = mat;

            Object.Destroy(indicator.GetComponent<Collider>());

            indicator.SetActive(false);
            faceIndicators[i] = indicator;
        }
    }

    private void PositionFaceIndicator(GameObject indicator, CubeFace originalFace)
    {
        float offset = 0.55f;
        Vector3 scale = new Vector3(1f, 1f, 1f);

        switch (originalFace)
        {
            case CubeFace.Bottom:
                indicator.transform.localPosition = new Vector3(0, -offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(90, 180, 0);
                break;
            case CubeFace.Top:
                indicator.transform.localPosition = new Vector3(0, offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(-90, 180, 0);
                break;
            case CubeFace.Front:
                indicator.transform.localPosition = new Vector3(0, 0, offset);
                indicator.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case CubeFace.Back:
                indicator.transform.localPosition = new Vector3(0, 0, -offset);
                indicator.transform.localRotation = Quaternion.Euler(0, 0, 0);
                break;
        }

        indicator.transform.localScale = scale;
    }

    private Material CreateFaceIndicatorMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1, 1, 1, 0.8f);

        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    private void UpdateFaceVisuals()
    {
        UpdateFaceIndicatorPositions();
    }

    private void UpdateFaceIndicatorPositions()
    {
        if (!showFaceIndicators || faceIndicators == null) return;

        float offset = 0.55f;

        for (int i = 0; i < 4; i++)
        {
            if (faceIndicators[i] == null) continue;

            CubeFace originalFace = (CubeFace)i;
            FacePosition currentPosition = GetFacePosition(originalFace);

            Vector3 newPosition = Vector3.one;
            Quaternion newRotation = Quaternion.identity;

            switch (currentPosition)
            {
                case FacePosition.Down:
                    newPosition = new Vector3(0, -offset, 0);
                    newRotation = Quaternion.Euler(90, 180, 0);
                    break;
                case FacePosition.Up:
                    newPosition = new Vector3(0, offset, 0);
                    newRotation = Quaternion.Euler(-270, 180, 0);
                    break;
                case FacePosition.Forward:
                    newPosition = new Vector3(0, 0, offset);
                    newRotation = Quaternion.Euler(0, 180, 0);
                    break;
                case FacePosition.Back:
                    newPosition = new Vector3(0, 0, -offset);
                    newRotation = Quaternion.Euler(0, 0, 0);
                    break;
            }

            faceIndicators[i].transform.localPosition = newPosition;
            faceIndicators[i].transform.localRotation = newRotation;
            faceIndicators[i].GetComponent<Renderer>().material.color = faceColors[i];
        }
    }

    private FacePosition GetFacePosition(CubeFace originalFace)
    {
        for (int i = 0; i < 4; i++)
        {
            if (currentFaceMapping[i] == originalFace)
            {
                switch (i)
                {
                    case 0: return FacePosition.Down;
                    case 1: return FacePosition.Up;
                    case 2: return FacePosition.Forward;
                    case 3: return FacePosition.Back;
                    default: return FacePosition.Down;
                }
            }
        }
        return FacePosition.Down;
    }

    /// <summary>
    /// Cleans up face indicator GameObjects.
    /// </summary>
    public void CleanupIndicators()
    {
        for (int i = 0; i < faceIndicators.Length; i++)
        {
            if (faceIndicators[i] != null)
            {
                Object.Destroy(faceIndicators[i]);
                faceIndicators[i] = null;
            }
        }
    }
    #endregion

    #region Debug
    /// <summary>
    /// Test paints a face with status.
    /// </summary>
    public void TestPaintFace(CubeFace face, FaceStatus status)
    {
        Color color = status == FaceStatus.InfinityFace ? Color.black : Color.blue;
        PaintFace(face, status, color, 5);
        DebugLog($"Test painted {face} with {status} status");
    }

    /// <summary>
    /// Shows all faces with different test colors.
    /// </summary>
    public void DebugShowAllFaces()
    {
        if (!showFaceIndicators) return;

        Color[] testColors = { Color.red, Color.green, Color.blue, Color.yellow };
        FaceStatus[] testStatuses = { FaceStatus.InfinityFace, FaceStatus.MatrixFace, FaceStatus.RecursionFace, FaceStatus.RecursionFace };

        for (int i = 0; i < 4; i++)
        {
            PaintFace((CubeFace)i, testStatuses[i], testColors[i], -1);
        }

        DebugLog($"Debug: All faces painted with different colors. Current down face: {GetCurrentDownFace()}");
    }

    /// <summary>
    /// Prints face mapping to debug log.
    /// </summary>
    public void DebugPrintFaceMapping()
    {
        DebugLog($"Face Mapping for cube at ({cube.position.x}, {cube.position.y}):");
        DebugLog($"  Bottom position: {currentFaceMapping[0]}");
        DebugLog($"  Top position: {currentFaceMapping[1]}");
        DebugLog($"  Front position: {currentFaceMapping[2]}");
        DebugLog($"  Back position: {currentFaceMapping[3]}");
        DebugLog($"  Current down face: {GetCurrentDownFace()}");
        DebugLog($"  Active face status: {GetActiveFaceStatus()}");
    }

    public void SetDebugLogs(bool enabled)
    {
        enableDebugLogs = enabled;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CubeFacePainter] {message}");
        }
    }
    #endregion
}
