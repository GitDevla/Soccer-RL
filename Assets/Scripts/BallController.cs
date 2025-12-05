using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [HideInInspector]
    public SoccerEnv envController;
    private Rigidbody rb;

    private Vector3 startPosition;
    private static readonly float maxBallSpeed = 20f;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
    }

    public void ResetBall()
    {
        transform.localPosition = startPosition;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void AddForce(Vector3 force)
    {
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("RedGoal"))
            envController.GoalScored(Team.Blue);
        else if (other.gameObject.CompareTag("BlueGoal"))
            envController.GoalScored(Team.Red);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector3 normal = collision.contacts[0].normal;
            Vector3 bounce = Vector3.Reflect(rb.velocity, normal);
            rb.velocity = bounce * 1.2f;
        }
    }

    void FixedUpdate()
    {
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxBallSpeed);

        var BlueTeamGoal = envController.blueGoal.transform.position;
        var RedTeamGoal = envController.redGoal.transform.position;

        if (new Vector2(rb.velocity.x, rb.velocity.z).magnitude < 1f)
            return;


        if (Vector3.Dot(rb.velocity, (BlueTeamGoal - transform.position).normalized) > 0)
            envController.GiveRewardToTeam(Team.Red, envController.possessionReward);
        else if (Vector3.Dot(rb.velocity, (RedTeamGoal - transform.position).normalized) > 0)
            envController.GiveRewardToTeam(Team.Blue, envController.possessionReward);
    }
}
