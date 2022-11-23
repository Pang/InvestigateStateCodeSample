using System.Collections;
using UnityEngine;

public class NpcStateManager : MonoBehaviour
{
    public NpcBaseState currentState;
    public NpcIdleState idleState = new NpcIdleState();
    public NpcInvestigateState investigateState = new NpcInvestigateState(); 
    public NpcChasingState chasingState = new NpcChasingState();
    public NpcAttackingState attackingState = new NpcAttackingState(); 
    public NpcDeadState deadState = new NpcDeadState();
    public Animator animator;

    public NpcBase npc;
    public GameObject player;

    public Action Alert;
    public bool isAlerted;
    public Action Alarm;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        npc = GetComponent<NpcBase>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Start()
    {
        Alert += OnAlert;

        currentState = idleState;
        currentState.EnterState(this);
    }

    public void SwitchState(NpcBaseState state)
    {
        StopAllCoroutines();
        currentState = state;
        state.EnterState(this);
    }

    public IEnumerator AlarmRoutine()
    {
        float delay = 0.2f;
        WaitForSeconds wait = new WaitForSeconds(delay);
        while (!npc.isDead)
        {
            yield return wait;
            if (HelperMethods.CheckInRangeAndSight(player.transform.position, npc.transform, npc.radius, npc.cutoffSight))
                Alarm.Invoke();
        }
    }

    public IEnumerator AlertRoutine()
    {
        float delay = 0.2f;
        WaitForSeconds wait = new WaitForSeconds(delay);
        while (!npc.isDead)
        {
            yield return wait;
            if (HelperMethods.CheckInRange(player.transform.position, npc.transform.position, npc.radius))
            {
                PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
                if (!playerMovement.crouching) Alert.Invoke();
            }
        }
    }

    public void OnAlert()
    {
        if (!isAlerted)
        {
            isAlerted = true;
            npc.questionMarkAlert.SetActive(true);
            StartCoroutine(ShakeAlertSymbol());
            Invoke("Alerted", 1f);
        }
    }

    public IEnumerator ShakeAlertSymbol()
    {
        float delay = 0.02f;
        float lacunarity = 1.5f;
        int scale = 100;

        WaitForSeconds wait = new WaitForSeconds(delay);
        while (isAlerted)
        {
            for (float x = 0; x < 1; x += 0.1f)
            {
                for (float y = 0; y < 1; y += 0.1f)
                {
                    yield return wait;
                    HelperMethods.ShakeRotational(npc.questionMarkAlert.transform, new Vector2(x, y), lacunarity, scale);
                }
            }
        }
    }

    private void Alerted()
    {
        npc.questionMarkAlert.SetActive(false);
        npc.questionMarkAlert.transform.position = npc.headPos + new Vector3(0, 1, 0);
        SwitchState(investigateState);
        isAlerted = false;
    }
}

public class NpcInvestigateState : NpcBaseState
{
    private Vector3 startPosition;
    private Quaternion startRotation;
    public Vector3 areaToInvestigate;
    private bool _reachedInvestigatePoint;

    public override void EnterState(NpcStateManager state)
    {
        SetNpcStats(state);
        areaToInvestigate = state.player.transform.position;
        state.npc.agent.SetDestination(areaToInvestigate);

        state.StartCoroutine(Routine(state));
        state.StartCoroutine(state.AlarmRoutine());
    }

    protected override void SetNpcStats(NpcStateManager state)
    {
        state.animator.SetBool("Walk", true);
        state.animator.SetBool("Run", false);
        state.animator.SetBool("Idle", false);

        _reachedInvestigatePoint = false;
        startPosition = state.npc.transform.position;
        startRotation = state.npc.transform.rotation;

        state.npc.agent.speed = state.npc.walkSpeed;
        state.npc.agent.angularSpeed = state.npc.turningWalkSpeed;
    }

    protected override IEnumerator Routine(NpcStateManager state)
    {
        float delay = 0.2f;
        WaitForSeconds wait = new WaitForSeconds(delay);
        while (state.currentState == this)
        {
            yield return wait;
            CheckReachedDestination(state);

            if (state.npc.hostileTarget != null) state.SwitchState(state.chasingState);
        }
    }

    private void CheckReachedDestination(NpcStateManager state)
    {
        if (Vector3.Distance(state.npc.transform.position, state.npc.agent.pathEndPosition) < state.npc.agent.stoppingDistance)
        {
            if (!_reachedInvestigatePoint)
            {
                _reachedInvestigatePoint = true;
                state.npc.agent.SetDestination(startPosition);
            }
            else 
            {
                state.npc.transform.rotation = startRotation;
                state.SwitchState(state.idleState);
            }
        }
    }
}

public static class HelperMethods
{
    public static bool CheckInRange(Vector3 targetPos, Vector3 fromPos, float radius)
    {
        // Displace position & check distance to 0,0
        float magnitude = (targetPos - fromPos).magnitude;
        float distance = Mathf.Sqrt(magnitude);
        return distance < radius;
    }

    public static bool CheckInSight(Vector3 target, Transform tSelf, float radius, float peripheralVisionDegrees)
    {
        Vector3 dotTarget = (target - tSelf.position).normalized;
        float dotProduct = Vector3.Dot(dotTarget, tSelf.forward);

        if (dotProduct > 0)
        {
            // Check target is within peripheral vision
            float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
            
            if (angle < peripheralVisionDegrees)
            {
                // check vision not obscured from eye position to targets head
                RaycastHit hit;
                Vector3 sightLevel = tSelf.position + new Vector3(0, 3, 0);
                Vector3 targetLevel = target + new Vector3(0, 1.5f, 0);

                Physics.Linecast(sightLevel, targetLevel, out hit);
                if (hit.collider?.tag == "Player") return true;
            }
        }

        return false;
    }

    public static bool CheckInRangeAndSight(Vector3 targetPos, Transform tSelf, float radius, float peripheralVisionDegrees)
    {
        if (!CheckInRange(targetPos, tSelf.position, radius)) return false;
        return CheckInSight(targetPos, tSelf, radius, peripheralVisionDegrees);
    }

    public static void ShakeRotational(Transform transform, Vector2 coords, float lacunarity, float scale)
    {
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x,
            transform.eulerAngles.y,
            ((Mathf.PerlinNoise(coords.x, coords.y) - 0.5f) * lacunarity) * scale
        );
    }
}
