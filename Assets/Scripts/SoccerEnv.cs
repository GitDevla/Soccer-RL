using Unity.MLAgents;
using UnityEngine;

public enum Team
{
    Red,
    Blue
}

enum LearningDifficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2,
    Extreme = 3,
    SelfPlay = 4
}

public class SoccerEnv : MonoBehaviour
{
    [SerializeField] private PlayerAgent redPlayerAgent;
    [SerializeField] public GameObject redGoal;
    [SerializeField] private PlayerAgent bluePlayerAgent;
    [SerializeField] public GameObject blueGoal;

    [SerializeField] private BallController ball;


    public int maxEnvironmentSteps = 1000;
    private int resetTimer;

    void Start()
    {
        resetTimer = 0;
        // Red Player
        redPlayerAgent.envController = this;
        redPlayerAgent.ball = ball.transform;
        redPlayerAgent.opponent = bluePlayerAgent.transform;
        redPlayerAgent.ownGoal = redGoal.transform;
        redPlayerAgent.opponentGoal = blueGoal.transform;
        redPlayerAgent.myTeam = Team.Red;

        // Blue Player
        bluePlayerAgent.envController = this;
        bluePlayerAgent.ball = ball.transform;
        bluePlayerAgent.opponent = redPlayerAgent.transform;
        bluePlayerAgent.ownGoal = blueGoal.transform;
        bluePlayerAgent.opponentGoal = redGoal.transform;
        bluePlayerAgent.myTeam = Team.Blue;


        ResetScene();
    }

    private void ResetScene()
    {
        ball.ResetBall();
        // var currentLearningDifficulty = Academy.Instance.EnvironmentParameters.GetWithDefault("difficulty", (int)LearningDifficulty.SelfPlay);
        var currentLearningDifficulty = (int)LearningDifficulty.SelfPlay;
        if (currentLearningDifficulty == (int)LearningDifficulty.Medium)
        {
            var randomPos = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            ball.transform.localPosition += randomPos;
        }
        else if (currentLearningDifficulty == (int)LearningDifficulty.Hard)
        {
            var randomPos = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            var randomVelocity = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            ball.transform.localPosition += randomPos;
            ball.AddForce(randomVelocity.normalized * 2f);
        }
        else if (currentLearningDifficulty == (int)LearningDifficulty.Extreme)
        {
            var randomPos = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            var randomVelocity = new Vector3(Random.Range(-1f, 2f), 0, Random.Range(-1f, 2f));
            ball.transform.localPosition += randomPos;
            ball.AddForce(randomVelocity.normalized * 1f);
            var playerRandomPosZ = Random.Range(0, 4f);
            var playerRandomPosX = Random.Range(-1, 1f);
            redPlayerAgent.transform.localPosition += new Vector3(playerRandomPosX, 0, playerRandomPosZ);
            var playerRandomRotation = Random.Range(0, 360);
            redPlayerAgent.transform.localRotation = Quaternion.Euler(0, playerRandomRotation, 0);
        }
        else if (currentLearningDifficulty == (int)LearningDifficulty.SelfPlay)
        {
            redPlayerAgent.transform.localPosition += new Vector3(0, -2, 0);
            redPlayerAgent.setActivity(true);
            bluePlayerAgent.seeOpponent = true;
            redPlayerAgent.seeOpponent = true;
        }
        resetTimer = 0;
    }

    private void EndEpisode(float redReward, float blueReward)
    {
        redPlayerAgent.AddReward(redReward);
        bluePlayerAgent.AddReward(blueReward);
        redPlayerAgent.EndEpisode();
        bluePlayerAgent.EndEpisode();
        ResetScene();
    }

    public void GiveRewardToTeam(Team team, float reward)
    {
        // if (team == Team.Red)
        // {
        //     redPlayerAgent.AddReward(reward);
        //     bluePlayerAgent.AddReward(-reward);
        // }
        // else if (team == Team.Blue)
        // {
        //     bluePlayerAgent.AddReward(reward);
        //     redPlayerAgent.AddReward(-reward);
        // }
    }

    void FixedUpdate()
    {
        resetTimer++;
        if (resetTimer >= maxEnvironmentSteps)
            EndEpisode(0, 0);
    }

    public void GoalScored(Team scoringTeam)
    {
        if (scoringTeam == Team.Red)
            EndEpisode(1, -1);
        else if (scoringTeam == Team.Blue)
            EndEpisode(-1, 1);
    }
}
