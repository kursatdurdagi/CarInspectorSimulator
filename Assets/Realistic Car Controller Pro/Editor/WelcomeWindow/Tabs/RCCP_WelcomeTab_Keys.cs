//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

/// <summary>
/// Keys tab content for the RCCP Welcome Window.
/// Shows all RCCP scripting define symbols (defined and undefined) as a discovery panel.
/// </summary>
public class RCCP_WelcomeTab_Keys : IRCCP_WelcomeTabContent {

    private VisualElement root;

    private struct SymbolEntry {
        public string symbol;
        public string description;
        public bool isDefined;
        public string detectionPath;
    }

    public VisualElement CreateContent() {

        root = new VisualElement();
        Rebuild();
        return root;

    }

    private void Rebuild() {

        if (root == null) return;
        root.Clear();

        var section = RCCP_WelcomeWindowUI.CreateSection(
            "Scripting Define Symbols",
            "These symbols gate addon-specific code. Remove one if its addon package is uninstalled to avoid compile errors."
        );

        section.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
            "Only remove a symbol if you have uninstalled the corresponding addon package.",
            "warning"
        ));

        foreach (var entry in BuildSymbolTable()) {
            section.Add(BuildSymbolRow(entry));
        }

        root.Add(section);

    }

    private static SymbolEntry[] BuildSymbolTable() {

        return new[] {
            new SymbolEntry {
                symbol = "BCG_ENTEREXIT",
                description = "Enables BCG Shared Assets integration — enter/exit vehicles with FPS/TPS character controllers.",
                isDefined = IsSymbolDefined("BCG_ENTEREXIT"),
                detectionPath = "Assets/BoneCracker Games Shared Assets"
            },
            new SymbolEntry {
                symbol = "RCCP_DEMO",
                description = "Enables the Demo Content addon — demo scenes, vehicles, and example setups.",
                isDefined = IsSymbolDefined("RCCP_DEMO"),
                detectionPath = "Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content"
            },
            new SymbolEntry {
                symbol = "RCCP_PHOTON",
                description = "Enables Photon PUN 2 multiplayer integration — online lobby and multiplayer demo scenes.",
                isDefined = IsSymbolDefined("RCCP_PHOTON"),
                detectionPath = "Assets/Realistic Car Controller Pro/Addons/Installed/Photon PUN 2"
            },
            new SymbolEntry {
                symbol = "RCCP_MIRROR",
                description = "Enables Mirror networking integration — blank Mirror multiplayer demo scene.",
                isDefined = IsSymbolDefined("RCCP_MIRROR"),
                detectionPath = "Assets/Realistic Car Controller Pro/Addons/Installed/Mirror"
            },
            new SymbolEntry {
                symbol = "BCG_RTRC",
                description = "Enables Realistic Traffic Controller AI traffic integration.",
                isDefined = IsSymbolDefined("BCG_RTRC"),
                detectionPath = "Assets/Realistic Traffic Controller"
            },
        };

    }

    private VisualElement BuildSymbolRow(SymbolEntry entry) {

        bool folderMissing = !string.IsNullOrEmpty(entry.detectionPath) && !Directory.Exists(entry.detectionPath);
        bool orphaned = entry.isDefined && folderMissing;

        if (entry.isDefined) {

            string description = orphaned
                ? entry.description + "\n⚠ Addon folder not found — safe to remove this symbol."
                : entry.description;

            var card = RCCP_WelcomeWindowUI.CreateAddonCard(
                entry.symbol,
                description,
                false,
                "Remove Symbol",
                () => {
                    bool confirm = EditorUtility.DisplayDialog(
                        "Realistic Car Controller Pro | Remove Scripting Symbol",
                        $"Remove '{entry.symbol}' from the scripting define symbols?\n\n" +
                        "Only do this if you have already deleted the addon's folder. " +
                        "Unity will recompile all scripts after removal.",
                        "Remove",
                        "Cancel"
                    );
                    if (!confirm) return;
                    RCCP_SetScriptingSymbol.SetEnabled(entry.symbol, false);
                    Rebuild();
                }
            );
            card.tooltip = entry.description;
            return card;

        }

        // Undefined: show greyed-out state with tooltip.
        var undefinedCard = RCCP_WelcomeWindowUI.CreateAddonCard(
            entry.symbol,
            entry.description,
            true, "", null
        );
        undefinedCard.SetEnabled(false);
        undefinedCard.tooltip = "Not defined. Symbol will activate when the corresponding addon is imported.";

        // Override the "Installed" label — replace with "Not defined".
        var statusLabel = undefinedCard.Q<Label>(className: "rccp-welcome-addon-card__status");
        if (statusLabel != null) {
            statusLabel.text = "Not defined";
            statusLabel.RemoveFromClassList("rccp-welcome-addon-card__status--installed");
            statusLabel.AddToClassList("rccp-welcome-addon-card__status--available");
        }

        return undefinedCard;

    }

    private static bool IsSymbolDefined(string symbol) {

        var group = UnityEditor.BuildPipeline.GetBuildTargetGroup(UnityEditor.EditorUserBuildSettings.activeBuildTarget);

#if UNITY_2023_1_OR_NEWER
        var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
        string defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbols(namedTarget);
#else
        string defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif

        if (string.IsNullOrEmpty(defines))
            return false;

        foreach (string d in defines.Split(';')) {
            if (d.Trim() == symbol)
                return true;
        }

        return false;

    }

    public void OnActivated() {

        Rebuild();

    }

    public void OnDeactivated() { }

}

#endif
