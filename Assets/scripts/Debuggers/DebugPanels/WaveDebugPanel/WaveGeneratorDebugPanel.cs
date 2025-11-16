using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static Enumerations;

namespace WaveDebugSystem
{
    /// <summary>
    /// Debug panel for the IQWaveGenerator, providing controls for wave generation,
    /// analysis display, and batch testing capabilities.
    /// </summary>
    public class WaveGeneratorDebugPanel
    {
        #region Fields
        private IQWaveGenerator waveGenerator;
        private GridManager gridManager;
        private WaveManager waveManager;
        
        // Generation controls
        private int generationDifficulty = 1;
        private GenerationStrategy generationStrategy = GenerationStrategy.Random;
        private bool useBalancedGeneration = false;
        
        // Last generated wave
        private WaveData lastGeneratedWave;
        private WaveAnalysisResult lastAnalysisResult;
        
        // Batch testing
        private int batchCount = 5;
        private int batchStartDifficulty = 1;
        private List<BatchTestResult> batchResults = new List<BatchTestResult>();
        private bool showBatchResults = false;
        
        // UI State
        private bool showGenerationControls = true;
        private bool showAnalysisResults = false;
        private bool showBatchTesting = false;
        private bool showCubeDistribution = false;
        
        // Visualization
        private Vector2 scrollPosition;
        #endregion
        
        #region Initialization
        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;
            this.waveGenerator = IQWaveGenerator.Instance;
            
            if (waveGenerator == null)
            {
                waveGenerator = Object.FindAnyObjectByType<IQWaveGenerator>();
            }
        }
        #endregion
        
        #region Main Panel Drawing
        public void DrawPanel(System.Action<WaveData> onWaveChanged = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("WAVE GENERATOR", GUI.skin.box);
            
            if (waveGenerator != null)
            {
                DrawSectionToggles();
                GUILayout.Space(5);
                
                if (showGenerationControls)
                    DrawGenerationControls(onWaveChanged);
                
                if (showAnalysisResults && lastAnalysisResult != null)
                    DrawAnalysisResults();
                
                if (showCubeDistribution && lastGeneratedWave != null)
                    DrawCubeDistribution();
                
                if (showBatchTesting)
                    DrawBatchTesting();
            }
            else
            {
                GUILayout.Label("IQWaveGenerator not found!");
                if (GUILayout.Button("Create Wave Generator"))
                {
                    CreateWaveGenerator();
                }
            }
            
            GUILayout.EndVertical();
        }
        #endregion
        
        #region Generation Controls
        private void DrawGenerationControls(System.Action<WaveData> onWaveChanged)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Generation Controls", EditorStyles.boldLabel);
            
            // Difficulty slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("Difficulty:", GUILayout.Width(80));
            generationDifficulty = (int)GUILayout.HorizontalSlider(generationDifficulty, 1, 10, GUILayout.Width(150));
            GUILayout.Label(generationDifficulty.ToString(), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            
            // Strategy selection
            GUILayout.BeginHorizontal();
            GUILayout.Label("Strategy:", GUILayout.Width(80));
            generationStrategy = (GenerationStrategy)GUILayout.SelectionGrid(
                (int)generationStrategy, 
                System.Enum.GetNames(typeof(GenerationStrategy)), 
                3
            );
            GUILayout.EndHorizontal();
            
            // Balanced generation toggle
            useBalancedGeneration = GUILayout.Toggle(useBalancedGeneration, "Use Balanced Generation");
            
            GUILayout.Space(5);
            
            // Generation buttons
            GUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Wave", GUILayout.Height(30)))
            {
                GenerateWave();
            }
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Generate & Analyze", GUILayout.Height(30)))
            {
                GenerateWave();
                if (lastGeneratedWave != null)
                {
                    AnalyzeWave();
                }
            }
            
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            // Action buttons for generated wave
            if (lastGeneratedWave != null)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Load in Wave Manager"))
                {
                    LoadWaveInManager();
                    onWaveChanged?.Invoke(lastGeneratedWave);
                }
                
                GUI.backgroundColor = Color.magenta;
                if (GUILayout.Button("Export as Asset"))
                {
                    ExportWaveAsset();
                }
                
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            
            // Generator stats
            DrawGeneratorStats();
            
            GUILayout.EndVertical();
        }
        
