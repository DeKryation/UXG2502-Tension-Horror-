using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public enum MonsterState
    {
        Patrol,
        Investigate,
        Search,
        Chase,
        Capture
    }

    [Header("References")]
    public Transform player;
    public LayerMask wallLayer;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float investigateSpeed = 2.5f;
    public float chaseSpeed = 4.5f;

    [Header("Vision")]
    public float viewDistance = 6f;
    [Range(0f, 360f)]
    public float viewAngle = 90f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolPointReachDistance = 0.3f;
    public float patrolWaitTime = 1f;

    [Header("Search")]
    public float searchDuration = 4f;
    public float searchRadius = 2f;

    [Header("Capture")]
    public float captureDistance = 0.8f;

    [Header("Debug")]
    public MonsterState currentState;

    private Rigidbody2D rb;

    private int patrolIndex = 0;
    private float patrolWaitTimer;

    private Vector2 lastKnownPlayerPosition;
    private Vector2 investigatePosition;

    private float searchTimer;

    private bool playerVisible;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        currentState = MonsterState.Patrol;
    }

    void Update()
    {
        UpdateVision();

        StateTransitions();
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case MonsterState.Patrol:
                Patrol();
                break;

            case MonsterState.Investigate:
                Investigate();
                break;

            case MonsterState.Search:
                Search();
                break;

            case MonsterState.Chase:
                Chase();
                break;

            case MonsterState.Capture:
                Capture();
                break;
        }
    }

    void StateTransitions()
    {
        if (playerVisible)
        {
            lastKnownPlayerPosition = player.position;

            if (currentState != MonsterState.Capture)
            {
                currentState = MonsterState.Chase;
            }
        }

        if (currentState == MonsterState.Chase)
        {
            float distance = Vector2.Distance(
                transform.position,
                player.position
            );

            if (distance <= captureDistance)
            {
                currentState = MonsterState.Capture;
            }

            if (!playerVisible)
            {
                currentState = MonsterState.Search;
                searchTimer = searchDuration;
            }
        }
    }

    // =========================
    // PATROL
    // =========================

    void Patrol()
    {
        if (patrolPoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Transform targetPoint = patrolPoints[patrolIndex];

        float distance = Vector2.Distance(
            rb.position,
            targetPoint.position
        );

        if (distance <= patrolPointReachDistance)
        {
            rb.linearVelocity = Vector2.zero;

            patrolWaitTimer += Time.fixedDeltaTime;

            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;

                patrolIndex++;

                if (patrolIndex >= patrolPoints.Length)
                    patrolIndex = 0;
            }

            return;
        }

        MoveTowards(
            targetPoint.position,
            patrolSpeed
        );
    }

    // =========================
    // INVESTIGATE
    // =========================

    void Investigate()
    {
        float distance = Vector2.Distance(
            rb.position,
            investigatePosition
        );

        if (distance <= 0.3f)
        {
            currentState = MonsterState.Search;
            searchTimer = searchDuration;

            rb.linearVelocity = Vector2.zero;
            return;
        }

        MoveTowards(
            investigatePosition,
            investigateSpeed
        );
    }

    // =========================
    // SEARCH
    // =========================

    void Search()
    {
        searchTimer -= Time.fixedDeltaTime;

        float distance = Vector2.Distance(
            rb.position,
            lastKnownPlayerPosition
        );

        if (distance > 0.4f)
        {
            MoveTowards(
                lastKnownPlayerPosition,
                investigateSpeed
            );
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (searchTimer <= 0f)
        {
            currentState = MonsterState.Patrol;
        }
    }

    // =========================
    // CHASE
    // =========================

    void Chase()
    {
        if (player == null)
            return;

        lastKnownPlayerPosition = player.position;

        MoveTowards(
            player.position,
            chaseSpeed
        );
    }

    // =========================
    // CAPTURE
    // =========================

    void Capture()
    {
        rb.linearVelocity = Vector2.zero;

        Debug.Log("PLAYER CAPTURED");

        // Later:
        // Start QTE
        // Lock player movement
        // Trigger capture animation
        // Trigger capture audio
    }

    // =========================
    // MOVEMENT
    // =========================

    void MoveTowards(
        Vector2 target,
        float speed
    )
    {
        Vector2 direction =
            (target - rb.position).normalized;

        rb.linearVelocity =
            direction * speed;
    }

    // =========================
    // VISION
    // =========================

    void UpdateVision()
    {
        if (player == null)
        {
            playerVisible = false;
            return;
        }

        Vector2 directionToPlayer =
            ((Vector2)player.position - rb.position);

        float distanceToPlayer =
            directionToPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
        {
            playerVisible = false;
            return;
        }

        Vector2 monsterForward =
            rb.linearVelocity.normalized;

        if (monsterForward == Vector2.zero)
        {
            monsterForward = Vector2.right;
        }

        float angle = Vector2.Angle(
            monsterForward,
            directionToPlayer
        );

        if (angle > viewAngle / 2f)
        {
            playerVisible = false;
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            directionToPlayer.normalized,
            distanceToPlayer,
            wallLayer
        );

        if (hit.collider != null)
        {
            playerVisible = false;
            return;
        }

        playerVisible = true;
    }

    // =========================
    // SOUND INPUT
    // =========================

    public void HearSound(Vector2 soundPosition)
    {
        investigatePosition = soundPosition;

        lastKnownPlayerPosition = soundPosition;

        if (
            currentState != MonsterState.Chase &&
            currentState != MonsterState.Capture
        )
        {
            currentState = MonsterState.Investigate;
        }
    }

    // =========================
    // DEBUG
    // =========================

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            viewDistance
        );

        Gizmos.DrawWireSphere(
            transform.position,
            captureDistance
        );

        if (currentState == MonsterState.Search)
        {
            Gizmos.DrawWireSphere(
                lastKnownPlayerPosition,
                searchRadius
            );
        }
    }
}
