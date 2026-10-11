using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBTController : MonoBehaviour
{
    // Nilai numerik HARUS sama dengan parameter integer "State" pada Animator
    // (EnemyAnimation mengirim (int)CurrentAction ke Animator).
    public enum EnemyAction
    {
        Patrol = 0,
        Chase = 1,
        Attack = 2,
        Flee = 3,
        Dead = 4
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform safePoint;
    [SerializeField] private EnemyHealth health;

    [Header("Perception")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackExitRange = 2.75f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;

    [Header("Flee")]
    [SerializeField] private float lowHealthThreshold = 30f;
    [SerializeField] private float safeDistance = 12f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float fleeSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private EnemyAction activeAction = EnemyAction.Patrol;

    private NavMeshAgent agent;
    private BTNode rootNode;
    private PlayerHealth playerHealth;

    private int currentPatrolIndex = 0;

    // Flee hanya dipicu sekali, sama seperti fleeTriggered pada FSM.
    private bool fleeCompleted = false;

    public EnemyAction CurrentAction => activeAction;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<EnemyHealth>();

        if (health == null)
        {
            Debug.LogWarning(
                name
                + ": EnemyHealth belum terpasang, ditambahkan otomatis. "
                + "Tambahkan komponen EnemyHealth lewat Inspector."
            );

            health = gameObject.AddComponent<EnemyHealth>();
        }

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        BuildBehaviorTree();
    }

    private void Update()
    {
        if (rootNode != null)
        {
            rootNode.Tick();
        }
    }

    private void BuildBehaviorTree()
    {
        // ------------------------------
        // DEAD
        // ------------------------------

        BTNode deadSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(IsDead),
                    new ActionNode(Die)
                }
            );

        // ------------------------------
        // FLEE
        // ------------------------------

        BTNode fleeSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(ShouldFlee),
                    new ActionNode(Flee)
                }
            );

        // ------------------------------
        // ATTACK
        // ------------------------------

        // Cooldown hanya membungkus pukulan; EngageTarget dijalankan setiap tick agar enemy tetap berhenti dan menghadap player selama menunggu cooldown (seperti UpdateAttack pada FSM).
        BTNode strikeWithCooldown =
            new CooldownDecorator(
                new ActionNode(StrikePlayer),
                attackCooldown
            );

        BTNode attackSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(CanSeePlayer),
                    new ConditionNode(IsPlayerInAttackRange),
                    new ActionNode(EngageTarget),
                    strikeWithCooldown
                }
            );

        // ------------------------------
        // CHASE
        // ------------------------------

        BTNode chaseSequence =
            new SequenceNode(
                new List<BTNode>
                {
                    new ConditionNode(CanSeePlayer),
                    new ActionNode(ChasePlayer)
                }
            );

        // ------------------------------
        // PATROL
        // ------------------------------

        BTNode patrolAction =
            new ActionNode(Patrol);

        // ------------------------------
        // ROOT SELECTOR
        // ------------------------------

        rootNode =
            new SelectorNode(
                new List<BTNode>
                {
                    deadSequence,
                    fleeSequence,
                    attackSequence,
                    chaseSequence,
                    patrolAction
                }
            );
    }

    // ==================================================
    // CONDITIONS
    // ==================================================

    private bool IsDead()
    {
        return health.IsDead;
    }

    private bool ShouldFlee()
    {
        return !fleeCompleted &&
               health.CurrentHealth <= lowHealthThreshold;
    }

    private bool IsPlayerInAttackRange()
    {
        if (player == null)
            return false;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // Histeresis seperti FSM: enemy yang sedang menyerang baru dianggap keluar jangkauan setelah melewati attackExitRange.
        float range =
            activeAction == EnemyAction.Attack
                ? attackExitRange
                : attackRange;

        return distance <= range;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        // Player yang sudah mati tidak lagi terlihat oleh enemy.
        if (playerHealth != null && playerHealth.IsDead)
            return false;

        Vector3 eyePosition =
            transform.position
            + Vector3.up * 1.5f;

        Vector3 targetPosition =
            player.position
            + Vector3.up * 1f;

        Vector3 directionToPlayer =
            targetPosition - eyePosition;

        float distanceToPlayer =
            directionToPlayer.magnitude;

        // Player terlalu jauh.
        if (distanceToPlayer > visionRange)
            return false;

        // Player berada di luar Field of View.
        float angle =
            Vector3.Angle(
                transform.forward,
                directionToPlayer
            );

        if (angle > visionAngle * 0.5f)
            return false;

        // Periksa apakah ada obstacle.
        bool blocked =
            Physics.Raycast(
                eyePosition,
                directionToPlayer.normalized,
                distanceToPlayer,
                obstacleMask
            );

        return !blocked;
    }

    // ==================================================
    // ACTIONS
    // ==================================================

    private NodeState Patrol()
    {
        SetAction(EnemyAction.Patrol);

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return NodeState.Failure;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;

        Transform target =
            patrolPoints[currentPatrolIndex];

        agent.SetDestination(target.position);

        if (!agent.pathPending &&
            agent.remainingDistance <= 0.5f)
        {
            currentPatrolIndex++;

            if (currentPatrolIndex
                >= patrolPoints.Length)
            {
                currentPatrolIndex = 0;
            }
        }

        return NodeState.Running;
    }

    private NodeState ChasePlayer()
    {
        if (player == null)
            return NodeState.Failure;

        SetAction(EnemyAction.Chase);

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance =
            attackRange * 0.8f;

        agent.SetDestination(player.position);

        return NodeState.Running;
    }

    // Berhenti dan menghadap player. Dijalankan setiap tick selama player berada dalam jangkauan serang.
    private NodeState EngageTarget()
    {
        if (player == null)
            return NodeState.Failure;

        SetAction(EnemyAction.Attack);

        agent.isStopped = true;

        FacePlayer();

        return NodeState.Success;
    }

    // Pukulan sebenarnya; hanya dipanggil saat cooldown sudah habis.
    private NodeState StrikePlayer()
    {
        Debug.Log(
            name
            + " attacks Player! Damage = "
            + attackDamage
        );

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                attackDamage
            );
        }

        return NodeState.Success;
    }

    private NodeState Flee()
    {
        if (safePoint == null)
            return NodeState.Failure;

        SetAction(EnemyAction.Flee);

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0.5f;

        agent.SetDestination(
            safePoint.position
        );

        float playerDistance =
            player != null
                ? Vector3.Distance(
                    transform.position,
                    player.position
                )
                : Mathf.Infinity;

        float safePointDistance =
            Vector3.Distance(
                transform.position,
                safePoint.position
            );

        // Selesai jika sudah cukup jauh dari player atau sudah sampai di safe point (kondisi yang sama dengan FSM).
        if (playerDistance >= safeDistance ||
            safePointDistance <= 1f)
        {
            fleeCompleted = true;

            return NodeState.Success;
        }

        return NodeState.Running;
    }

    private NodeState Die()
    {
        SetAction(EnemyAction.Dead);

        if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        return NodeState.Success;
    }

    private void SetAction(EnemyAction action)
    {
        if (activeAction == action)
            return;

        activeAction = action;

        Debug.Log(
            name
            + " → State: "
            + activeAction
        );
    }

    private void FacePlayer()
    {
        Vector3 direction =
            player.position
            - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude
            < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
    }

    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        // Vision Range
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            visionRange
        );

        // Attack Range
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        // Attack Exit Range
        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            transform.position,
            attackExitRange
        );

        // FOV direction
        Gizmos.color = Color.yellow;

        Vector3 leftDirection =
            Quaternion.Euler(
                0,
                -visionAngle * 0.5f,
                0
            )
            * transform.forward;

        Vector3 rightDirection =
            Quaternion.Euler(
                0,
                visionAngle * 0.5f,
                0
            )
            * transform.forward;

        Gizmos.DrawRay(
            transform.position,
            leftDirection * visionRange
        );

        Gizmos.DrawRay(
            transform.position,
            rightDirection * visionRange
        );
    }
}
