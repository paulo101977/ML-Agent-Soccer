using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.Sentis;

public class AgentSoccerFinal  : Agent
{
    // Note that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    // The coefficient for the reward for colliding with a ball. Set using curriculum.
    float m_BallTouch;
    public Position position;

    const float k_Power = 20f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    private SoccerEnvControllerFinal envController;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;
    
    EnvironmentParameters m_ResetParams;

    private BehaviorParameters behaviorParameters;
    
    public override void Initialize()
    {
        envController = GetComponentInParent<SoccerEnvControllerFinal>();


        behaviorParameters = GetComponent<BehaviorParameters>();

        

        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            // initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            initialPos = transform.position;
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            // initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            initialPos = transform.position;
            rotSign = -1f;
        }
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }
        m_SoccerSettings = FindFirstObjectByType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

    private void HeadBall() {
        GameObject ball = envController.ball;
        if (ball == null) return;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
        float headRadius = 2.0f;
        float headForce = 15f;

        float minHeightForHeader = 1.5f;
        float maxHeightForHeader = 3.0f;

        float ballHeight = ball.transform.position.y;
        float agentHeight = transform.position.y;

        if (distanceToBall <= headRadius && 
            ballHeight > agentHeight + minHeightForHeader && 
            ballHeight < agentHeight + maxHeightForHeader)
        {
            Vector3 headDirection = (ball.transform.position - transform.position).normalized + Vector3.up;

            ballRb.AddForce(headDirection * headForce, ForceMode.Impulse);

            AddReward(0.1f);

            envController.m_Kick.Play();
        }
        else
        {
            
        }
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];
        var kickAction = act[3];
        var kickAngleAction = act[4];
        var headAction = act[5];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                // m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        GameObject ball = envController.ball;
        Vector3 kickDirection = (ball.transform.position - transform.position).normalized;

        if (kickAction == 1)
        {
            // m_KickPower = 1f;
            KickBall(kickDirection);
        }

        if (kickAngleAction == 1)
        {
            // m_KickPower = 1f;
            Vector3 angledKickDirection = (kickDirection + Vector3.up * 0.5f).normalized;
            KickBall(angledKickDirection);
        }

        if( headAction == 1) {
            HeadBall();
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange);

    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {

        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[3] = 1;
        }
        else
        {
            discreteActionsOut[3] = 0;
        }

        if (Input.GetKey(KeyCode.LeftShift)) {
            discreteActionsOut[4] = 1;
        } else {
            discreteActionsOut[4] = 0;
        }

        if (Input.GetKey(KeyCode.R))
        {
            discreteActionsOut[5] = 1;
        }
        else
        {
            discreteActionsOut[5] = 0;
        }
    }
    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        // var force = k_Power * m_KickPower;

        // Goalie don't apply force to ball
        if (position == Position.Goalie)
        {
            // force = k_Power;
            return;
        }

    }

    private void KickBall(Vector3 kickDirection)
    {
        GameObject ball = envController.ball;
        if (ball == null) return;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);

        float kickRadius = 3.0f;

        Vector3 agentForward = transform.forward;

        Vector3 directionToBall = (ball.transform.position - transform.position).normalized;

        float angle = Vector3.Angle(agentForward, directionToBall);
        
        if (distanceToBall <= kickRadius && angle < 45f)
        {
            // Vector3 kickDirection = (ball.transform.position - transform.position).normalized;

            float kickForce = k_Power;

            ballRb.AddForce(kickDirection * kickForce, ForceMode.Impulse);

            AddReward(0.1f);

            envController.m_Kick.Play();
        }
        else
        {
            
        }
    }


}
