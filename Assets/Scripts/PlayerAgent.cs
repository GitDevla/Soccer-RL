using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.Collections.ObjectModel;

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
    [Header("Player Agent")]
    private Rigidbody rb;
    private Vector3 startPosition;
    private Vector3 initialRotation;

    public Team myTeam;

    [HideInInspector] public SoccerEnv envController;

    [HideInInspector] private readonly float moveSpeed = 0.1f;
    [HideInInspector] private readonly float turnSpeed = 150f;
    [HideInInspector] private readonly float kickForce = 1f;

    [HideInInspector] public BallController ball;
    [HideInInspector] public PlayerAgent opponent;
    [HideInInspector] public Transform ownGoal;
    [HideInInspector] public Transform opponentGoal;

    [HideInInspector]
    private BehaviorParameters behaviorParameters;
    public bool seeOpponent = false;

    private int goodBallTouches = 0;

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
    }

    private void OnDrawGizmos()
    {
        ReadOnlyCollection<float> observations = GetObservations();

        if (observations.Count == 0)
            return;

        Vector3 ballDir = new Vector3(observations[0], observations[1], observations[2]) * SoccerEnv.MAX_DISTANCE;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(ballDir));

        Vector3 opponentGoalDir = new Vector3(observations[3], 0, observations[4]) * SoccerEnv.MAX_DISTANCE;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(opponentGoalDir));

        Vector3 ownGoalDir = new Vector3(observations[5], 0, observations[6]) * SoccerEnv.MAX_DISTANCE;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(ownGoalDir));

        Vector3 agentVelocity = new Vector3(observations[7], 0, observations[8]) * SoccerEnv.MAX_AGENT_SPEED;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(agentVelocity));

        Vector3 ballVelocity = new Vector3(observations[9], observations[10], observations[11]) * (SoccerEnv.MAX_BALL_SPEED + SoccerEnv.MAX_AGENT_SPEED);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(ballVelocity));

        float deltaAngle = observations[12] * 180f;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, deltaAngle, 0) * transform.forward);

        if (seeOpponent && opponent != null)
        {
            Vector3 opponentDir = new Vector3(observations[13], 0, observations[14]) * SoccerEnv.MAX_DISTANCE;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(opponentDir));

            Vector3 opponentVelocity = 2 * SoccerEnv.MAX_AGENT_SPEED * new Vector3(observations[15], 0, observations[16]);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(opponentVelocity));
        }
    }

    private static Vector2 Vector3ToVector2(Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 ballDir = ball.transform.position - transform.position;
        sensor.AddObservation(transform.InverseTransformDirection(ballDir) / SoccerEnv.MAX_DISTANCE);

        Vector3 opponentGoalDir = opponentGoal.position - transform.position;
        Vector3 ownGoalDir = ownGoal.position - transform.position;
        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(opponentGoalDir)) / SoccerEnv.MAX_DISTANCE);
        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(ownGoalDir)) / SoccerEnv.MAX_DISTANCE);

        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(rb.velocity)) / SoccerEnv.MAX_AGENT_SPEED);
        sensor.AddObservation(transform.InverseTransformDirection(ball.rb.velocity) / (SoccerEnv.MAX_BALL_SPEED + SoccerEnv.MAX_AGENT_SPEED));

        float deltaAngle = Mathf.DeltaAngle(initialRotation.y, transform.rotation.eulerAngles.y);
        sensor.AddObservation(deltaAngle / 180f);

        if (seeOpponent && opponent != null)
        {
            Vector3 opponentDir = opponent.transform.position - transform.position;
            sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(opponentDir)) / SoccerEnv.MAX_DISTANCE);
            sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(opponent.rb.velocity)) / SoccerEnv.MAX_AGENT_SPEED * 2);
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

        Vector3 dirToGo = Vector3.zero;

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

        rb.velocity = Vector3.ClampMagnitude(rb.velocity, SoccerEnv.MAX_AGENT_SPEED);
        AddReward(-1f / envController.maxEnvironmentSteps);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;
        float forward = Input.GetAxisRaw("Vertical");
        float side = Input.GetAxisRaw("Horizontal");

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
        Vector3 kickDirection = (collision.transform.position - transform.position).normalized;

        float dotProduct = Vector3.Dot(transform.forward, kickDirection);

        if (dotProduct > 0.5f) // 0.5 ~= 60 degrees
        {
            float currentSpeed = rb.velocity.magnitude;
            float adjustedKickForce = kickForce + currentSpeed;
            ball.AddForce(kickDirection * adjustedKickForce);
            if (Vector3.Dot(ball.rb.velocity.normalized, (opponentGoal.position - ball.rb.position).normalized) > 0)
            {
                envController.GiveRewardToTeam(myTeam, envController.kickReward * Mathf.Pow(0.8f, goodBallTouches));
                goodBallTouches += 1;
            }
        }
    }


    public void SetActivity(bool isActive, BehaviorType behaviorType = BehaviorType.Default)
    {
        rb.useGravity = isActive;
        behaviorParameters.BehaviorType = behaviorType;
    }
}