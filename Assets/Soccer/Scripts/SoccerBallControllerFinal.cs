using UnityEngine;

public class SoccerBallControllerFinal : MonoBehaviour
{
    public GameObject area;
    [HideInInspector]
    public SoccerEnvControllerFinal envController;
    public string purpleGoalTag; //will be used to check if collided with purple goal
    public string blueGoalTag; //will be used to check if collided with blue goal


    void Start()
    {
        envController = area.GetComponent<SoccerEnvControllerFinal>();
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag(purpleGoalTag)) //ball touched purple goal
        {
            envController.GoalTouched(Team.Blue);
        }
        if (col.gameObject.CompareTag(blueGoalTag)) //ball touched blue goal
        {
            envController.GoalTouched(Team.Purple);
        }

        if (col.gameObject.CompareTag("blueAgent") || col.gameObject.CompareTag("purpleAgent")) //ball touched player
        {
            envController.m_Kick.Play();
        }
    }
}
