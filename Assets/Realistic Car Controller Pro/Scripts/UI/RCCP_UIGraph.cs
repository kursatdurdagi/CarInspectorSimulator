//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Runtime Canvas graph component that draws line graphs via OnPopulateMesh.
/// Supports multiple data series with ring buffers for real-time telemetry,
/// and static AnimationCurve display.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Graph")]
[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(RectTransform))]
public class RCCP_UIGraph : MaskableGraphic {

    /// <summary>
    /// A single data series displayed on the graph.
    /// </summary>
    [System.Serializable]
    public class GraphSeries {

        [Tooltip("Display name for this data series in the legend.")]
        public string label = "Series";
        [Tooltip("Line color used to draw this series on the graph.")]
        public Color color = Color.green;
        [Tooltip("Thickness of the line in pixels.")]
        [Range(0.5f, 5f)] public float lineWidth = 1.5f;
        [HideInInspector] public float[] samples;
        [HideInInspector] public int head;
        [HideInInspector] public int count;

    }

    [Header("Graph Settings")]
    [Tooltip("Maximum number of samples stored per series (ring buffer size).")]
    public int maxSamples = 256;

    [Tooltip("Automatically fit Y axis to the data range.")]
    public bool autoRange = true;

    [Tooltip("Manual Y axis range when autoRange is off.")]
    public Vector2 fixedRange = new Vector2(0f, 1f);

    [Header("Grid")]
    [Tooltip("Whether to draw background grid lines on the graph.")]
    public bool showGrid = true;
    [Tooltip("Color and opacity of the grid lines.")]
    public Color gridColor = new Color(1f, 1f, 1f, 0.1f);
    [Tooltip("Number of vertical grid divisions.")]
    public int gridLinesX = 4;
    [Tooltip("Number of horizontal grid divisions.")]
    public int gridLinesY = 4;

    [Header("Background")]
    [Tooltip("Fill color drawn behind the graph area.")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);

    [Header("Data")]
    [Tooltip("List of data series displayed on this graph.")]
    public List<GraphSeries> series = new List<GraphSeries>();

    private bool _dirty;

    /// <summary>
    /// Uses the built-in white texture (no material needed).
    /// </summary>
    public override Texture mainTexture => s_WhiteTexture;

    /// <summary>
    /// Push a single sample to a series. Call once per frame per channel.
    /// </summary>
    public void PushSample(int seriesIndex, float value) {

        if (seriesIndex < 0 || seriesIndex >= series.Count) return;

        var s = series[seriesIndex];
        EnsureBuffer(s);
        s.samples[s.head] = value;
        s.head = (s.head + 1) % maxSamples;
        if (s.count < maxSamples) s.count++;
        _dirty = true;

    }

    /// <summary>
    /// Push one sample per series in a single call (values[0] -> series[0], etc.).
    /// </summary>
    public void PushSamples(float[] values) {

        int count = Mathf.Min(values.Length, series.Count);

        for (int i = 0; i < count; i++) {

            var s = series[i];
            EnsureBuffer(s);
            s.samples[s.head] = values[i];
            s.head = (s.head + 1) % maxSamples;
            if (s.count < maxSamples) s.count++;

        }

        _dirty = true;

    }

    /// <summary>
    /// Fill a series buffer by evaluating an AnimationCurve (for static display).
    /// </summary>
    public void SetStaticCurve(int seriesIndex, AnimationCurve curve, float xMin, float xMax, int resolution = 64) {

        if (seriesIndex < 0 || seriesIndex >= series.Count || curve == null) return;

        var s = series[seriesIndex];

        if (resolution > maxSamples)
            maxSamples = resolution;

        s.samples = new float[maxSamples];
        s.head = 0;
        s.count = resolution;

        for (int i = 0; i < resolution; i++) {

            float t = Mathf.Lerp(xMin, xMax, i / (float)(resolution - 1));
            s.samples[i] = curve.Evaluate(t);

        }

        s.head = resolution % maxSamples;
        _dirty = true;

    }

    /// <summary>
    /// Clear all series buffers.
    /// </summary>
    public void Clear() {

        foreach (var s in series) {

            s.head = 0;
            s.count = 0;

            if (s.samples != null)
                System.Array.Clear(s.samples, 0, s.samples.Length);

        }

        _dirty = true;

    }

