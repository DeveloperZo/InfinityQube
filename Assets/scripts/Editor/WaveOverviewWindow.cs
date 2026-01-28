using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using static Enumerations;

/// <summary>
/// Editor window that displays grid visualizations for all waves in a stage.
/// Allows side-by-side comparison and screenshot export of wave patterns.
/// </summary>
public class WaveOverviewWindow : EditorWindow
{
    private StageData selectedStage;
    private Vector2 scrollPosition;
    private int wavesPerRow = 3;
    
    // Grid cell rendering
    private const float CELL_SIZE = 18f;
    private const float WAVE_PADDING = 15f;
    private const float HEADER_HEIGHT = 25f;
    
    // Clipboard helper for Windows
    #if UNITY_EDITOR_WIN
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(System.IntPtr hWndNewOwner);
    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();
    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();
    [DllImport("user32.dll")]
    private static extern System.IntPtr SetClipboardData(uint uFormat, System.IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern System.IntPtr GlobalAlloc(uint uFlags, System.UIntPtr dwBytes);
    [DllImport("kernel32.dll")]
    private static extern System.IntPtr GlobalLock(System.IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(System.IntPtr hMem);
    
    private const uint CF_DIB = 8;
    private const uint GMEM_MOVEABLE = 0x0002;
    #endif
    
    // Cached textures for screenshot rendering
    private Dictionary<CubeType, Color> cubeColors = new Dictionary<CubeType, Color>
    {
        { CubeType.Unit, new Color(0.2f, 0.8f, 0.2f) },      // Green
        { CubeType.Matrix, new Color(0.2f, 0.4f, 0.9f) },    // Blue
        { CubeType.Recursion, new Color(0.9f, 0.8f, 0.1f) }, // Yellow
        { CubeType.Infinity, new Color(0.9f, 0.2f, 0.2f) }   // Red
    };
    
    [MenuItem("Tools/Infinity Qube/Wave Overview")]
    public static void ShowWindow()
    {
        var window = GetWindow<WaveOverviewWindow>("Wave Overview");
        window.minSize = new Vector2(600, 400);
    }
    
    private void OnEnable()
    {
        // Try to auto-select stage if one is selected in project
        if (Selection.activeObject is StageData stage)
        {
            selectedStage = stage;
        }
    }
    
    private void OnSelectionChange()
    {
        // Auto-update when selecting a stage in project window
        if (Selection.activeObject is StageData stage)
        {
            selectedStage = stage;
            Repaint();
        }
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        
        // Stage selector
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Stage:", GUILayout.Width(50));
        selectedStage = (StageData)EditorGUILayout.ObjectField(selectedStage, typeof(StageData), false);
        
        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        
        if (selectedStage == null)
        {
            EditorGUILayout.HelpBox("Select a StageData asset to view all its wave configurations.", MessageType.Info);
            return;
        }
        
        if (selectedStage.waveConfigurations == null || selectedStage.waveConfigurations.Count == 0)
        {
            EditorGUILayout.HelpBox("Selected stage has no wave configurations.", MessageType.Warning);
            return;
        }
        
        // Display settings
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Layout:", GUILayout.Width(50));
        wavesPerRow = EditorGUILayout.IntSlider(wavesPerRow, 1, 6);
        EditorGUILayout.EndHorizontal();
        
        // Copy to clipboard controls
        EditorGUILayout.Space(5);
        DrawClipboardControls();
        
        // Wave count info
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"Stage {selectedStage.stageNumber:D2}: {selectedStage.stageName} | Waves: {selectedStage.waveConfigurations.Count}", EditorStyles.boldLabel);
        
        // Scrollable wave grid
        EditorGUILayout.Space(5);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawWaveGrid();
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawClipboardControls()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("📋 Copy All Waves to Clipboard", GUILayout.Height(25)))
        {
            CopyStageOverviewToClipboard();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawWaveGrid()
    {
        int waveCount = selectedStage.waveConfigurations.Count;
        int currentColumn = 0;
        
        EditorGUILayout.BeginHorizontal();
        
        for (int i = 0; i < waveCount; i++)
        {
            WaveData wave = selectedStage.waveConfigurations[i];
            if (wave == null) continue;
            
            DrawWavePanel(wave, i);
            currentColumn++;
            
            if (currentColumn >= wavesPerRow && i < waveCount - 1)
            {
                currentColumn = 0;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawWavePanel(WaveData wave, int index)
    {
        float panelWidth = (CELL_SIZE * wave.GridWidth) + WAVE_PADDING * 2;
        float panelHeight = (CELL_SIZE * wave.GridHeight) + HEADER_HEIGHT + WAVE_PADDING * 2 + 30; // +30 for buttons
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(panelWidth), GUILayout.Height(panelHeight));
        
        // Header - use stage number and wave index
        int stageNum = selectedStage.stageNumber;
        int waveNum = wave.Index;
        string waveLabel = $"S{stageNum:D2}-W{waveNum:D2}";
        EditorGUILayout.LabelField($"{waveLabel} ({wave.GridWidth}x{wave.GridHeight})", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Cubes: {wave.CubesData?.Count ?? 0}", EditorStyles.miniLabel);
        
        // Draw grid
        DrawWaveGridCells(wave);
        
        // Action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select", GUILayout.Height(20)))
        {
            Selection.activeObject = wave;
            EditorGUIUtility.PingObject(wave);
        }
        if (GUILayout.Button("📋", GUILayout.Width(30), GUILayout.Height(20)))
        {
            CopyWaveToClipboard(wave, index);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawWaveGridCells(WaveData wave)
    {
        int gridWidth = Mathf.Max(1, wave.GridWidth);
        int gridHeight = Mathf.Max(1, wave.GridHeight);
        
        // Build lookup
        Dictionary<Vector2Int, CubeData> cubeMap = new Dictionary<Vector2Int, CubeData>();
        if (wave.CubesData != null)
        {
            foreach (var cube in wave.CubesData)
            {
                if (!cubeMap.ContainsKey(cube.position))
                    cubeMap[cube.position] = cube;
            }
        }
        
        // Draw grid (top to bottom, Y inverted for visual)
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Rect cellRect = GUILayoutUtility.GetRect(CELL_SIZE, CELL_SIZE, GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE));
                
                // Background
                Color bgColor = Color.gray * 0.3f;
                if (cubeMap.TryGetValue(pos, out CubeData cube))
                {
                    bgColor = cubeColors.ContainsKey(cube.type) ? cubeColors[cube.type] * 0.4f : Color.white * 0.4f;
                }
                EditorGUI.DrawRect(cellRect, bgColor);
                
                // Border
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.black * 0.5f);
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.black * 0.5f);
                
                // Label
                if (cubeMap.TryGetValue(pos, out CubeData cubeData))
                {
                    string label = GetCubeLabel(cubeData.type);
                    GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    style.normal.textColor = cubeColors.ContainsKey(cubeData.type) ? cubeColors[cubeData.type] : Color.white;
                    style.fontStyle = FontStyle.Bold;
                    style.fontSize = 10;
                    GUI.Label(cellRect, label, style);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private string GetCubeLabel(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit: return "U";
            case CubeType.Matrix: return "M";
            case CubeType.Recursion: return "R";
            case CubeType.Infinity: return "∞";
            default: return "?";
        }
    }
    
    #region Clipboard Methods
    
    private void CopyWaveToClipboard(WaveData wave, int index)
    {
        Texture2D texture = RenderWaveToTexture(wave, index);
        CopyTextureToClipboard(texture);
        DestroyImmediate(texture);
        
        string waveLabel = $"S{selectedStage.stageNumber:D2}-W{wave.Index:D2}";
        Debug.Log($"[WaveOverview] Copied {waveLabel} to clipboard");
    }
    
    private void CopyStageOverviewToClipboard()
    {
        Texture2D texture = RenderStageOverviewToTexture();
        CopyTextureToClipboard(texture);
        DestroyImmediate(texture);
        
        Debug.Log($"[WaveOverview] Copied Stage {selectedStage.stageNumber:D2} ({selectedStage.waveConfigurations.Count} waves) to clipboard");
    }
    
    private void CopyTextureToClipboard(Texture2D texture)
    {
        #if UNITY_EDITOR_WIN
        // Convert to BMP format for Windows clipboard (DIB)
        byte[] bmpData = EncodeToBMP(texture);
        
        // Skip BMP file header (14 bytes) to get DIB data
        int dibSize = bmpData.Length - 14;
        
        System.IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (System.UIntPtr)dibSize);
        if (hMem == System.IntPtr.Zero)
        {
            Debug.LogError("[WaveOverview] Failed to allocate memory for clipboard");
            return;
        }
        
        System.IntPtr pMem = GlobalLock(hMem);
        if (pMem != System.IntPtr.Zero)
        {
            Marshal.Copy(bmpData, 14, pMem, dibSize);
            GlobalUnlock(hMem);
            
            if (OpenClipboard(System.IntPtr.Zero))
            {
                EmptyClipboard();
                SetClipboardData(CF_DIB, hMem);
                CloseClipboard();
            }
            else
            {
                Debug.LogError("[WaveOverview] Failed to open clipboard");
            }
        }
        #else
        // Fallback: Save to temp file and notify user
        string tempPath = Path.Combine(Application.temporaryCachePath, "wave_clipboard.png");
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(tempPath, pngData);
        Debug.Log($"[WaveOverview] Image saved to: {tempPath} (clipboard copy only works on Windows)");
        EditorUtility.RevealInFinder(tempPath);
        #endif
    }
    
    #if UNITY_EDITOR_WIN
    private byte[] EncodeToBMP(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        
        // BMP row size must be multiple of 4 bytes
        int rowSize = ((width * 3 + 3) / 4) * 4;
        int pixelDataSize = rowSize * height;
        int fileSize = 14 + 40 + pixelDataSize; // File header + DIB header + pixel data
        
        byte[] bmp = new byte[fileSize];
        
        // BMP File Header (14 bytes)
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        WriteInt(bmp, 2, fileSize);
        WriteInt(bmp, 10, 54); // Pixel data offset
        
        // DIB Header (BITMAPINFOHEADER - 40 bytes)
        WriteInt(bmp, 14, 40); // Header size
        WriteInt(bmp, 18, width);
        WriteInt(bmp, 22, height);
        WriteShort(bmp, 26, 1); // Color planes
        WriteShort(bmp, 28, 24); // Bits per pixel
        WriteInt(bmp, 34, pixelDataSize);
        
        // Pixel data (bottom-up, BGR format)
        Color[] pixels = texture.GetPixels();
        int offset = 54;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * width + x];
                int idx = offset + y * rowSize + x * 3;
                bmp[idx] = (byte)(pixel.b * 255);     // Blue
                bmp[idx + 1] = (byte)(pixel.g * 255); // Green
                bmp[idx + 2] = (byte)(pixel.r * 255); // Red
            }
        }
        
        return bmp;
    }
    
    private void WriteInt(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }
    
    private void WriteShort(byte[] data, int offset, short value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
    #endif
    
    private Texture2D RenderWaveToTexture(WaveData wave, int index)
    {
        int cellSize = 24;
        int padding = 10;
        int headerHeight = 0;
        
        int width = (wave.GridWidth * cellSize) + (padding * 2);
        int height = (wave.GridHeight * cellSize) + (padding * 2) + headerHeight;
        
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // Fill background
        Color bgColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, bgColor);
            }
        }
        
