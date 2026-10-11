using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyBTController controller;
    [SerializeField] private Animator animator;

    private static readonly int StateHash = Animator.StringToHash("State");

    private EnemyBTController.EnemyAction lastAction;
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

        // Kirim nilai enum hanya saat action berubah
        if (initialized && controller.CurrentAction == lastAction)
            return;

        initialized = true;
        lastAction = controller.CurrentAction;

        // Patrol = 0, Chase = 1, Attack = 2, Flee = 3, Dead = 4
        animator.SetInteger(StateHash, (int)lastAction);
    }
}