    private void LateUpdate() {

        if (_dirty) {

            _dirty = false;
            SetVerticesDirty();

        }

    }

    protected override void OnPopulateMesh(VertexHelper vh) {

        vh.Clear();

        Rect rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;

        // Background
        if (backgroundColor.a > 0f)
            AddQuad(vh,
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMax, rect.yMin),
                backgroundColor);

        // Grid
        if (showGrid) {

            float gridHalf = 0.5f;

            for (int i = 0; i <= gridLinesY; i++) {

                float y = Mathf.Lerp(rect.yMin, rect.yMax, i / (float)gridLinesY);
                AddQuad(vh,
                    new Vector2(rect.xMin, y - gridHalf),
                    new Vector2(rect.xMin, y + gridHalf),
                    new Vector2(rect.xMax, y + gridHalf),
                    new Vector2(rect.xMax, y - gridHalf),
                    gridColor);

            }

            for (int i = 0; i <= gridLinesX; i++) {

                float x = Mathf.Lerp(rect.xMin, rect.xMax, i / (float)gridLinesX);
                AddQuad(vh,
                    new Vector2(x - gridHalf, rect.yMin),
                    new Vector2(x - gridHalf, rect.yMax),
                    new Vector2(x + gridHalf, rect.yMax),
                    new Vector2(x + gridHalf, rect.yMin),
                    gridColor);

            }

        }

        // Compute Y range
        float yMin, yMax;

        if (autoRange) {

            yMin = float.MaxValue;
            yMax = float.MinValue;

            foreach (var s in series) {

                if (s.count == 0) continue;

                for (int i = 0; i < s.count; i++) {

                    int idx = (s.head - s.count + i + maxSamples) % maxSamples;
                    float v = s.samples[idx];
                    if (v < yMin) yMin = v;
                    if (v > yMax) yMax = v;

                }

            }

            if (yMin >= yMax) {
                yMin -= 0.5f;
                yMax += 0.5f;
            } else {
                float range = yMax - yMin;
                yMin -= range * 0.05f;
                yMax += range * 0.05f;
            }

        } else {

            yMin = fixedRange.x;
            yMax = fixedRange.y;

        }

        // Draw series
        foreach (var s in series) {

            if (s.count < 2) continue;

            float halfWidth = s.lineWidth * 0.5f;
            Vector2 prevPoint = Vector2.zero;

            for (int i = 0; i < s.count; i++) {

                int idx = (s.head - s.count + i + maxSamples) % maxSamples;
                float v = s.samples[idx];

                float nx = (float)i / (s.count - 1);
                float ny = (v - yMin) / (yMax - yMin);

                Vector2 point = new Vector2(
                    Mathf.Lerp(rect.xMin, rect.xMax, nx),
                    Mathf.Lerp(rect.yMin, rect.yMax, ny));

                if (i > 0) {

                    Vector2 dir = point - prevPoint;
                    float len = dir.magnitude;

                    if (len > 0.001f) {

                        dir /= len;
                        Vector2 perp = new Vector2(-dir.y, dir.x) * halfWidth;

                        AddQuad(vh,
                            prevPoint - perp,
                            prevPoint + perp,
                            point + perp,
                            point - perp,
                            s.color);

                    }

                }

                prevPoint = point;

            }

        }

    }

    private void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 col) {

        int idx = vh.currentVertCount;

        UIVertex vert = UIVertex.simpleVert;
        vert.color = col;

        vert.position = new Vector3(a.x, a.y, 0f);
        vh.AddVert(vert);

        vert.position = new Vector3(b.x, b.y, 0f);
        vh.AddVert(vert);

        vert.position = new Vector3(c.x, c.y, 0f);
        vh.AddVert(vert);

        vert.position = new Vector3(d.x, d.y, 0f);
        vh.AddVert(vert);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);

    }

    private void EnsureBuffer(GraphSeries s) {

        if (s.samples == null || s.samples.Length != maxSamples) {

            s.samples = new float[maxSamples];
            s.head = 0;
            s.count = 0;

        }

    }

}
