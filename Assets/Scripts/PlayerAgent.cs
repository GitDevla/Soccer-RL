using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

enum ForwardMovement
{
    None = 0,
    Forward = 1,
    Backward = -1
}

enum SideMovement
{
    None = 0,
    Right = 1,
    Left = -1
}

enum RotationMovement
{
    None = 0,
    RotateRight = 1,
    RotateLeft = -1
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
    [HideInInspector] private readonly float turnSpeed = 3f;
    [HideInInspector] private readonly float kickForce = 1f;
    [HideInInspector] private readonly float maxSpeed = 5f;

    [HideInInspector] public Transform ball;
    [HideInInspector] public Transform opponent;
    [HideInInspector] public Transform ownGoal;
    [HideInInspector] public Transform opponentGoal;

    [HideInInspector]
    private BehaviorParameters behaviorParameters;
    public bool seeOpponent = false;

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
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        var observation = GetObservations();
        var y = transform.position.y;
        // Positions
        var relativeBallPos = new Vector3(observation[0], observation[1], observation[2]);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(relativeBallPos));

        // Goal Directions
        var relativeOpponentGoalDir = new Vector3(observation[3], y, observation[4]);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(relativeOpponentGoalDir));
        var relativeOwnGoalDir = new Vector3(observation[5], y, observation[6]);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(relativeOwnGoalDir));

        // Velocities
        var relativeVelocity = new Vector3(observation[7], y, observation[8]);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(relativeVelocity));
        var relativeBallVelocity = new Vector3(observation[9], observation[10], observation[11]);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(relativeBallVelocity));

        // Orientation 2 skipped

        var relativeOpponentPos = new Vector3(observation[14], y, observation[15]);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(relativeOpponentPos));
        var relativeOpponentVelocity = new Vector3(observation[16], y, observation[17]);
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(relativeOpponentVelocity));
    }

    private static Vector2 Vector3ToVector2(Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Position (3 values)
        sensor.AddObservation(transform.InverseTransformPoint(ball.position));

        // Goal directions (4 values)
        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(opponentGoal.position - transform.position)));
        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(ownGoal.position - transform.position)));

        // Velocities (7 values)
        sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(rb.velocity)));
        sensor.AddObservation(transform.InverseTransformDirection(ball.GetComponent<Rigidbody>().velocity));

        // Orientation (2 values)
        sensor.AddObservation(transform.rotation[1]);
        sensor.AddObservation(transform.rotation[3]);


        // Opponent info (4 values)
        if (seeOpponent)
        {
            sensor.AddObservation(Vector3ToVector2(transform.InverseTransformPoint(opponent.position)));
            sensor.AddObservation(Vector3ToVector2(transform.InverseTransformDirection(opponent.GetComponent<Rigidbody>().velocity)));
        }
        else
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(Vector2.zero);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var rotateDir = Vector3.up * actions.DiscreteActions[2];
        transform.Rotate(rotateDir, turnSpeed);

        var dirToGo = Vector3.zero;
        dirToGo += transform.forward * actions.DiscreteActions[0];
        dirToGo += transform.right * actions.DiscreteActions[1];
        dirToGo = dirToGo.normalized;
        rb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);

        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
        AddReward(1 / envController.maxEnvironmentSteps * -0.5f);
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
            envController.GiveRewardToTeam(myTeam, 0.1f);
        }
        else
        {
            envController.GiveRewardToTeam(myTeam, 0.03f);
        }
    }

    public void setActivity(bool isActive, BehaviorType behaviorType = BehaviorType.Default)
    {
        rb.useGravity = isActive;
        behaviorParameters.BehaviorType = behaviorType;
    }
}