using UnityEngine;

public class PlayerGatheringController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GatheringCastBarUI castBar; // optional — Stage 6

    private static readonly int IsMiningHash = Animator.StringToHash("IsMining");

public void StartMining(float duration)
    {
        Debug.Log($"[PGC] StartMining called. Duration={duration}. Animator={(animator != null ? animator.gameObject.name : "NULL")}");
        if (animator == null) { Debug.LogError("[PGC] animator is NULL — drag PlayerModel into the Animator field on PlayerGatheringController!"); return; }
        animator.SetBool(IsMiningHash, true);
        Debug.Log($"[PGC] IsMining set to TRUE. ToolLayer weight check needed if still no anim.");
        castBar?.Show(duration);
    }

public void StopMining()
    {
        Debug.Log("[PGC] StopMining called.");
        if (animator == null) return;
        animator.SetBool(IsMiningHash, false);
        castBar?.Hide();
    }
}