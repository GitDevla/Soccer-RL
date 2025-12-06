using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

public enum Team
{
    Red,
    Blue
}

enum LearningDifficulty
{
    QuickLose = -1,
    Easy = 0,
    Medium = 1,
    Hard = 2,
    Extreme = 3,
    SelfPlayTransition = 4,
    SelfPlay = 5
}

public class SoccerEnv : MonoBehaviour
{
    [Header("Agents and Goals")]
    [SerializeField] private PlayerAgent redPlayerAgent;
    [SerializeField] public GameObject redGoal;
    [SerializeField] private PlayerAgent bluePlayerAgent;
    [SerializeField] public GameObject blueGoal;

    [SerializeField] private BallController ball;

    [Header("Rewards")]
    public float kickReward = 0.4f;
    public float possessionReward = 0.003f;
    public float goalReward = 1.0f;

    [Header("Environment")]
    private int resetTimer;
    public int maxEnvironmentSteps = 2000;

    public const float MAX_DISTANCE = 5f;
    public const float MAX_AGENT_SPEED = 5f;
    public const float MAX_BALL_SPEED = 20f;

    void Start()
    {
        resetTimer = 0;

        // Red Player
        redPlayerAgent.envController = this;
        redPlayerAgent.ball = ball;
        redPlayerAgent.opponent = bluePlayerAgent;
        redPlayerAgent.ownGoal = redGoal.transform;
        redPlayerAgent.opponentGoal = blueGoal.transform;
        redPlayerAgent.myTeam = Team.Red;

        // Blue Player
        bluePlayerAgent.envController = this;
        bluePlayerAgent.ball = ball;
        bluePlayerAgent.opponent = redPlayerAgent;
        bluePlayerAgent.ownGoal = blueGoal.transform;
        bluePlayerAgent.opponentGoal = redGoal.transform;
        bluePlayerAgent.myTeam = Team.Blue;

        ball.envController = this;

        ResetScene();
    }

    public void ResetScene()
    {
        ball.ResetBall();
        float currentLearningDifficulty = Academy.Instance.EnvironmentParameters.GetWithDefault("difficulty", (int)LearningDifficulty.SelfPlay);
        maxEnvironmentSteps = 2000;

        if (currentLearningDifficulty == (int)LearningDifficulty.Medium)
        {
            Vector3 randomPos = new(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            ball.transform.position += randomPos;
        }
        else if (currentLearningDifficulty == (int)LearningDifficulty.Hard)
        {
            Vector3 randomPos = new(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            Vector3 randomVelocity = new(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            ball.transform.position += randomPos;
            ball.AddForce(randomVelocity.normalized * 2f);
            kickReward = 0.2f;
            possessionReward = 0.002f;
        }
        else if (currentLearningDifficulty == (int)LearningDifficulty.Extreme)
        {
            Vector3 randomPos = new(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            Vector3 randomVelocity = new(Random.Range(-1f, 2f), 0, Random.Range(-1f, 2f));
            ball.transform.position += randomPos;
            ball.AddForce(randomVelocity.normalized * 1f);
            float playerRandomPosZ = Random.Range(0, 4f);
            float playerRandomPosX = Random.Range(-1, 1f);
            bluePlayerAgent.transform.position += new Vector3(playerRandomPosX, 0, playerRandomPosZ);
            float playerRandomRotation = Random.Range(0, 360);
            bluePlayerAgent.transform.localRotation = Quaternion.Euler(0, playerRandomRotation, 0);
            kickReward = 0.2f;
            possessionReward = 0.002f;
        }
        else if (currentLearningDifficulty == (int)LearningDifficulty.SelfPlayTransition)
        {
            redPlayerAgent.transform.position += new Vector3(0, -2, 0);
            redPlayerAgent.SetActivity(true, BehaviorType.InferenceOnly);
            bluePlayerAgent.seeOpponent = true;
            maxEnvironmentSteps = 10000;
            kickReward = 0.1f;
            possessionReward = 0.001f;
        }
        else if (currentLearningDifficulty == (int)LearningDifficulty.SelfPlay)
        {
            redPlayerAgent.transform.position += new Vector3(0, -2, 0);
            redPlayerAgent.SetActivity(true, BehaviorType.Default);
            redPlayerAgent.seeOpponent = true;
            bluePlayerAgent.seeOpponent = true;
            maxEnvironmentSteps = 10000;
            kickReward = 0.0f;
            possessionReward = 0.0f;
        }
        resetTimer = 0;
    }

    public void EndEpisode(float redReward, float blueReward)
    {
        redPlayerAgent.AddReward(redReward);
        bluePlayerAgent.AddReward(blueReward);
        redPlayerAgent.EndEpisode();
        bluePlayerAgent.EndEpisode();
        ResetScene();
    }

    public void GiveRewardToTeam(Team team, float reward)
    {
        if (team == Team.Red)
        {
            redPlayerAgent.AddReward(reward);
            bluePlayerAgent.AddReward(-reward);
        }
        else if (team == Team.Blue)
        {
            bluePlayerAgent.AddReward(reward);
            redPlayerAgent.AddReward(-reward);
        }
    }

    void FixedUpdate()
    {
        resetTimer++;
        if (resetTimer >= maxEnvironmentSteps)
        {
            bluePlayerAgent.EndEpisode();
            redPlayerAgent.EndEpisode();
            ResetScene();
        }
    }

    public void GoalScored(Team scoringTeam)
    {
        if (scoringTeam == Team.Red)
            EndEpisode(goalReward - (float)resetTimer / maxEnvironmentSteps, -goalReward);
        else if (scoringTeam == Team.Blue)
            EndEpisode(-goalReward, goalReward - (float)resetTimer / maxEnvironmentSteps);
    }
}
