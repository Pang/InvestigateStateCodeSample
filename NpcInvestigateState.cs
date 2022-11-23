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
