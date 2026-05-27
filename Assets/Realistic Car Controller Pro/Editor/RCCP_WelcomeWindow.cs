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
using BoneCrackerGames.RCCP.CoreProtection;

/// <summary>
/// Welcome Window for Realistic Car Controller Pro.
/// Provides quick start guide, demo scenes, addon management, and documentation links.
/// </summary>
public class RCCP_WelcomeWindow : EditorWindow {

    #region Variables

    private RCCP_WelcomeWindowController controller;

    private VisualElement tabContainer;
    private VisualElement contentContainer;
    private VisualElement graceBanner;
    private VisualElement modalOverlay;
    private VisualElement modalContainer;

    private bool forceShowVerification;
    private bool showFirstRunSetup;

    private const int windowWidth = 760;
    private const int windowHeight = 680;

    #endregion

    #region Menu Items

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Welcome Window", false, 0)]
    [MenuItem("GameObject/BoneCracker Games/Realistic Car Controller Pro/Welcome Window", false, 0)]
    public static void OpenWindow() {

        GetWindow<RCCP_WelcomeWindow>(true);

    }

    /// <summary>
    /// Opens the Welcome Window with the first-run setup panel visible.
    /// Called from RCCP_InitLoad on first import.
    /// </summary>
    public static void OpenWindowFirstRun() {

        var window = GetWindow<RCCP_WelcomeWindow>(true);
        window.showFirstRunSetup = true;
        window.ShowModal(window.CreateFirstRunModal());

    }

    /// <summary>
    /// Opens the Welcome Window with the verification panel visible.
    /// </summary>
    public static void OpenWindowWithVerification() {

        var window = GetWindow<RCCP_WelcomeWindow>(true);
        window.forceShowVerification = true;
        window.ShowModal(window.CreateVerificationModal());

    }

    #endregion

    #region Lifecycle

    private void OnEnable() {

        titleContent = new GUIContent("Realistic Car Controller Pro");
        // 760x680 matches the Setup Wizard so both windows share the same
        // 220px-sidebar shell footprint and fit on common low-res laptops
        // (1366x768 with title bar + Windows taskbar). Note: Unity ignores
        // minSize when the window is docked, so the sidebar tab list also
        // scrolls (see rccp_welcome_window.uxml's tab-scroll ScrollView) to
        // keep the footer in view at small docked heights.
        minSize = new Vector2(windowWidth, windowHeight);

        // set_position throws NRE when m_Parent (HostView) isn't wired yet —
        // can happen if something opens the window during compilation or asset
        // import. Defer the size reset to the next editor tick when unsettled.
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            EditorApplication.delayCall += ResetWindowSize;
        else
            ResetWindowSize();

        controller = new RCCP_WelcomeWindowController();
        controller.OnVerificationStateChanged += OnVerificationStateChanged;

        BuildUI();

    }

    private void ResetWindowSize() {

        // Window may have been closed before delayCall fires.
        if (this == null) return;

        var r = position;
        r.width = windowWidth;
        r.height = windowHeight;
        position = r;

    }

    private void OnDisable() {

        if (controller != null)
            controller.OnVerificationStateChanged -= OnVerificationStateChanged;

    }

    #endregion

    #region UI Construction

    private void BuildUI() {

        rootVisualElement.Clear();

        var root = RCCP_WelcomeWindowUI.CreateShell(
            out tabContainer,
            out contentContainer,
            out graceBanner,
            out modalOverlay,
            out modalContainer
        );

        rootVisualElement.Add(root);

        // Populate tab bar.
        RCCP_WelcomeWindowUI.PopulateTabBar(
            tabContainer,
            controller.TabNames,
            controller.CurrentTabIndex,
            OnTabSelected
        );

        // Wire grace banner.
        UpdateGraceBanner();

        var verifyButton = root.Q<Button>("verify-now-button");
        if (verifyButton != null)
            verifyButton.clicked += () => ShowModal(CreateVerificationModal());

        // Load current tab content.
        LoadTabContent();

        // Show modals if requested.
        if (forceShowVerification && !RCCP_CoreServerProxy.IsVerified)
            ShowModal(CreateVerificationModal());
        else if (showFirstRunSetup)
            ShowModal(CreateFirstRunModal());

    }

    private void LoadTabContent() {

        if (contentContainer == null) return;

        contentContainer.Clear();

        var provider = controller.GetContentProvider(controller.CurrentTabIndex);
        var content = provider.CreateContent();
        contentContainer.Add(content);
        provider.OnActivated();

    }

    #endregion

    #region Tab Navigation

    private void OnTabSelected(int index) {

        // Deactivate current tab.
        var currentProvider = controller.GetContentProvider(controller.CurrentTabIndex);
        currentProvider.OnDeactivated();

        // Switch tab.
        controller.CurrentTabIndex = index;
        RCCP_WelcomeWindowUI.UpdateTabButtons(tabContainer, index);

        // Load new tab content.
        LoadTabContent();

    }

    #endregion

    #region Grace Period Banner

    private void UpdateGraceBanner() {

        if (graceBanner == null) return;

        graceBanner.style.display = RCCP_CoreServerProxy.IsVerified
            ? DisplayStyle.None
            : DisplayStyle.Flex;

    }

    #endregion

    #region Modal System

    private void ShowModal(VisualElement content) {

        if (modalOverlay == null || modalContainer == null) return;

        modalContainer.Clear();
        modalContainer.Add(content);
        modalOverlay.style.display = DisplayStyle.Flex;

    }

