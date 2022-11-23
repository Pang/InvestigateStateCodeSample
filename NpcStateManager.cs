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