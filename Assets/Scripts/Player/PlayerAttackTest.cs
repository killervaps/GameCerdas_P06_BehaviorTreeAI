using UnityEngine;

public class PlayerAttackTest : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemy;
    [SerializeField] private float attackDistance = 3f;
    [SerializeField] private float damage = 20f;

    [SerializeField] private Animator animator;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private PlayerHealth health;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (animator != null)
                animator.SetTrigger(AttackHash);

            TryAttackEnemy();
        }
    }

    private void TryAttackEnemy()
    {
        if (enemy == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

        if (distance <= attackDistance)
        {
            enemy.TakeDamage(damage);

            Debug.Log(
                "Player attacks Enemy!"
            );
        }
    }
}
