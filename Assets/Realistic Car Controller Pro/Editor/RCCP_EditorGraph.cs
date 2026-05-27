//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Static utility for rendering AnimationCurve graphs as textures in the Inspector.
/// Uses GL commands on a temporary RenderTexture, then reads back to Texture2D.
/// </summary>
public static class RCCP_EditorGraph {

    /// <summary>
    /// Data for a single curve in an overlaid graph.
    /// </summary>
    public struct CurveData {

        [Tooltip("AnimationCurve to plot in the graph.")]
        public AnimationCurve curve;
        [Tooltip("Line color used when drawing this curve.")]
        public Color color;
        [Tooltip("Display label shown in the graph legend.")]
        public string label;
        [Tooltip("Minimum X-axis value for this curve's plot range.")]
        public float xMin;
        [Tooltip("Maximum X-axis value for this curve's plot range.")]
        public float xMax;

    }

    private static Material _glMaterial;
    private static Dictionary<int, Texture2D> _cache = new Dictionary<int, Texture2D>();
    private const int MaxCacheSize = 8;

    private static Material GLMaterial {

        get {

            if (_glMaterial == null) {

                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _glMaterial = new Material(shader);
                _glMaterial.hideFlags = HideFlags.HideAndDontSave;
                _glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _glMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _glMaterial.SetInt("_ZWrite", 0);

            }

            return _glMaterial;

        }

    }

    /// <summary>
    /// Renders a single AnimationCurve to a Texture2D.
    /// </summary>
    public static Texture2D RenderCurve(AnimationCurve curve, int width, int height,
        Color curveColor, Color bgColor, float xMin, float xMax, float yMin, float yMax,
        bool showGrid = true, Color? gridColor = null) {

        return RenderCurves(new[] {

            new CurveData {
                curve = curve,
                color = curveColor,
                label = null,
                xMin = xMin,
                xMax = xMax
            }

        }, width, height, bgColor, yMin, yMax, showGrid, gridColor);

    }

    /// <summary>
    /// Renders multiple overlaid AnimationCurves to a Texture2D.
    /// </summary>
    public static Texture2D RenderCurves(CurveData[] curves, int width, int height,
        Color? bgColor = null, float yMin = 0f, float yMax = 0f,
        bool showGrid = true, Color? gridColor = null) {

        int hash = ComputeHash(curves, width, height);

        if (_cache.TryGetValue(hash, out var cached) && cached != null)
            return cached;

        Color bg = bgColor ?? new Color(0.15f, 0.15f, 0.15f);
        Color grid = gridColor ?? new Color(1f, 1f, 1f, 0.08f);

        // Auto-range Y if min == max
        if (Mathf.Approximately(yMin, yMax)) {

            yMin = float.MaxValue;
            yMax = float.MinValue;

            foreach (var cd in curves) {

                if (cd.curve == null) continue;

                for (int i = 0; i <= width; i++) {

                    float t = Mathf.Lerp(cd.xMin, cd.xMax, i / (float)width);
                    float v = cd.curve.Evaluate(t);
                    if (v < yMin) yMin = v;
                    if (v > yMax) yMax = v;

                }

            }

            float range = yMax - yMin;

            if (range < 0.001f) {
                yMin -= 0.5f;
                yMax += 0.5f;
            } else {
                yMin -= range * 0.05f;
                yMax += range * 0.05f;
            }

        }

        // Render to RenderTexture
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, width, height, 0);
        GL.Clear(true, true, bg);

        GLMaterial.SetPass(0);

        // Grid
        if (showGrid) {

            GL.Begin(GL.LINES);
            GL.Color(grid);

            for (int i = 0; i <= 4; i++) {

                float y = i / 4f * height;
                GL.Vertex3(0, y, 0);
                GL.Vertex3(width, y, 0);

            }

            for (int i = 0; i <= 4; i++) {

                float x = i / 4f * width;
                GL.Vertex3(x, 0, 0);
                GL.Vertex3(x, height, 0);

            }

            GL.End();

        }

