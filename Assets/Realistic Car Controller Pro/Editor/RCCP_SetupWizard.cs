//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

/// <summary>
/// Setup Wizard for Realistic Car Controller Pro.
/// Guides users through a 7-step vehicle configuration process.
/// </summary>
public class RCCP_SetupWizard : EditorWindow {

    #region Variables

    private RCCP_SetupWizardController controller;
    private VisualElement contentContainer;
    private VisualElement headerContainer;
    private VisualElement footerContainer;
    private VisualElement sidebarSlot;

    private IRCCP_SetupWizardStep[] steps;

    private static readonly string[] stepTitles = {
        "Basic Settings",
        "Wheel Assignment",
        "Suspension",
        "Engine & Drivetrain",
        "Components",
        "Body Colliders",
        "Finalize"
    };

    #endregion

    #region Menu Items

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Setup Wizard", false, 30)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Vehicle Setup/Setup Wizard", false, 30)]
    public static void ShowWindow() {

        var window = GetWindow<RCCP_SetupWizard>("RCCP Setup Wizard");
        // Wider minimum to fit the 220px sidebar alongside the content column,
        // and tall enough that the footer's Back/Next buttons are never clipped
        // when the window is floating. Note: Unity ignores minSize when the
        // window is docked, so the sidebar also scrolls (see CreateSidebar) to
        // keep the main column's footer in view at small docked heights.
        window.minSize = new Vector2(760f, 680f);
        // Reset size to minSize on every open so a previously-docked or
        // shrunken layout never carries over and hides the footer buttons.
        var r = window.position;
        r.width = 760f;
        r.height = 680f;
        window.position = r;

    }

    #endregion

    #region Lifecycle

    private void OnEnable() {

        controller = new RCCP_SetupWizardController();
        controller.OnStepChanged += LoadStepContent;

        steps = new IRCCP_SetupWizardStep[] {
            new RCCP_SetupStep_BasicSettings(),
            new RCCP_SetupStep_Wheels(),
            new RCCP_SetupStep_Suspension(),
            new RCCP_SetupStep_Engine(),
            new RCCP_SetupStep_Components(),
            new RCCP_SetupStep_BodyColliders(),
            new RCCP_SetupStep_Finalize()
        };

        // If the user already had a vehicle selected before opening the wizard,
        // lock onto it now so Step 0 doesn't ask them to select one again.
        controller.TryCaptureFromSelection();

        SceneView.duringSceneGui += OnWheelSceneGUI;

        BuildUI();

    }

    private void OnDisable() {

        SceneView.duringSceneGui -= OnWheelSceneGUI;

        if (controller != null)
            controller.OnStepChanged -= LoadStepContent;

    }

    private void OnSelectionChange() {

        if (controller == null || controller.CurrentStep != 0)
            return;

        // While Step 0 is visible, a new valid selection should auto-capture
        // so the user doesn't have to explicitly confirm. After capture, the
        // wizard stays locked on that target regardless of later Hierarchy
        // clicks.
        if (!controller.HasCapturedVehicle)
            controller.TryCaptureFromSelection();

        LoadStepContent();

    }

    #endregion

    #region UI Construction

    private void BuildUI() {

        rootVisualElement.Clear();

        var root = new VisualElement();
        root.AddToClassList("rccp-wizard-root");
        root.style.flexGrow = 1;

        RCCP_SetupWizardUI.AttachStyleSheets(root);

        // Body row: sidebar on the left, main column on the right.
        var body = new VisualElement();
        body.AddToClassList("rccp-wizard-body");
        root.Add(body);

        // Sidebar slot — contents are rebuilt every step so the current
        // indicator and completion checkmarks can update.
        sidebarSlot = new VisualElement();
        sidebarSlot.style.flexShrink = 0;
        body.Add(sidebarSlot);

        // Main column holds header + scrollable content + footer.
        var main = new VisualElement();
        main.AddToClassList("rccp-wizard-main");
        body.Add(main);

        // Header.
        headerContainer = new VisualElement();
        headerContainer.AddToClassList("rccp-wizard-header");
        main.Add(headerContainer);

        // Content.
        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.AddToClassList("rccp-wizard-content");
        scrollView.style.flexGrow = 1;

        contentContainer = new VisualElement();
        contentContainer.style.paddingTop = 12;
        contentContainer.style.paddingBottom = 12;
        contentContainer.style.paddingLeft = 12;
        contentContainer.style.paddingRight = 12;
        scrollView.Add(contentContainer);
        main.Add(scrollView);

        // Footer.
        footerContainer = new VisualElement();
        main.Add(footerContainer);

        rootVisualElement.Add(root);

        // Load initial step.
        LoadStepContent();

    }

    private void LoadStepContent() {

        if (contentContainer == null || headerContainer == null || footerContainer == null)
            return;

        int step = controller.CurrentStep;

        // Rebuild sidebar so the current-step highlight tracks navigation.
        if (sidebarSlot != null) {
            sidebarSlot.Clear();
            sidebarSlot.Add(RCCP_SetupWizardUI.CreateSidebar(stepTitles, step));
        }

        // Update header.
        headerContainer.Clear();
        headerContainer.Add(RCCP_SetupWizardUI.CreateStepHeader(step + 1, RCCP_SetupWizardController.TOTAL_STEPS, stepTitles[step]));

        // Enter step (triggers lazy operations).
        steps[step].OnStepEntered(controller);

        // Update content.
        contentContainer.Clear();
        contentContainer.Add(steps[step].CreateContent(controller));

        // Update footer.
        footerContainer.Clear();
        bool canBack = step > 0;
        bool canNext = controller.CanAdvanceFromStep(step);
        bool isLast = step == RCCP_SetupWizardController.TOTAL_STEPS - 1;

        footerContainer.Add(RCCP_SetupWizardUI.CreateFooter(
            // CurrentStep change fires OnStepChanged -> LoadStepContent, no manual reload needed.
            () => controller.CurrentStep--,
            () => {
                if (isLast) {
                    if (controller.FinishSetup())
                        Close();
                } else {
                    // Leaving Step 0: make sure we've captured a target before
                    // advancing so later steps don't fall back to live Selection.
                    if (step == 0 && !controller.HasCapturedVehicle)
                        controller.TryCaptureFromSelection();
                    controller.CurrentStep++;
                }
            },
            canBack,
            canNext,
            isLast
        ));

    }

    #endregion

    #region Scene Visualization

    /// <summary>
    /// Draws wheel wireframe visualization in the Scene View.
    /// Uses IMGUI Handles API (cannot be migrated to UI Toolkit).
    /// </summary>
    private void OnWheelSceneGUI(SceneView sceneView) {

        if (controller == null || controller.CurrentStep != 1 || !controller.HasDetectionResult)
            return;

        var data = controller.Data;

        // Draw assigned wheels in green.
        GameObject[] assignedWheels = {
            data.frontWheels.Count > 0 ? data.frontWheels[0] : null,
            data.frontWheels.Count > 1 ? data.frontWheels[1] : null,
            data.rearWheels.Count > 0 ? data.rearWheels[0] : null,
            data.rearWheels.Count > 1 ? data.rearWheels[1] : null
        };
        string[] labels = { "FL", "FR", "RL", "RR" };

        Handles.color = Color.green;

        for (int i = 0; i < 4; i++) {

            if (assignedWheels[i] == null) continue;

            Vector3 pos = assignedWheels[i].transform.position;
            float radius = 0.4f;

            MeshFilter mf = assignedWheels[i].GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                radius = Mathf.Max(mf.sharedMesh.bounds.extents.y, mf.sharedMesh.bounds.extents.z);

            Handles.DrawWireDisc(pos, Vector3.right, radius);
            Handles.Label(pos + Vector3.up * (radius + 0.15f), labels[i], EditorStyles.whiteBoldLabel);

        }

        // Draw unassigned candidates in yellow.
        if (controller.LastDetection.unassigned != null) {

            Handles.color = Color.yellow;

            foreach (GameObject go in controller.LastDetection.unassigned) {

                if (go == null) continue;

                Vector3 pos = go.transform.position;
                float radius = 0.4f;

                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                    radius = Mathf.Max(mf.sharedMesh.bounds.extents.y, mf.sharedMesh.bounds.extents.z);

                Handles.DrawWireDisc(pos, Vector3.right, radius);
                Handles.Label(pos + Vector3.up * (radius + 0.15f), "?", EditorStyles.whiteBoldLabel);

            }

        }

        sceneView.Repaint();

    }

    #endregion

}

#endif