        // Build cube lookup
        Dictionary<Vector2Int, CubeData> cubeMap = new Dictionary<Vector2Int, CubeData>();
        if (wave.CubesData != null)
        {
            foreach (var cube in wave.CubesData)
            {
                if (!cubeMap.ContainsKey(cube.position))
                    cubeMap[cube.position] = cube;
            }
        }
        
        // Draw grid cells
        for (int gy = 0; gy < wave.GridHeight; gy++)
        {
            for (int gx = 0; gx < wave.GridWidth; gx++)
            {
                int px = padding + (gx * cellSize);
                int py = padding + ((wave.GridHeight - 1 - gy) * cellSize); // Flip Y
                
                Vector2Int pos = new Vector2Int(gx, gy);
                Color cellColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                
                if (cubeMap.TryGetValue(pos, out CubeData cube))
                {
                    cellColor = cubeColors.ContainsKey(cube.type) ? cubeColors[cube.type] : Color.white;
                }
                
                // Fill cell
                for (int cy = 0; cy < cellSize - 1; cy++)
                {
                    for (int cx = 0; cx < cellSize - 1; cx++)
                    {
                        texture.SetPixel(px + cx, py + cy, cellColor);
                    }
                }
                
                // Draw border
                Color borderColor = Color.black;
                for (int i = 0; i < cellSize; i++)
                {
                    texture.SetPixel(px + i, py, borderColor);
                    texture.SetPixel(px, py + i, borderColor);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    private Texture2D RenderStageOverviewToTexture()
    {
        int cellSize = 16;
        int wavePadding = 20;
        int headerHeight = 30;
        int waveSpacing = 10;
        
        // Calculate total size
        int cols = Mathf.Min(wavesPerRow, selectedStage.waveConfigurations.Count);
        int rows = Mathf.CeilToInt((float)selectedStage.waveConfigurations.Count / cols);
        
        // Find max wave dimensions
        int maxWaveWidth = 0;
        int maxWaveHeight = 0;
        foreach (var wave in selectedStage.waveConfigurations)
        {
            if (wave == null) continue;
            maxWaveWidth = Mathf.Max(maxWaveWidth, wave.GridWidth);
            maxWaveHeight = Mathf.Max(maxWaveHeight, wave.GridHeight);
        }
        
        int waveBlockWidth = (maxWaveWidth * cellSize) + wavePadding * 2;
        int waveBlockHeight = (maxWaveHeight * cellSize) + wavePadding * 2 + headerHeight;
        
        int totalWidth = (cols * waveBlockWidth) + ((cols - 1) * waveSpacing) + 40;
        int totalHeight = (rows * waveBlockHeight) + ((rows - 1) * waveSpacing) + 60;
        
        Texture2D texture = new Texture2D(totalWidth, totalHeight, TextureFormat.RGBA32, false);
        
        // Fill background
        Color bgColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        for (int y = 0; y < totalHeight; y++)
        {
            for (int x = 0; x < totalWidth; x++)
            {
                texture.SetPixel(x, y, bgColor);
            }
        }
        
        // Draw each wave
        int waveIndex = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (waveIndex >= selectedStage.waveConfigurations.Count) break;
                
                WaveData wave = selectedStage.waveConfigurations[waveIndex];
                if (wave != null)
                {
                    int baseX = 20 + col * (waveBlockWidth + waveSpacing);
                    int baseY = totalHeight - 40 - (row + 1) * (waveBlockHeight + waveSpacing) + waveSpacing;
                    
                    RenderWaveToTextureAt(texture, wave, waveIndex, baseX, baseY, cellSize, wavePadding);
                }
                
                waveIndex++;
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    private void RenderWaveToTextureAt(Texture2D texture, WaveData wave, int index, int baseX, int baseY, int cellSize, int padding)
    {
        // Build cube lookup
        Dictionary<Vector2Int, CubeData> cubeMap = new Dictionary<Vector2Int, CubeData>();
        if (wave.CubesData != null)
        {
            foreach (var cube in wave.CubesData)
            {
                if (!cubeMap.ContainsKey(cube.position))
                    cubeMap[cube.position] = cube;
            }
        }
        
        // Draw panel background
        Color panelBg = new Color(0.18f, 0.18f, 0.2f, 1f);
        int panelWidth = (wave.GridWidth * cellSize) + padding * 2;
        int panelHeight = (wave.GridHeight * cellSize) + padding * 2 + 25;
        
        for (int py = 0; py < panelHeight; py++)
        {
            for (int px = 0; px < panelWidth; px++)
            {
                int tx = baseX + px;
                int ty = baseY + py;
                if (tx >= 0 && tx < texture.width && ty >= 0 && ty < texture.height)
                {
                    texture.SetPixel(tx, ty, panelBg);
                }
            }
        }
        
        // Draw grid cells
        int gridBaseY = baseY + 25; // Offset for header
        
        for (int gy = 0; gy < wave.GridHeight; gy++)
        {
            for (int gx = 0; gx < wave.GridWidth; gx++)
            {
                int px = baseX + padding + (gx * cellSize);
                int py = gridBaseY + padding + ((wave.GridHeight - 1 - gy) * cellSize);
                
                Vector2Int pos = new Vector2Int(gx, gy);
                Color cellColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                
                if (cubeMap.TryGetValue(pos, out CubeData cube))
                {
                    cellColor = cubeColors.ContainsKey(cube.type) ? cubeColors[cube.type] : Color.white;
                }
                
                // Fill cell
                for (int cy = 0; cy < cellSize - 1; cy++)
                {
                    for (int cx = 0; cx < cellSize - 1; cx++)
                    {
                        int tx = px + cx;
                        int ty = py + cy;
                        if (tx >= 0 && tx < texture.width && ty >= 0 && ty < texture.height)
                        {
                            texture.SetPixel(tx, ty, cellColor);
                        }
                    }
                }
                
                // Draw border
                Color borderColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                for (int i = 0; i < cellSize; i++)
                {
                    int tx1 = px + i;
                    int ty1 = py;
                    int tx2 = px;
                    int ty2 = py + i;
                    
                    if (tx1 >= 0 && tx1 < texture.width && ty1 >= 0 && ty1 < texture.height)
                        texture.SetPixel(tx1, ty1, borderColor);
                    if (tx2 >= 0 && tx2 < texture.width && ty2 >= 0 && ty2 < texture.height)
                        texture.SetPixel(tx2, ty2, borderColor);
                }
            }
        }
    }
    
    #endregion
}
