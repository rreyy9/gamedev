using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GatheringCastBarUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image fillBar;
    [SerializeField] private TextMeshProUGUI label;

    public void Show(float duration, string actionName = "Mining")
    {
        panel.SetActive(true);
        label.text = actionName;
        StartCoroutine(FillRoutine(duration));
    }

    public void Hide()
    {
        StopAllCoroutines();
        panel.SetActive(false);
        fillBar.fillAmount = 0f;
    }

    private IEnumerator FillRoutine(float duration)
    {
        float elapsed = 0f;
        fillBar.fillAmount = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fillBar.fillAmount = elapsed / duration;
            yield return null;
        }
        fillBar.fillAmount = 1f;
    }
}