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

/// <summary>
/// Step 5: Body collider selection for the RCCP Setup Wizard.
/// Scans vehicle meshes and lets user select which parts get MeshColliders.
/// </summary>
public class RCCP_SetupStep_BodyColliders : IRCCP_SetupWizardStep {

    public VisualElement CreateContent(RCCP_SetupWizardController controller) {

        var root = new VisualElement();

        var section = RCCP_WelcomeWindowUI.CreateSection("Body Colliders",
            "Select which body mesh parts should receive MeshColliders. Wheel meshes are automatically excluded.");

        // Select All / None.
        section.Add(RCCP_WelcomeWindowUI.CreateButtonRow(
            ("Select All", () => { controller.SetAllColliders(true); Rebuild(root, controller); }, "success"),
            ("Select None", () => { controller.SetAllColliders(false); Rebuild(root, controller); }, "danger")
        ));

        // Convex toggle.
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Convex Colliders", "Required for Rigidbody interaction", controller.ColliderConvex, v => controller.ColliderConvex = v));

        section.Add(RCCP_WelcomeWindowUI.CreateSeparator());

        // Collider candidates list.
        if (controller.ColliderCandidates.Count == 0) {

            section.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
                "No mesh candidates found on this vehicle.",
                "warning"
            ));

        } else {

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.maxHeight = 300;

            for (int i = 0; i < controller.ColliderCandidates.Count; i++) {

                var candidate = controller.ColliderCandidates[i];
                if (candidate == null) continue;

                string meshName = candidate.name;
                string volumeText = "";

                MeshFilter mf = candidate.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) {
                    Vector3 size = mf.sharedMesh.bounds.size;
                    float volume = size.x * size.y * size.z;
                    volumeText = $"Vol: {volume:F3}";
                }

                int index = i;
                bool selected = i < controller.ColliderSelected.Length && controller.ColliderSelected[i];

                section.Add(RCCP_SetupWizardUI.CreateColliderRow(meshName, selected, volumeText, v => {
                    if (index < controller.ColliderSelected.Length)
                        controller.ColliderSelected[index] = v;
                }));

            }

        }

        section.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
            "Meshes are sorted by volume (largest first). Parts above 10% of the largest volume are auto-selected.",
            "info"
        ));

        root.Add(section);

        return root;

    }

    private void Rebuild(VisualElement root, RCCP_SetupWizardController controller) {

        root.Clear();
        root.Add(CreateContent(controller));

    }

    public void OnStepEntered(RCCP_SetupWizardController controller) {

        if (!controller.ColliderCandidatesRefreshed) {
            controller.RefreshColliderCandidates();
            controller.ColliderCandidatesRefreshed = true;
        }

    }

}

#endif