        private void DrawGeneratorStats()
        {
            if (waveGenerator == null) return;
            
            GUILayout.Space(5);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Generator Statistics", EditorStyles.miniLabel);
            GUILayout.Label($"Total Generated: {waveGenerator.TotalWavesGenerated}");
            GUILayout.Label($"Success Rate: {waveGenerator.SuccessRate:P0}");
            
            if (lastGeneratedWave != null)
            {
                GUILayout.Label($"Last Wave: {lastGeneratedWave.CubesData.Count} cubes");
            }
            GUILayout.EndVertical();
        }
        #endregion
        
        #region Analysis Display
        private void DrawAnalysisResults()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Analysis Results", EditorStyles.boldLabel);
            
            // Solvability status
            GUI.backgroundColor = lastAnalysisResult.isSolvable ? Color.green : Color.red;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"SOLVABLE: {lastAnalysisResult.isSolvable}", EditorStyles.largeLabel);
            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();
            
            // Key metrics
            GUILayout.BeginHorizontal();
            DrawMetricBox("Min Slack", lastAnalysisResult.minimumSlackSpace.ToString(), Color.cyan);
            DrawMetricBox("Markers", lastAnalysisResult.requiredMarkers.ToString(), Color.yellow);
            DrawMetricBox("Difficulty", lastAnalysisResult.difficulty.ToString(), Color.magenta);
            GUILayout.EndHorizontal();
            
            // Warnings
            if (lastAnalysisResult.warnings != null && lastAnalysisResult.warnings.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("Warnings:", EditorStyles.boldLabel);
                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);
                GUILayout.BeginVertical(GUI.skin.box);
                GUI.backgroundColor = Color.white;
                
                foreach (var warning in lastAnalysisResult.warnings)
                {
                    GUILayout.Label($"⚠ {warning}", EditorStyles.wordWrappedMiniLabel);
                }
                GUILayout.EndVertical();
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawMetricBox(string label, string value, Color color)
        {
            GUI.backgroundColor = color;
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;
            GUILayout.Label(label, EditorStyles.miniLabel);
            GUILayout.Label(value, EditorStyles.largeLabel);
            GUILayout.EndVertical();
        }
        #endregion
        
        #region Cube Distribution
        private void DrawCubeDistribution()
        {
            if (lastGeneratedWave == null || lastGeneratedWave.CubesData == null) return;
            
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Cube Distribution", EditorStyles.boldLabel);
            
            // Count cubes by type
            Dictionary<CubeType, int> distribution = new Dictionary<CubeType, int>();
            foreach (var cube in lastGeneratedWave.CubesData)
            {
                if (!distribution.ContainsKey(cube.type))
                    distribution[cube.type] = 0;
                distribution[cube.type]++;
            }
            
            // Display distribution
            foreach (var kvp in distribution)
            {
                float percentage = (float)kvp.Value / lastGeneratedWave.CubesData.Count;
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{kvp.Key}:", GUILayout.Width(80));
                
                // Progress bar
                Rect rect = GUILayoutUtility.GetRect(150, 20);
                GUI.Box(rect, "");
                rect.width *= percentage;
                GUI.backgroundColor = GetCubeTypeColor(kvp.Key);
                GUI.Box(rect, "");
                GUI.backgroundColor = Color.white;
                
                GUILayout.Label($"{kvp.Value} ({percentage:P0})", GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }
            
            // Visual grid preview
            if (GUILayout.Button("Show Grid Preview"))
            {
                showAnalysisResults = true;
            }
            
            GUILayout.EndVertical();
        }
        
        private Color GetCubeTypeColor(CubeType type)
        {
            switch (type)
            {
                case CubeType.Unit: return Color.white;
                case CubeType.Prime: return Color.blue;
                case CubeType.Infinity: return Color.black;
                case CubeType.Recursion: return Color.green;
                default: return Color.gray;
            }
        }
        #endregion
        
        #region Batch Testing
        private void DrawBatchTesting()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Batch Testing", EditorStyles.boldLabel);
            
            // Batch parameters
            GUILayout.BeginHorizontal();
            GUILayout.Label("Count:", GUILayout.Width(50));
            batchCount = Mathf.Max(1, EditorGUILayout.IntField(batchCount, GUILayout.Width(50)));
            GUILayout.Label("Start Diff:", GUILayout.Width(70));
            batchStartDifficulty = Mathf.Max(1, EditorGUILayout.IntField(batchStartDifficulty, GUILayout.Width(50)));
            GUILayout.EndHorizontal();
            
            // Run batch test
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Run Batch Test", GUILayout.Height(25)))
            {
                RunBatchTest();
            }
            GUI.backgroundColor = Color.white;
            
            // Display results
            if (showBatchResults && batchResults.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("Batch Results:", EditorStyles.boldLabel);
                
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));
                
                foreach (var result in batchResults)
                {
                    DrawBatchResult(result);
                }
                
                GUILayout.EndScrollView();
                
                // Summary stats
                DrawBatchSummary();
            }
            
            GUILayout.EndVertical();
        }
        
        private void DrawBatchResult(BatchTestResult result)
        {
            GUI.backgroundColor = result.isSolvable ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUI.backgroundColor = Color.white;
            
            GUILayout.Label($"Wave {result.waveNumber}", GUILayout.Width(60));
            GUILayout.Label($"Diff: {result.difficulty}", GUILayout.Width(50));
            GUILayout.Label($"Cubes: {result.cubeCount}", GUILayout.Width(60));
            GUILayout.Label(result.isSolvable ? "✓ PASS" : "✗ FAIL", GUILayout.Width(60));
            GUILayout.Label($"Slack: {result.minSlackSpace}", GUILayout.Width(60));
            
            GUILayout.EndHorizontal();
        }
        
        private void DrawBatchSummary()
        {
            int passed = batchResults.Count(r => r.isSolvable);
            float passRate = (float)passed / batchResults.Count;
            
            GUILayout.Space(5);
            GUI.backgroundColor = passRate > 0.8f ? Color.green : Color.yellow;
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;
            
            GUILayout.Label($"Pass Rate: {passRate:P0} ({passed}/{batchResults.Count})", EditorStyles.boldLabel);
            
            if (batchResults.Count > 0)
            {
                float avgSlack = (float)batchResults.Average(r => r.minSlackSpace);
                float avgMarkers = (float)batchResults.Average(r => r.requiredMarkers);
                
                GUILayout.Label($"Avg Slack Space: {avgSlack:F1}");
                GUILayout.Label($"Avg Markers Required: {avgMarkers:F1}");
            }
            
            GUILayout.EndVertical();
        }
        #endregion
        
        #region Section Toggles
        private void DrawSectionToggles()
        {
            GUILayout.BeginHorizontal();
            
            showGenerationControls = GUILayout.Toggle(showGenerationControls, "Generation", "Button", GUILayout.Width(80));
            showAnalysisResults = GUILayout.Toggle(showAnalysisResults, "Analysis", "Button", GUILayout.Width(80));
            showCubeDistribution = GUILayout.Toggle(showCubeDistribution, "Distribution", "Button", GUILayout.Width(80));
            showBatchTesting = GUILayout.Toggle(showBatchTesting, "Batch Test", "Button", GUILayout.Width(80));
            
            GUILayout.EndHorizontal();
        }
        #endregion
        
        #region Actions
        private void GenerateWave()
        {
            if (waveGenerator == null) return;
            
            if (useBalancedGeneration)
            {
                // Create a temporary wave for balanced generation
                lastGeneratedWave = ScriptableObject.CreateInstance<WaveData>();
                lastGeneratedWave.name = $"Generated_D{generationDifficulty}_{System.DateTime.Now:HHmmss}";
                lastGeneratedWave.GridWidth = gridManager != null ? gridManager.Width : 5;
                lastGeneratedWave.GridHeight = gridManager != null ? gridManager.Height : 20;
                lastGeneratedWave.CubesData = new List<CubeData>();
                
                int cubeCount = 5 + (generationDifficulty - 1) * 2;
                waveGenerator.GenerateBalancedWave(lastGeneratedWave, cubeCount, generationDifficulty);
            }
            else
            {
                lastGeneratedWave = waveGenerator.GenerateWave(generationDifficulty, generationStrategy);
            }
            
            if (lastGeneratedWave != null)
            {
                Debug.Log($"[WaveGeneratorDebugPanel] Generated wave with {lastGeneratedWave.CubesData.Count} cubes");
                showCubeDistribution = true;
            }
        }
        
        private void AnalyzeWave()
        {
            if (lastGeneratedWave == null || waveGenerator == null) return;
            
            var analyzer = new WaveAnalyzer(waveGenerator);
            lastAnalysisResult = analyzer.AnalyzeWave(lastGeneratedWave);
            
            if (lastAnalysisResult != null)
            {
                showAnalysisResults = true;
                Debug.Log($"[WaveGeneratorDebugPanel] Analysis complete - Solvable: {lastAnalysisResult.isSolvable}");
            }
        }
        
        private void LoadWaveInManager()
        {
            if (lastGeneratedWave == null || waveManager == null) return;

            // There is no LoadWave method in WaveManager, but there is a CurrentWave property.
            // If you want to set the current wave, you may need to add a method to WaveManager
            // that allows you to set the current wave, such as SetCurrentWave(WaveData wave).
            // For now, you can try adding the wave to the configuration and updating the index.

            // Example workaround:
            if (waveManager.waveConfiguration == null)
                waveManager.waveConfiguration = new List<WaveData>();

            waveManager.waveConfiguration.Add(lastGeneratedWave);
            waveManager.currentWaveIndex = waveManager.waveConfiguration.Count - 1;

            Debug.Log($"[WaveGeneratorDebugPanel] Loaded generated wave in WaveManager");
        }
        
        private void ExportWaveAsset()
        {
            #if UNITY_EDITOR
            if (lastGeneratedWave == null) return;
            
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Wave Data",
                lastGeneratedWave.name,
                "asset",
                "Save generated wave as asset"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(lastGeneratedWave, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"[WaveGeneratorDebugPanel] Exported wave to: {path}");
            }
            #endif
        }
        
        private void RunBatchTest()
        {
            if (waveGenerator == null) return;
            
            batchResults.Clear();
            var analyzer = new WaveAnalyzer(waveGenerator);
            
            for (int i = 0; i < batchCount; i++)
            {
                int difficulty = batchStartDifficulty + (i / 3);
                var wave = waveGenerator.GenerateWave(difficulty, generationStrategy);
                
                if (wave != null)
                {
                    var analysis = analyzer.AnalyzeWave(wave);
                    
                    batchResults.Add(new BatchTestResult
                    {
                        waveNumber = i + 1,
                        difficulty = difficulty,
                        cubeCount = wave.CubesData.Count,
                        isSolvable = analysis.isSolvable,
                        minSlackSpace = analysis.minimumSlackSpace,
                        requiredMarkers = analysis.requiredMarkers
                    });
                    
                    // Clean up temporary wave
                    Object.DestroyImmediate(wave);
                }
            }
            
            showBatchResults = true;
            Debug.Log($"[WaveGeneratorDebugPanel] Batch test complete: {batchResults.Count} waves tested");
        }
        
        private void CreateWaveGenerator()
        {
            var go = new GameObject("IQWaveGenerator");
            waveGenerator = go.AddComponent<IQWaveGenerator>();
            Debug.Log("[WaveGeneratorDebugPanel] Created IQWaveGenerator");
        }
        #endregion
        
        #region Helper Classes
        private class BatchTestResult
        {
            public int waveNumber;
            public int difficulty;
            public int cubeCount;
            public bool isSolvable;
            public int minSlackSpace;
            public int requiredMarkers;
        }
        #endregion
    }
}