        // Curves as 2px-wide quads
        foreach (var cd in curves) {

            if (cd.curve == null) continue;

            GL.Begin(GL.QUADS);
            GL.Color(cd.color);

            float halfWidth = 1f;
            Vector2 prevPoint = Vector2.zero;
            int steps = width;

            for (int i = 0; i <= steps; i++) {

                float t = Mathf.Lerp(cd.xMin, cd.xMax, i / (float)steps);
                float v = cd.curve.Evaluate(t);
                float px = i / (float)steps * width;
                float py = (1f - (v - yMin) / (yMax - yMin)) * height;

                Vector2 point = new Vector2(px, py);

                if (i > 0) {

                    Vector2 dir = (point - prevPoint);
                    float len = dir.magnitude;

                    if (len > 0.001f) {

                        dir /= len;
                        Vector2 perp = new Vector2(-dir.y, dir.x) * halfWidth;

                        GL.Vertex3(prevPoint.x - perp.x, prevPoint.y - perp.y, 0);
                        GL.Vertex3(prevPoint.x + perp.x, prevPoint.y + perp.y, 0);
                        GL.Vertex3(point.x + perp.x, point.y + perp.y, 0);
                        GL.Vertex3(point.x - perp.x, point.y - perp.y, 0);

                    }

                }

                prevPoint = point;

            }

            GL.End();

        }

        GL.PopMatrix();

        // Read to Texture2D
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        // Evict oldest if cache is full
        if (_cache.Count >= MaxCacheSize) {

            foreach (var kvp in _cache)
                if (kvp.Value != null) Object.DestroyImmediate(kvp.Value);

            _cache.Clear();

        }

        _cache[hash] = tex;
        return tex;

    }

    /// <summary>
    /// Legend entry for a curve in the graph.
    /// </summary>
    public struct LegendEntry {

        [Tooltip("Color swatch displayed next to the legend text.")]
        public Color color;
        [Tooltip("Text label identifying the curve in the legend.")]
        public string label;

    }

    /// <summary>
    /// Draws a graph texture in a GUILayout region with optional title, axis labels, and legend.
    /// </summary>
    public static void DrawGraphLayout(Texture2D texture, string title = null,
        string xLabel = null, string yLabel = null, float height = 150f,
        LegendEntry[] legends = null) {

        if (texture == null) return;

        // Title + Legend row
        if (!string.IsNullOrEmpty(title) || (legends != null && legends.Length > 0)) {

            EditorGUILayout.BeginHorizontal();

            if (!string.IsNullOrEmpty(title))
                EditorGUILayout.LabelField(title, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

            if (legends != null) {

                GUIStyle legendStyle = new GUIStyle(EditorStyles.miniLabel);
                legendStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

                for (int i = 0; i < legends.Length; i++) {

                    // Color swatch
                    Rect swatchRect = GUILayoutUtility.GetRect(10f, 10f, GUILayout.Width(10f));
                    swatchRect.y += 2f;
                    EditorGUI.DrawRect(swatchRect, legends[i].color);

                    // Label
                    GUILayout.Label(legends[i].label, legendStyle, GUILayout.ExpandWidth(false));

                    if (i < legends.Length - 1)
                        GUILayout.Space(6f);

                }

            }

            EditorGUILayout.EndHorizontal();

        }

        Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40f, height);
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);

        if (!string.IsNullOrEmpty(xLabel)) {

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            Rect xRect = new Rect(rect.x, rect.yMax, rect.width, 14f);
            GUI.Label(xRect, xLabel, labelStyle);
            GUILayoutUtility.GetRect(0, 14f);

        }

    }

    private static int ComputeHash(CurveData[] curves, int width, int height) {

        int hash = width * 31 + height;

        foreach (var cd in curves) {

            if (cd.curve == null) continue;

            hash = hash * 31 + cd.color.GetHashCode();
            hash = hash * 31 + cd.xMin.GetHashCode();
            hash = hash * 31 + cd.xMax.GetHashCode();

            for (int i = 0; i < cd.curve.length; i++) {

                var key = cd.curve[i];
                hash = hash * 31 + key.time.GetHashCode();
                hash = hash * 31 + key.value.GetHashCode();
                hash = hash * 31 + key.inTangent.GetHashCode();
                hash = hash * 31 + key.outTangent.GetHashCode();

            }

        }

        return hash;

    }

}
#endif
