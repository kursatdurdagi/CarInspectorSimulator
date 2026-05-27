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
using System.Linq;

/// <summary>
/// Editor window that lists every mesh under a vehicle hierarchy (largest → smallest)
/// and live-edits MeshCollider presence per part: each row's toggle reflects whether
/// that GameObject currently has a MeshCollider, and flipping the toggle instantly
/// adds or removes the component (with Undo support). Selected/colliderized parts
/// are highlighted in the Scene view.
/// </summary>
public class RCCP_BodyCollidersWizard : EditorWindow {

    //───────────────────────────────────────────────────────────────────────//
    #region Fields
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Currently selected root GameObject (vehicle).</summary>
    [Tooltip("Root GameObject of the vehicle receiving body colliders.")]
    public GameObject selectedVehicle;

    private List<Transform> candidates = new List<Transform>();     // all mesh parts
    private List<Transform> excludedCandidates = new List<Transform>();     // all excluded mesh parts

    private int autoSelectTopCount = 0;                    // on open, auto-add MeshColliders to top N largest parts that don't already have one
    private bool hasAutoSeeded = false;                    // gate so auto-seed fires once per ShowWindow call, not per Refresh

    // highlight settings
    private Color highlightColor = new Color(1f, .55f, 0f, .35f);   // default orange
    private bool solidOverlay = true;                                 // solid vs wire overlay

    // collider settings (applied when ADDING a new MeshCollider; not retroactive)
    private bool convexColliders = true;

    private static Material highlightMat;                           // shared GL material
    private Vector2 scrollPos;                              // scroll in list

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Menu & Lifecycle
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Opens the wizard.</summary>
    public static void ShowWindow(GameObject _selectedVehicle, List<Transform> excluded) {

        ShowWindow(_selectedVehicle, excluded, 1);

    }

    /// <summary>Opens the wizard with auto-selection of top N largest meshes.</summary>
    public static void ShowWindow(GameObject _selectedVehicle, List<Transform> excluded, int autoSelectTop) {

        RCCP_BodyCollidersWizard window = GetWindow<RCCP_BodyCollidersWizard>("Quick Body Colliders Wizard");
        window.minSize = new Vector2(420f, 560f);
        window.selectedVehicle = _selectedVehicle;
        window.excludedCandidates.Clear();
        window.excludedCandidates = excluded;
        window.autoSelectTopCount = autoSelectTop;
        window.hasAutoSeeded = false;
        window.RefreshCandidates();

    }

