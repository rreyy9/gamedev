using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility that builds the complete Loot Window UI in one click.
/// Menu: Tools > Loot System > Build Loot Window UI
/// Deletes any existing loot UI objects first, then rebuilds from scratch.
/// </summary>
public static class LootWindowSetup
{
    [MenuItem("Tools/Loot System/Build Loot Window UI")]
    public static void BuildLootWindowUI()
    {
        // Find Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("LootWindowSetup: No Canvas found in scene!");
            return;
        }

        RectTransform canvasRT = canvas.GetComponent<RectTransform>();

        // ──────────────────────────────────────────────────
        //  Clean up existing objects
        // ──────────────────────────────────────────────────
        DestroyExisting("LootWindowPanel");
        DestroyExisting("InteractionPromptPanel");

        // ──────────────────────────────────────────────────
        //  1. BUILD LOOT SLOT PREFAB (saved as asset)
        // ──────────────────────────────────────────────────
        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject slotPrefab = BuildLootSlotPrefab();
        string prefabPath = prefabFolder + "/LootSlotPrefab.prefab";

        // Delete old prefab if exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(slotPrefab, prefabPath);
        Object.DestroyImmediate(slotPrefab);
        Debug.Log($"LootWindowSetup: Created loot slot prefab at {prefabPath}");

        // ──────────────────────────────────────────────────
        //  2. BUILD LOOT WINDOW PANEL
        // ──────────────────────────────────────────────────
        GameObject lootPanel = CreateUIObject("LootWindowPanel", canvasRT);
        RectTransform lootPanelRT = lootPanel.GetComponent<RectTransform>();
        lootPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
        lootPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
        lootPanelRT.pivot = new Vector2(0.5f, 0.5f);
        lootPanelRT.sizeDelta = new Vector2(300, 400);
        lootPanelRT.anchoredPosition = new Vector2(0, 0);

        Image lootPanelBg = lootPanel.AddComponent<Image>();
        lootPanelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        lootPanelBg.raycastTarget = true;

        // Add Outline for border effect
        var outline = lootPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.4f, 0.2f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        // --- Title Bar ---
        GameObject titleBar = CreateUIObject("TitleBar", lootPanelRT);
        RectTransform titleBarRT = titleBar.GetComponent<RectTransform>();
        titleBarRT.anchorMin = new Vector2(0, 1);
        titleBarRT.anchorMax = new Vector2(1, 1);
        titleBarRT.pivot = new Vector2(0.5f, 1);
        titleBarRT.sizeDelta = new Vector2(0, 40);
        titleBarRT.anchoredPosition = Vector2.zero;

        Image titleBarBg = titleBar.AddComponent<Image>();
        titleBarBg.color = new Color(0.15f, 0.12f, 0.08f, 0.95f);
        titleBarBg.raycastTarget = true;

        // Title Text
        GameObject titleTextObj = CreateUIObject("TitleText", titleBarRT);
        RectTransform titleTextRT = titleTextObj.GetComponent<RectTransform>();
        titleTextRT.anchorMin = Vector2.zero;
        titleTextRT.anchorMax = Vector2.one;
        titleTextRT.sizeDelta = new Vector2(-50, 0); // Leave room for close button
        titleTextRT.anchoredPosition = new Vector2(-15, 0);

