
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility — run via Tools > Gathering > Build Cast Bar UI
/// Creates the GatheringCastBarPanel in the HUDCanvas and wires all
/// serialized references on GatheringCastBarUI and PlayerGatheringController.
/// Safe to re-run: deletes any existing GatheringCastBarPanel first.
/// </summary>
public static class GatheringCastBarSetup
{
    [MenuItem("Tools/Gathering/Build Cast Bar UI")]
    public static void BuildCastBarUI()
    {
        // ── 1. Find HUDCanvas ─────────────────────────────────────────
        Canvas hudCanvas = Object.FindFirstObjectByType<Canvas>();
        if (hudCanvas == null)
        {
            Debug.LogError("[GatheringCastBarSetup] No Canvas found in scene.");
            return;
        }

        // Prefer the one named HUDCanvas if multiple exist
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.name == "HUDCanvas") { hudCanvas = c; break; }
        }

        // ── 2. Remove ALL stale panels if they exist ─────────────────────
        // Collect first to avoid mutating the collection during iteration
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in hudCanvas.transform)
        {
            if (child.name == "GatheringCastBarPanel")
                toDestroy.Add(child.gameObject);
        }
        bool removedAny = toDestroy.Count > 0;
        foreach (var go in toDestroy)
            Object.DestroyImmediate(go);
        if (removedAny)
            Debug.Log("[GatheringCastBarSetup] Removed all existing GatheringCastBarPanel(s).");

        // ── 3. Create panel ───────────────────────────────────────────
        GameObject panel = new GameObject("GatheringCastBarPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(hudCanvas.transform, false);
        panel.layer = LayerMask.NameToLayer("UI");

        RectTransform panelRT = panel.GetComponent<RectTransform>();
        // Bottom-center, 420x50, 120px above bottom
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot     = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 120f);
        panelRT.sizeDelta = new Vector2(420f, 50f);

        // Dark semi-transparent background
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        // ── 4. Create fill bar ────────────────────────────────────────
        GameObject fillGO = new GameObject("CastBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGO.transform.SetParent(panel.transform, false);
        fillGO.layer = LayerMask.NameToLayer("UI");

        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(4f, 4f);
        fillRT.offsetMax = new Vector2(-4f, -4f);

        Image fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(1f, 0.65f, 0f, 1f); // Gold/orange
        fillImg.type  = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;

        // ── 5. Create label ───────────────────────────────────────────
        GameObject labelGO = new GameObject("CastBarLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(panel.transform, false);
        labelGO.layer = LayerMask.NameToLayer("UI");

        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text      = "Mining...";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize  = 18f;
        label.color     = Color.white;
        label.fontStyle = FontStyles.Bold;

        // ── 6. Add & wire GatheringCastBarUI on panel ─────────────────
        GatheringCastBarUI castBarUI = panel.AddComponent<GatheringCastBarUI>();
        SerializedObject so = new SerializedObject(castBarUI);
        so.FindProperty("panel").objectReferenceValue   = panel;
        so.FindProperty("fillBar").objectReferenceValue = fillImg;
        so.FindProperty("label").objectReferenceValue   = label;
        so.ApplyModifiedProperties();

        // ── 7. Disable panel by default ───────────────────────────────
        panel.SetActive(false);

        // ── 8. Wire castBar into PlayerGatheringController ────────────
        PlayerGatheringController pgc = Object.FindFirstObjectByType<PlayerGatheringController>();
        if (pgc != null)
        {
            SerializedObject pgcSO = new SerializedObject(pgc);
            pgcSO.FindProperty("castBar").objectReferenceValue = castBarUI;
            pgcSO.ApplyModifiedProperties();
            Debug.Log("[GatheringCastBarSetup] Wired castBar -> PlayerGatheringController.");
        }
        else
        {
            Debug.LogWarning("[GatheringCastBarSetup] PlayerGatheringController not found — wire castBar manually.");
        }

        // ── 9. Wire animator into PlayerGatheringController ───────────
        if (pgc != null)
        {
            SerializedObject pgcSO = new SerializedObject(pgc);
            // Find the Animator on the Player's child model
            Animator anim = pgc.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                pgcSO.FindProperty("animator").objectReferenceValue = anim;
                pgcSO.ApplyModifiedProperties();
                Debug.Log($"[GatheringCastBarSetup] Wired animator ({anim.gameObject.name}) -> PlayerGatheringController.");
            }
            else
            {
                Debug.LogWarning("[GatheringCastBarSetup] No Animator found under Player — wire animator manually.");
            }
        }

        // ── 10. Save & mark dirty ─────────────────────────────────────
        EditorUtility.SetDirty(panel);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[GatheringCastBarSetup] ✅ Cast Bar UI built and wired successfully!");
    }
}
#endif