    /// <summary>Opens the wizard.</summary>
    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Body Colliders", false, 30)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Body Colliders", false, 30)]
    public static void ShowWindow() {

        RCCP_BodyCollidersWizard window = GetWindow<RCCP_BodyCollidersWizard>("Quick Body Colliders Wizard");
        window.minSize = new Vector2(420f, 560f);
        window.excludedCandidates.Clear();
        window.autoSelectTopCount = 0;
        window.hasAutoSeeded = true;     // menu-launched: skip auto-seed, user controls everything
        window.RefreshCandidates();

    }

    private void OnEnable() {

        SceneView.duringSceneGui += OnSceneGUI;
        RefreshCandidates();

    }

    private void OnDisable() {

        SceneView.duringSceneGui -= OnSceneGUI;

        if (highlightMat)
            DestroyImmediate(highlightMat);

    }

    /// <summary>Repaints at ~10Hz so external MeshCollider changes (Inspector edits, undo, scripts) reflect live.</summary>
    private void OnInspectorUpdate() {

        Repaint();

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region GUI
    //───────────────────────────────────────────────────────────────────────//

    private void OnGUI() {

        EditorGUILayout.HelpBox(
            "Live MeshCollider editor for vehicle body parts.\n" +
            "• Each row shows whether that part currently has a MeshCollider.\n" +
            "• Toggle a row to add or remove the MeshCollider instantly (Undo supported).\n" +
            "• Convex setting below applies to NEW colliders only.",
            MessageType.Info);

        EditorGUILayout.Space();

        selectedVehicle = (GameObject)EditorGUILayout.ObjectField("Root Vehicle", selectedVehicle, typeof(GameObject), true);

        if (GUILayout.Button("Refresh List"))
            RefreshCandidates();

        if (candidates == null || candidates.Count == 0) {

            EditorGUILayout.HelpBox("No mesh parts found under the selected vehicle.", MessageType.Info);
            return;

        }

        EditorGUILayout.Space();

        // Live count
        int colliderCount = CountPartsWithCollider();
        EditorGUILayout.LabelField($"MeshColliders: {colliderCount} / {candidates.Count}", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Add All", "Add a MeshCollider to every part that doesn't already have one")))
            BulkSetColliders(true);
        if (GUILayout.Button(new GUIContent("Remove All", "Remove the MeshCollider from every listed part")))
            BulkSetColliders(false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("Parts (largest → smallest)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Toggle = add/remove MeshCollider (live)", EditorStyles.miniLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(220f));
        for (int i = 0; i < candidates.Count; i++) {

            if (!candidates[i])
                continue;

            bool hasCollider = candidates[i].TryGetComponent(out MeshCollider existing);

            EditorGUILayout.BeginHorizontal();
            bool wantsCollider = EditorGUILayout.Toggle(hasCollider, GUILayout.Width(20f));
            GUILayout.Label(candidates[i].name);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Ping", "Highlight this GameObject in the Hierarchy"), EditorStyles.miniButton, GUILayout.Width(44f)))
                EditorGUIUtility.PingObject(candidates[i].gameObject);
            EditorGUILayout.EndHorizontal();

            if (wantsCollider != hasCollider)
                ApplyColliderState(candidates[i].gameObject, existing, wantsCollider);

        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        GUILayout.Label("Highlight Options", EditorStyles.boldLabel);

        highlightColor = EditorGUILayout.ColorField("Color", highlightColor);
        solidOverlay = EditorGUILayout.ToggleLeft("Solid overlay (otherwise wireframe)", solidOverlay);

        EditorGUILayout.Space();
        GUILayout.Label("Collider Options", EditorStyles.boldLabel);

        convexColliders = EditorGUILayout.ToggleLeft(new GUIContent("Convex MeshColliders", "Applied to newly added MeshColliders. Existing ones are not modified — change them via the Inspector."), convexColliders);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("Done", "Close this window. Changes are already applied — no save step needed."), GUILayout.Height(28f)))
            Close();

        // repaint Scene view instantly on changes
        if (GUI.changed)
            SceneView.RepaintAll();

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Core Logic
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Collects every mesh under the root, sorts them by volume, and resets the toggle array.</summary>
    private void RefreshCandidates() {

        candidates.Clear();

        if (!selectedVehicle)
            return;

        List<MeshFilter> mfs = new List<MeshFilter>(selectedVehicle.GetComponentsInChildren<MeshFilter>(true));
        List<MeshFilter> properMfs = new List<MeshFilter>();

        for (int i = 0; i < mfs.Count; i++) {

            if (mfs[i] == null)
                continue;

            if (excludedCandidates.Contains(mfs[i].transform))
                continue;

            // Skip wheel-related objects by name
            if (IsWheelByName(mfs[i].transform))
                continue;

            // Skip wheel-shaped meshes (cylindrical aspect ratio)
            if (IsWheelByShape(mfs[i].sharedMesh))
                continue;

            properMfs.Add(mfs[i]);

        }

        candidates = properMfs
            .OrderByDescending(mf => MeshVolume(mf.sharedMesh))
            .Select(mf => mf.transform)
            .ToList();

        // Auto-seed: on first RefreshCandidates after a ShowWindow that requested seeding,
        // ensure the top N largest parts have a MeshCollider. Skips parts that already have one,
        // so re-opening the wizard doesn't override the user's manual toggles.
        if (!hasAutoSeeded && autoSelectTopCount > 0) {

            int seedCount = Mathf.Min(autoSelectTopCount, candidates.Count);

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Auto-seed Body MeshColliders");
            int undoGroup = Undo.GetCurrentGroup();

            for (int i = 0; i < seedCount; i++) {

                if (!candidates[i])
                    continue;

                if (!candidates[i].TryGetComponent<MeshCollider>(out _)) {

                    MeshCollider mc = Undo.AddComponent<MeshCollider>(candidates[i].gameObject);
                    mc.convex = convexColliders;

                }

            }

            Undo.CollapseUndoOperations(undoGroup);
            hasAutoSeeded = true;

        }

    }

    // Wheel name patterns to exclude
    private static readonly string[] wheelNamePatterns = new string[] {
        "wheel", "tire", "tyre", "rim", "whl",
        "fl_", "fr_", "rl_", "rr_",
        "front_left", "front_right", "rear_left", "rear_right",
        "frontleft", "frontright", "rearleft", "rearright",
        "_fl", "_fr", "_rl", "_rr",
        "lf_", "rf_", "lr_", "rr_",
        "leftfront", "rightfront", "leftrear", "rightrear"
    };

    /// <summary>Checks if the transform name indicates it's a wheel.</summary>
    private bool IsWheelByName(Transform t) {

        if (t == null)
            return false;

        // Check current object and its parents (up to 3 levels)
        Transform current = t;
        int depth = 0;

        while (current != null && depth < 4) {

            string currentName = current.name.ToLowerInvariant();

            foreach (string pattern in wheelNamePatterns) {

                if (currentName.Contains(pattern))
                    return true;

            }

            current = current.parent;
            depth++;

        }

        return false;

    }

    /// <summary>Checks if the mesh has a wheel-like cylindrical shape (width much smaller than diameter).</summary>
    private bool IsWheelByShape(Mesh mesh) {

        if (mesh == null)
            return false;

        Vector3 size = mesh.bounds.size;

        // Skip very small meshes (likely not significant parts)
        float volume = size.x * size.y * size.z;
        if (volume < 0.001f)
            return false;

        // A wheel typically has one dimension (width) much smaller than the other two (diameter)
        // Sort dimensions to find the smallest
        float[] dims = new float[] { size.x, size.y, size.z };
        System.Array.Sort(dims);

        float smallest = dims[0];
        float middle = dims[1];
        float largest = dims[2];

        // Wheel criteria:
        // 1. The two larger dimensions should be similar (circular cross-section)
        // 2. The smallest dimension (width) should be significantly smaller than diameter
        // 3. Aspect ratio: width/diameter < 0.6 (wheels are typically 0.2-0.5)

        // Check if larger two dimensions are similar (within 30% of each other)
        float diameterRatio = middle / largest;
        if (diameterRatio < 0.7f)
            return false; // Not circular enough

        // Check if width is significantly smaller than diameter
        float widthToDiameterRatio = smallest / largest;

        // Typical wheel: width/diameter = 0.2 to 0.5
        // Body panels: usually more uniform or very flat
        // Threshold: if width < 55% of diameter AND diameter dimensions are similar, it's likely a wheel
        if (widthToDiameterRatio < 0.55f && diameterRatio > 0.7f)
            return true;

        return false;

    }

    /// <summary>Returns the approximate mesh volume used for sorting.</summary>
    private float MeshVolume(Mesh mesh) {

        if (mesh == null)
            return 0f;

        Vector3 size = mesh.bounds.size;
        return size.x * size.y * size.z;

    }

    /// <summary>Counts how many of the listed parts currently have a MeshCollider.</summary>
    private int CountPartsWithCollider() {

        int count = 0;

        for (int i = 0; i < candidates.Count; i++) {

            if (candidates[i] && candidates[i].TryGetComponent<MeshCollider>(out _))
                count++;

        }

        return count;

    }

    /// <summary>Adds or removes a MeshCollider on the given part with a single Undo step.</summary>
    private void ApplyColliderState(GameObject target, MeshCollider existing, bool wantsCollider) {

        if (!target)
            return;

        if (wantsCollider) {

            if (!existing) {

                MeshCollider mc = Undo.AddComponent<MeshCollider>(target);
                mc.convex = convexColliders;

            }

        } else {

            if (existing)
                Undo.DestroyObjectImmediate(existing);

        }

        SceneView.RepaintAll();

    }

    /// <summary>Bulk add or remove MeshColliders across every listed part as one Undo group.</summary>
    private void BulkSetColliders(bool addColliders) {

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(addColliders ? "Add All Body MeshColliders" : "Remove All Body MeshColliders");
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < candidates.Count; i++) {

            if (!candidates[i])
                continue;

            bool hasCollider = candidates[i].TryGetComponent(out MeshCollider existing);

            if (addColliders && !hasCollider) {

                MeshCollider mc = Undo.AddComponent<MeshCollider>(candidates[i].gameObject);
                mc.convex = convexColliders;

            } else if (!addColliders && hasCollider) {

                Undo.DestroyObjectImmediate(existing);

            }

        }

        Undo.CollapseUndoOperations(undoGroup);
        SceneView.RepaintAll();

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Scene Drawing
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Draws the coloured overlay for every selected part.</summary>
    private void OnSceneGUI(SceneView sv) {

        if (candidates == null || highlightColor.a <= 0f)
            return;

        EnsureMaterial();
        highlightMat.SetColor("_Color", highlightColor);
        highlightMat.SetPass(0);

        for (int i = 0; i < candidates.Count; i++) {

            if (!candidates[i])
                continue;

            if (!candidates[i].TryGetComponent<MeshCollider>(out _))
                continue;

            foreach (Renderer r in candidates[i].GetComponentsInChildren<Renderer>(true))
                DrawRenderer(r);

        }

        sv.Repaint();

    }

    /// <summary>Makes sure the GL material exists and is configured.</summary>
    private void EnsureMaterial() {

        if (highlightMat)
            return;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        highlightMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

        highlightMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        highlightMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        highlightMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
        highlightMat.SetInt("_ZWrite", 0);

    }

    /// <summary>Draws either a solid translucent mesh or a wireframe outline.</summary>
    private void DrawRenderer(Renderer r) {

        if (!r)
            return;

        if (r is MeshRenderer meshRenderer) {

            MeshFilter mf = meshRenderer.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
                DrawMesh(mf.sharedMesh, r.localToWorldMatrix);

        } else if (r is SkinnedMeshRenderer smr) {

            if (smr.sharedMesh)
                DrawMesh(smr.sharedMesh, r.localToWorldMatrix);

        }

    }

    private void DrawMesh(Mesh mesh, Matrix4x4 matrix) {

        if (solidOverlay) {

            Graphics.DrawMeshNow(mesh, matrix);

        } else {

            HandlesExtension.DrawWireMesh(mesh, matrix);

        }

    }

    #endregion

}

//───────────────────────────────────────────────────────────────────────────//
//  HandlesExtension – replacement for missing Handles.DrawWireMesh
//───────────────────────────────────────────────────────────────────────────//

/// <summary>
/// Compatibility helper that draws a wireframe representation of any mesh when
/// running on Unity versions that lack <c>Handles.DrawWireMesh</c>.
/// </summary>
public static class HandlesExtension {

    private static readonly Dictionary<Mesh, Vector3[]> cache = new Dictionary<Mesh, Vector3[]>();

    /// <summary>
    /// Draws the mesh as lines in the Scene view.
    /// </summary>
    public static void DrawWireMesh(Mesh mesh, Matrix4x4 matrix) {

        if (mesh == null)
            return;

        if (!cache.TryGetValue(mesh, out Vector3[] lines)) {

            lines = BuildLines(mesh);
            cache.Add(mesh, lines);

        }

        Handles.matrix = matrix;
        Handles.DrawLines(lines);
        Handles.matrix = Matrix4x4.identity;

    }

    /// <summary>Converts triangles into a unique list of line pairs.</summary>
    private static Vector3[] BuildLines(Mesh mesh) {

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        HashSet<ulong> edges = new HashSet<ulong>();

        for (int i = 0; i < tris.Length; i += 3) {

            AddEdge(edges, tris[i], tris[i + 1]);
            AddEdge(edges, tris[i + 1], tris[i + 2]);
            AddEdge(edges, tris[i + 2], tris[i]);

        }

        List<Vector3> pts = new List<Vector3>(edges.Count * 2);
        foreach (ulong e in edges) {

            ushort a = (ushort)(e & 0xFFFF);
            ushort b = (ushort)(e >> 16);

            pts.Add(verts[a]);
            pts.Add(verts[b]);

        }

        return pts.ToArray();

    }

    /// <summary>Adds the edge if it is not already present, otherwise removes it (dedup).</summary>
    private static void AddEdge(HashSet<ulong> set, int a, int b) {

        if (a < b) {

            ulong key = ((ulong)b << 16) | (uint)a;
            if (!set.Remove(key))
                set.Add(key);

        } else if (b < a) {

            ulong key = ((ulong)a << 16) | (uint)b;
            if (!set.Remove(key))
                set.Add(key);

        }

    }

}

#endif