        TextMeshProUGUI titleTMP = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Loot";
        titleTMP.fontSize = 20;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.9f, 0.8f, 0.5f, 1f); // Gold color
        titleTMP.raycastTarget = false;

        // Close Button
        GameObject closeBtn = CreateUIObject("CloseButton", titleBarRT);
        RectTransform closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 0.5f);
        closeBtnRT.anchorMax = new Vector2(1, 0.5f);
        closeBtnRT.pivot = new Vector2(1, 0.5f);
        closeBtnRT.sizeDelta = new Vector2(30, 30);
        closeBtnRT.anchoredPosition = new Vector2(-5, 0);

        Image closeBtnImg = closeBtn.AddComponent<Image>();
        closeBtnImg.color = new Color(0.6f, 0.15f, 0.15f, 0.9f);
        Button closeBtnComp = closeBtn.AddComponent<Button>();
        closeBtnComp.targetGraphic = closeBtnImg;

        // Close Button "X" text
        GameObject closeText = CreateUIObject("Text", closeBtnRT);
        RectTransform closeTextRT = closeText.GetComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI closeTMP = closeText.AddComponent<TextMeshProUGUI>();
        closeTMP.text = "X";
        closeTMP.fontSize = 16;
        closeTMP.fontStyle = FontStyles.Bold;
        closeTMP.alignment = TextAlignmentOptions.Center;
        closeTMP.color = Color.white;
        closeTMP.raycastTarget = false;

        // --- Scroll View for Slot Container ---
        GameObject scrollArea = CreateUIObject("ScrollArea", lootPanelRT);
        RectTransform scrollAreaRT = scrollArea.GetComponent<RectTransform>();
        scrollAreaRT.anchorMin = new Vector2(0, 0);
        scrollAreaRT.anchorMax = new Vector2(1, 1);
        scrollAreaRT.offsetMin = new Vector2(8, 48); // Bottom padding (above Loot All button)
        scrollAreaRT.offsetMax = new Vector2(-8, -44); // Top padding (below title bar)

        // Slot Container with Vertical Layout
        GameObject slotContainer = CreateUIObject("SlotContainer", scrollAreaRT);
        RectTransform slotContainerRT = slotContainer.GetComponent<RectTransform>();
        slotContainerRT.anchorMin = new Vector2(0, 1);
        slotContainerRT.anchorMax = new Vector2(1, 1);
        slotContainerRT.pivot = new Vector2(0.5f, 1);
        slotContainerRT.sizeDelta = new Vector2(0, 0);
        slotContainerRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = slotContainer.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 4;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        ContentSizeFitter csf = slotContainer.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Loot All Button ---
        GameObject lootAllBtn = CreateUIObject("LootAllButton", lootPanelRT);
        RectTransform lootAllBtnRT = lootAllBtn.GetComponent<RectTransform>();
        lootAllBtnRT.anchorMin = new Vector2(0, 0);
        lootAllBtnRT.anchorMax = new Vector2(1, 0);
        lootAllBtnRT.pivot = new Vector2(0.5f, 0);
        lootAllBtnRT.sizeDelta = new Vector2(-16, 38);
        lootAllBtnRT.anchoredPosition = new Vector2(0, 5);

        Image lootAllBtnImg = lootAllBtn.AddComponent<Image>();
        lootAllBtnImg.color = new Color(0.2f, 0.35f, 0.2f, 0.95f);
        Button lootAllBtnComp = lootAllBtn.AddComponent<Button>();
        lootAllBtnComp.targetGraphic = lootAllBtnImg;

        // Loot All button hover colors
        ColorBlock colors = lootAllBtnComp.colors;
        colors.highlightedColor = new Color(0.25f, 0.5f, 0.25f, 1f);
        colors.pressedColor = new Color(0.15f, 0.25f, 0.15f, 1f);
        lootAllBtnComp.colors = colors;

        GameObject lootAllText = CreateUIObject("Text", lootAllBtnRT);
        RectTransform lootAllTextRT = lootAllText.GetComponent<RectTransform>();
        lootAllTextRT.anchorMin = Vector2.zero;
        lootAllTextRT.anchorMax = Vector2.one;
        lootAllTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI lootAllTMP = lootAllText.AddComponent<TextMeshProUGUI>();
        lootAllTMP.text = "Loot All";
        lootAllTMP.fontSize = 18;
        lootAllTMP.fontStyle = FontStyles.Bold;
        lootAllTMP.alignment = TextAlignmentOptions.Center;
        lootAllTMP.color = new Color(0.85f, 0.9f, 0.85f, 1f);
        lootAllTMP.raycastTarget = false;

        // ──────────────────────────────────────────────────
        //  3. WIRE UP LootUIManager on GameManager
        // ──────────────────────────────────────────────────
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            Debug.LogWarning("LootWindowSetup: No GameManager found. Creating one.");
            gameManager = new GameObject("GameManager");
        }

        LootUIManager lootUIManager = gameManager.GetComponent<LootUIManager>();
        if (lootUIManager == null)
            lootUIManager = gameManager.AddComponent<LootUIManager>();

        // Use SerializedObject to set private [SerializeField] fields
        SerializedObject so = new SerializedObject(lootUIManager);
        so.FindProperty("lootWindowPanel").objectReferenceValue = lootPanel;
        so.FindProperty("titleText").objectReferenceValue = titleTMP;
        so.FindProperty("slotContainer").objectReferenceValue = slotContainerRT;
        so.FindProperty("lootSlotPrefab").objectReferenceValue = savedPrefab;
        so.FindProperty("lootAllButton").objectReferenceValue = lootAllBtnComp;
        so.FindProperty("closeButton").objectReferenceValue = closeBtnComp;
        so.FindProperty("maxLootDistance").floatValue = 5f;
        so.ApplyModifiedProperties();

        // Start disabled — LootUIManager opens it
        lootPanel.SetActive(false);

        Debug.Log("LootWindowSetup: Loot Window Panel built and wired to LootUIManager on GameManager.");

        // ──────────────────────────────────────────────────
        //  4. BUILD INTERACTION PROMPT PANEL
        // ──────────────────────────────────────────────────
        GameObject promptPanel = CreateUIObject("InteractionPromptPanel", canvasRT);
        RectTransform promptPanelRT = promptPanel.GetComponent<RectTransform>();
        // Position above the action bar (bottom-center, offset up)
        promptPanelRT.anchorMin = new Vector2(0.5f, 0);
        promptPanelRT.anchorMax = new Vector2(0.5f, 0);
        promptPanelRT.pivot = new Vector2(0.5f, 0);
        promptPanelRT.sizeDelta = new Vector2(350, 40);
        promptPanelRT.anchoredPosition = new Vector2(0, 80); // Above action bar

        Image promptBg = promptPanel.AddComponent<Image>();
        promptBg.color = new Color(0.05f, 0.05f, 0.1f, 0.8f);
        promptBg.raycastTarget = false;

        // Add CanvasGroup for alpha fade
        CanvasGroup promptCG = promptPanel.AddComponent<CanvasGroup>();
        promptCG.alpha = 0f; // Starts hidden
        promptCG.blocksRaycasts = false;
        promptCG.interactable = false;

        // Prompt Text
        GameObject promptTextObj = CreateUIObject("PromptText", promptPanelRT);
        RectTransform promptTextRT = promptTextObj.GetComponent<RectTransform>();
        promptTextRT.anchorMin = Vector2.zero;
        promptTextRT.anchorMax = Vector2.one;
        promptTextRT.sizeDelta = new Vector2(-16, 0);
        promptTextRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI promptTMP = promptTextObj.AddComponent<TextMeshProUGUI>();
        promptTMP.text = "Press [E] to Interact";
        promptTMP.fontSize = 18;
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.color = new Color(0.95f, 0.85f, 0.5f, 1f); // Gold
        promptTMP.raycastTarget = false;

        // Wire up InteractionPromptUI
        InteractionPromptUI promptUI = promptPanel.AddComponent<InteractionPromptUI>();
        SerializedObject promptSO = new SerializedObject(promptUI);
        promptSO.FindProperty("promptText").objectReferenceValue = promptTMP;
        promptSO.FindProperty("promptFormat").stringValue = "Press [E] {1}";
        promptSO.FindProperty("interactKeyDisplay").stringValue = "E";
        promptSO.ApplyModifiedProperties();

        Debug.Log("LootWindowSetup: Interaction Prompt Panel built and wired.");

        // ──────────────────────────────────────────────────
        //  5. MARK DIRTY & DONE
        // ──────────────────────────────────────────────────
        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("  LOOT WINDOW UI SETUP COMPLETE!");
        Debug.Log("  • LootWindowPanel created under Canvas");
        Debug.Log("  • LootSlotPrefab saved to Assets/Prefabs/");
        Debug.Log("  • InteractionPromptPanel created under Canvas");
        Debug.Log("  • All references wired to LootUIManager");
        Debug.Log("  • Save your scene! (Ctrl+S)");
        Debug.Log("═══════════════════════════════════════════");
    }

    // ──────────────────────────────────────────────────
    //  BUILD LOOT SLOT PREFAB
    // ──────────────────────────────────────────────────
    private static GameObject BuildLootSlotPrefab()
    {
        // Root
        GameObject root = new GameObject("LootSlotPrefab");
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(0, 50);

        LayoutElement le = root.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
        le.flexibleWidth = 1;

        // Background
        GameObject bg = CreateUIObject("Background", rootRT);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        StretchFill(bgRT);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        bgImg.raycastTarget = true;

        // Icon
        GameObject icon = CreateUIObject("Icon", rootRT);
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.sizeDelta = new Vector2(40, 40);
        iconRT.anchoredPosition = new Vector2(6, 0);
        Image iconImg = icon.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;

        // Item Name
        GameObject nameObj = CreateUIObject("ItemNameText", rootRT);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.offsetMin = new Vector2(52, 4);    // Left of text (past icon)
        nameRT.offsetMax = new Vector2(-50, -4);   // Right padding for quantity
        TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "Item Name";
        nameTMP.fontSize = 16;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
        nameTMP.color = Color.white;
        nameTMP.raycastTarget = false;

        // Quantity
        GameObject qtyObj = CreateUIObject("QuantityText", rootRT);
        RectTransform qtyRT = qtyObj.GetComponent<RectTransform>();
        qtyRT.anchorMin = new Vector2(1, 0.5f);
        qtyRT.anchorMax = new Vector2(1, 0.5f);
        qtyRT.pivot = new Vector2(1, 0.5f);
        qtyRT.sizeDelta = new Vector2(45, 30);
        qtyRT.anchoredPosition = new Vector2(-6, 0);
        TextMeshProUGUI qtyTMP = qtyObj.AddComponent<TextMeshProUGUI>();
        qtyTMP.text = "";
        qtyTMP.fontSize = 15;
        qtyTMP.alignment = TextAlignmentOptions.Center;
        qtyTMP.color = new Color(0.8f, 0.8f, 0.6f, 1f);
        qtyTMP.raycastTarget = false;

        // Highlight overlay
        GameObject highlight = CreateUIObject("Highlight", rootRT);
        RectTransform hlRT = highlight.GetComponent<RectTransform>();
        StretchFill(hlRT);
        Image hlImg = highlight.AddComponent<Image>();
        hlImg.color = new Color(1f, 0.9f, 0.3f, 0.15f);
        hlImg.raycastTarget = false;
        highlight.SetActive(false); // Starts hidden

        // Add LootSlotUI component and wire references
        LootSlotUI slotUI = root.AddComponent<LootSlotUI>();
        SerializedObject slotSO = new SerializedObject(slotUI);
        slotSO.FindProperty("iconImage").objectReferenceValue = iconImg;
        slotSO.FindProperty("itemNameText").objectReferenceValue = nameTMP;
        slotSO.FindProperty("quantityText").objectReferenceValue = qtyTMP;
        slotSO.FindProperty("backgroundImage").objectReferenceValue = bgImg;
        slotSO.FindProperty("highlightImage").objectReferenceValue = hlImg;
        slotSO.ApplyModifiedProperties();

        return root;
    }

    // ──────────────────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────────────────
    private static GameObject CreateUIObject(string name, RectTransform parent)
    {
        GameObject obj = new GameObject(name);
        obj.AddComponent<RectTransform>();
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void DestroyExisting(string name)
    {
        // Search in all canvases
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            Transform existing = c.transform.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                Debug.Log($"LootWindowSetup: Removed existing '{name}'");
            }
        }
    }
}