    private void HideModal() {

        if (modalOverlay == null) return;

        modalOverlay.style.display = DisplayStyle.None;
        modalContainer.Clear();

        forceShowVerification = false;
        showFirstRunSetup = false;

        UpdateGraceBanner();

    }

    #endregion

    #region Verification Modal

    private VisualElement CreateVerificationModal() {

        var panel = new VisualElement();
        panel.AddToClassList("rccp-welcome-modal-panel");

        var title = new Label("Purchase Verification");
        title.AddToClassList("rccp-welcome-modal-panel__title");
        panel.Add(title);

        var subtitle = new Label("Enter your Unity Asset Store invoice number to verify your purchase.");
        subtitle.AddToClassList("rccp-welcome-modal-panel__subtitle");
        panel.Add(subtitle);

        // Invoice input.
        var invoiceField = new TextField("Invoice Number");
        invoiceField.value = controller.InvoiceInput;
        invoiceField.RegisterValueChangedCallback(evt => controller.InvoiceInput = evt.newValue);
        panel.Add(invoiceField);

        // Message label.
        var messageLabel = new Label();
        messageLabel.AddToClassList("rccp-welcome-modal-message");
        messageLabel.style.display = DisplayStyle.None;
        panel.Add(messageLabel);

        // Verify button.
        var verifyButton = RCCP_WelcomeWindowUI.CreateButton("Verify Purchase", () => {
            controller.StartCoreVerification();
        }, "primary");
        verifyButton.name = "verify-button";
        panel.Add(verifyButton);

        // Find invoice link.
        var findInvoiceButton = RCCP_WelcomeWindowUI.CreateButton("How to find my invoice number?", () => {
            Application.OpenURL("https://assetstore.unity.com/orders");
        }, "link");
        panel.Add(findInvoiceButton);

        panel.Add(RCCP_WelcomeWindowUI.CreateSeparator());

        // Back button.
        var backButton = RCCP_WelcomeWindowUI.CreateButton("Back", () => HideModal());
        panel.Add(backButton);

        // Store references for state updates.
        verifyButton.userData = (messageLabel, verifyButton);

        // Wire Enter key.
        invoiceField.RegisterCallback<KeyDownEvent>(evt => {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) {
                controller.StartCoreVerification();
                evt.StopPropagation();
            }
        });

        return panel;

    }

    private void OnVerificationStateChanged() {

        if (modalContainer == null) return;

        var verifyBtn = modalContainer.Q<Button>("verify-button");
        var messageLabel = modalContainer.Q<Label>(className: "rccp-welcome-modal-message");

        if (verifyBtn != null) {
            verifyBtn.text = controller.IsVerifying ? "Verifying..." : "Verify Purchase";
            verifyBtn.SetEnabled(!controller.IsVerifying);
        }

        if (messageLabel != null && !string.IsNullOrEmpty(controller.VerificationMessage)) {
            messageLabel.text = controller.VerificationMessage;
            messageLabel.style.display = DisplayStyle.Flex;
            messageLabel.RemoveFromClassList("rccp-welcome-modal-message--error");
            messageLabel.RemoveFromClassList("rccp-welcome-modal-message--success");
            messageLabel.AddToClassList(controller.IsErrorMessage
                ? "rccp-welcome-modal-message--error"
                : "rccp-welcome-modal-message--success");
        }

        // Auto-close on success.
        if (!controller.IsVerifying && controller.IsVerified) {
            EditorApplication.delayCall += () => {
                HideModal();
                LoadTabContent();
            };
        }

    }

    #endregion

    #region First-Run Modal

    private VisualElement CreateFirstRunModal() {

        var panel = new VisualElement();
        panel.AddToClassList("rccp-welcome-modal-panel");

        var title = new Label("Welcome to RCCP!");
        title.AddToClassList("rccp-welcome-modal-panel__title");
        panel.Add(title);

        var subtitle = new Label("Let's set up a few things to get you started.");
        subtitle.AddToClassList("rccp-welcome-modal-panel__subtitle");
        panel.Add(subtitle);

        // Input System check.
        bool inputInstalled = controller.IsInputSystemInstalled();

        if (inputInstalled) {
            panel.Add(RCCP_WelcomeWindowUI.CreateHelpBox("Input System package is installed.", "success"));
        } else {
            panel.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
                "Input System package is required. Install it from Package Manager.",
                "warning"
            ));
            panel.Add(RCCP_WelcomeWindowUI.CreateButton("Open Package Manager", () => {
                UnityEditor.PackageManager.UI.Window.Open("com.unity.inputsystem");
            }, "primary"));
        }

        panel.Add(RCCP_WelcomeWindowUI.CreateSeparator());

        // Demo assets.
#if !RCCP_DEMO
        panel.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
            "Demo assets are available. Import them to explore example scenes and vehicles.",
            "info"
        ));
        panel.Add(RCCP_WelcomeWindowUI.CreateButton("Import Demo Assets", () => {
            RCCP_WelcomeWindowController.ImportPackageSafe(RCCP_AddonPackages.Instance.demoPackage, "Demo Content");
        }, "primary"));
#else
        panel.Add(RCCP_WelcomeWindowUI.CreateHelpBox("Demo assets are already installed.", "success"));
#endif

        panel.Add(RCCP_WelcomeWindowUI.CreateSeparator());

        // Continue button.
        panel.Add(RCCP_WelcomeWindowUI.CreateButton("Continue to Welcome Window", () => {
            HideModal();
        }, "primary"));

        return panel;

    }

    #endregion

}

#endif
