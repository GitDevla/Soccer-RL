using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [HideInInspector]
    public SoccerEnv envController;
    private Rigidbody rb;

    private Vector3 startPosition;


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

    void FixedUpdate()
    {
        var BlueTeamGoal = envController.blueGoal.transform.position;
        var RedTeamGoal = envController.redGoal.transform.position;

        if (rb.velocity.magnitude < 1f)
            return;

        if (Vector3.Dot(rb.velocity, (BlueTeamGoal - transform.position).normalized) > 0)
            envController.GiveRewardToTeam(Team.Red, 0.001f);
        else if (Vector3.Dot(rb.velocity, (RedTeamGoal - transform.position).normalized) > 0)
            envController.GiveRewardToTeam(Team.Blue, 0.001f);
    }
}
