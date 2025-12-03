using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

enum ForwardMovement
{
    None,
    Forward,
    Backward
}

enum SideMovement
{
    None,
    Right,
    Left
}

enum RotationMovement
{
    None,
    RotateRight,
    RotateLeft
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorParameters))]
public class PlayerAgent : Agent
{
    private Rigidbody rb;
    private Vector3 startPosition;
    private Vector3 initialRotation;

    public Team myTeam;

    [HideInInspector] public SoccerEnv envController;

    [HideInInspector] private readonly float moveSpeed = 0.1f;
    [HideInInspector] private readonly float turnSpeed = 150f;
    [HideInInspector] private readonly float kickForce = 1f;
    [HideInInspector] private readonly float maxSpeed = 5f;

    [HideInInspector] public Transform ball;
    [HideInInspector] public Transform opponent;
    [HideInInspector] public Transform ownGoal;
    [HideInInspector] public Transform opponentGoal;
    public Rigidbody ballRb;
    public Rigidbody opponentRb;

    [HideInInspector]
    private BehaviorParameters behaviorParameters;
    public bool seeOpponent = false;

    private const float MAX_DISTANCE = 6f;
    private const float MAX_AGENT_SPEED = 5f;
    private const float MAX_BALL_SPEED = 20f;

    private int goodBallTouches = 0;
    private int badBallTouches = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        behaviorParameters = GetComponent<BehaviorParameters>();
        startPosition = transform.position;
        initialRotation = transform.eulerAngles;
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.eulerAngles = initialRotation;
        goodBallTouches = 0;
        badBallTouches = 0;
    }

    private static Vector2 Vector3ToVector2(Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 ballDir = ball.position - transform.position;
        sensor.AddObservation(transform.InverseTransformDirection(ballDir) / MAX_DISTANCE);

        Vector3 opponentGoalDir = opponentGoal.position - transform.position;
        Vector3 ownGoalDir = ownGoal.position - transform.position;
        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(opponentGoalDir)) / MAX_DISTANCE);
        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(ownGoalDir)) / MAX_DISTANCE);

        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(rb.velocity)) / MAX_AGENT_SPEED);
        sensor.AddObservation(transform.InverseTransformDirection(ballRb.velocity) / MAX_BALL_SPEED);

        sensor.AddObservation((transform.rotation.eulerAngles.y - (myTeam == Team.Red ? 180f : 0f)) / 360.0f);

        if (seeOpponent && opponent != null)
        {
            Vector3 opponentDir = opponent.position - transform.position;
            sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(opponentDir)) / MAX_DISTANCE);
            sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(opponentRb.velocity)) / MAX_AGENT_SPEED * 2);
        }
        else
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(Vector2.zero);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        switch (actions.DiscreteActions[2])
        {
            case (int)RotationMovement.RotateLeft:
                transform.Rotate(-Vector3.up, turnSpeed * Time.fixedDeltaTime);
                break;
            case (int)RotationMovement.RotateRight:
                transform.Rotate(Vector3.up, turnSpeed * Time.fixedDeltaTime);
                break;
        }

        var dirToGo = Vector3.zero;

        switch (actions.DiscreteActions[0])
        {
            case (int)ForwardMovement.Forward:
                dirToGo += transform.forward;
                break;
            case (int)ForwardMovement.Backward:
                dirToGo += -transform.forward;
                break;
        }
        switch (actions.DiscreteActions[1])
        {
            case (int)SideMovement.Right:
                dirToGo += transform.right;
                break;
            case (int)SideMovement.Left:
                dirToGo += -transform.right;
                break;
        }

        dirToGo = dirToGo.normalized;
        rb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);

        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
        AddReward(-1f / envController.maxEnvironmentSteps);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        var forward = Input.GetAxisRaw("Vertical");
        var side = Input.GetAxisRaw("Horizontal");

        //forward
        if (forward > 0)
            discreteActionsOut[0] = (int)ForwardMovement.Forward;
        else if (forward < 0)
            discreteActionsOut[0] = (int)ForwardMovement.Backward;

        //side
        if (side > 0)
            discreteActionsOut[1] = (int)SideMovement.Right;
        else if (side < 0)
            discreteActionsOut[1] = (int)SideMovement.Left;

        //rotate
        if (Input.GetKey(KeyCode.Q))
            discreteActionsOut[2] = (int)RotationMovement.RotateLeft;
        else if (Input.GetKey(KeyCode.E))
            discreteActionsOut[2] = (int)RotationMovement.RotateRight;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
            OnBallTouch(collision);
    }

    private void OnBallTouch(Collision collision)
    {
        Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
        Vector3 kickDirection = (collision.transform.position - transform.position).normalized;

        float dotProduct = Vector3.Dot(transform.forward, kickDirection);

        if (dotProduct > 0.5f) // 0.5 ~= 60 degrees
        {
            float currentSpeed = rb.velocity.magnitude;
            float adjustedKickForce = kickForce + currentSpeed;
            ballRb.AddForce(kickDirection * adjustedKickForce, ForceMode.VelocityChange);
            envController.GiveRewardToTeam(myTeam, 0.4f * Mathf.Pow(0.9f, goodBallTouches));
            goodBallTouches += 1;
        }
        else
        {
            envController.GiveRewardToTeam(myTeam, 0.1f * Mathf.Pow(0.9f, badBallTouches));
            badBallTouches += 1;
        }
    }


    public void SetActivity(bool isActive, BehaviorType behaviorType = BehaviorType.Default)
    {
        rb.useGravity = isActive;
        behaviorParameters.BehaviorType = behaviorType;
    }
}