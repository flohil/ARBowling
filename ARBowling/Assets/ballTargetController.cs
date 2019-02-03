using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public static class BallTarget
{
    public static Vector3 size = new Vector3(24f, 0, 16.8f);
}

public class ballTargetController : MonoBehaviour {

    GameObject throwLine;
    GameObject ballObject;
    GameObject alley;
    StateManager sm;

    float accumulatedSamplingDeterminationTime;
    int samplingDeterminationCounter;

    // Use this for initialization
    void Start()
    {
        sm = TrackerManager.Instance.GetStateManager();

        ballObject = GameObject.Find("ball");
        alley = GameObject.Find("AlleyPlane");
        throwLine = GameObject.Find("ThrowLine");

        accumulatedSamplingDeterminationTime = 0;
        samplingDeterminationCounter = 0;
    }

    // Update is called once per frame
    void Update () {
        IList<TrackableBehaviour> activeTrackables = (IList<TrackableBehaviour>)sm.GetActiveTrackableBehaviours();

        // check if imaget target is tracked
        if (activeTrackables.Count == 2)
        {
            // show ball
            if (!Ball.displayed && !Alley.tooClose)
            {
                showBall();
            }

            if (Alley.tooClose && Ball.displayed)
            {
                hideBall();
            }

            if (!Ball.determinedSampling)
            {
                determineSamplingRate();
            }

            if (Ball.state == BallState.RESET)
            {
                resetBall();

                GUI.appliedBonus = 0f;

                Ball.state = BallState.IDLE;
            }
        }
        else
        {
            // hide ball
            if (Ball.displayed)
            {
                hideBall();
            }
        }
    }

    void determineSamplingRate()
    {
        accumulatedSamplingDeterminationTime += Time.deltaTime;
        samplingDeterminationCounter++;

        if (accumulatedSamplingDeterminationTime >= Ball.targetSamplingTime)
        {
            // adapt drag movement samples to actual framerate
            Ball.dragMovementSamples = samplingDeterminationCounter;
            Ball.determinedSampling = true;
        }
    }

    void showBall()
    {
        ballObject.SetActive(true);
        Ball.xOffset = 0f;
        Ball.zOffset = 0f;
        Ball.displayed = true;
    }

    void hideBall()
    {
        Ball.displayed = false;
        ballObject.SetActive(false);
        throwLine.SetActive(false);
    }

    void resetBall()
    {
        ballObject.SetActive(true);
        Ball.zOffset = 0f;
        Ball.xOffset = 0f;

        ballObject.transform.localPosition = new Vector3(0, 0, 0);
        ballObject.transform.position = new Vector3(0, Ball.yOffset, transform.position.z);
        ballObject.GetComponent<Rigidbody>().useGravity = false;
        ballObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ballObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        ballObject.transform.rotation = Quaternion.Euler(Vector3.zero);
        ballObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
    }
}
