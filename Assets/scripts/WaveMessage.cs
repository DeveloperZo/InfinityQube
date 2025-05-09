using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveMessage
{
    [TextArea(3, 5)]
    public string Message;

    public int DisplayMoveStep = -1;  // -1 means show at any time

    public bool RequirePause = false;

    public float AutoHideDelay = 5f;  // Seconds to auto-hide if not paused

    [Header("Highlight Options")]
    public bool HighlightTile = false;
    public List<Vector2Int> highlightTiles = new List<Vector2Int>();
    public Color highlightColor = Color.yellow;
}
