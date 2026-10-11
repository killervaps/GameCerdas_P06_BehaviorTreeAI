using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyBTController controller;
    [SerializeField] private Animator animator;

    private static readonly int StateHash = Animator.StringToHash("State");

    private int lastState;
    private bool initialized;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponentInParent<EnemyBTController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (controller == null || animator == null)
            return;

        int state = ToAnimatorState(controller.CurrentAction);

        // Kirim nilai hanya saat berubah
        if (initialized && state == lastState)
            return;

        initialized = true;
        lastState = state;

        // Patrol = 0, Chase = 1, Attack = 2, Flee = 3, Dead = 4, Search = 5
        animator.SetInteger(StateHash, state);
    }

    private int ToAnimatorState(EnemyBTController.EnemyAction action)
    {
        // Search punya dua tampilan: saat berjalan ke last known position
        // memakai animasi jalan (Patrol = 0), saat menoleh memakai Idle (5).
        if (action == EnemyBTController.EnemyAction.Search &&
            !controller.IsScanning)
        {
            return (int)EnemyBTController.EnemyAction.Patrol;
        }

        return (int)action;
    }
}
