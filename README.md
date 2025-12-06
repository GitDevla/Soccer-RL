# Soccer-RL

A reinforcement learning agent designed to play a simplified version of soccer in a 3D environment using Unity ML-Agents. The agent learns to score goals against an opponent while defending its own goal through curriculum learning and self-play.

## Video

[![YouTube Video](https://img.youtube.com/vi/IozEXvBEH6k/0.jpg)](https://www.youtube.com/watch?v=IozEXvBEH6k)

## Agent Overview

**Goal**: Get the ball into the opponent's goal while preventing the ball from entering own goal.

**Agent Reward Function**:

- `+1 - (step/maxStep)` for scoring a goal
- `-1` for conceding a goal
- `-1/maxStep` for each time step to encourage faster play
- `+kickReward` for kicking the ball towards the opponent's goal, where `kickReward` changes based on the curriculum level
- `+possessionReward` for the ball rolling towards the opponent's goal, where `possessionReward` changes based on the curriculum level
- `-possessionReward` for the ball rolling towards own goal, where `possessionReward` changes based on the curriculum level

**Behavior Parameters**:

- Observations: 17
  - Ball's position relative to agent (x, y, z)
  - Opponent's goal position relative to agent (x, z)
  - Own goal position relative to agent (x, z)
  - Agent's velocity (x, z)
  - Ball's velocity relative to agent (x, y, z)
  - Agent's orientation (delta y)
  - Opponent's position relative to agent (x, z)
  - Opponent's velocity relative to agent(x, z)
- Actions:
  - Move Forward/Backward/Stay
  - Move Left/Right/Stay
  - Rotate Left/Right/Stay

**Curriculum Learning**:

- **Easy**: Solo play, ball is stationary at the center of the field.
  - _Max Steps_: `2000`
  - _Kick Reward_: `0.4f`
  - _Possession Reward_: `0.003f`
  - _Completion Criteria_: Average reward of `0.0` over `1000` episodes.
- **Medium**: Solo play, ball is randomly placed within a radius of 1 units from the center.
  - _Max Steps_: `2000`
  - _Kick Reward_: `0.4f`
  - _Possession Reward_: `0.003f`
  - _Completion Criteria_: Average reward of `0.4` over `2000` episodes.
- **Hard**: Solo play, ball is randomly placed and gets a random initial velocity.
  - _Max Steps_: `2000`
  - _Kick Reward_: `0.2f`
  - _Possession Reward_: `0.002f`
  - _Completion Criteria_: Average reward of `0.8` over `3000` episodes.
- **Extreme**: Solo play, ball is randomly placed with random initial velocity, and the agent starts at a random position.
  - _Max Steps_: `2000`
  - _Kick Reward_: `0.2f`
  - _Possession Reward_: `0.002f`
  - _Completion Criteria_: Average reward of `0.8` over `4000` episodes.
- **Self-Play Transition**: A blind (can't see the learning agent), frozen (non-learning) opponent is introduced.
  - _Max Steps_: `10000`
  - _Kick Reward_: `0.1f`
  - _Possession Reward_: `0.001f`
  - _Completion Criteria_: Manually, after the average reward is hovering above `0.0`.
- **Self-Play**: Both agents learn simultaneously.
  - _Max Steps_: `10000`
  - _Kick Reward_: `0.0f`
  - _Possession Reward_: `0.0f`
