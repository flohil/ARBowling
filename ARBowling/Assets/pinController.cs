using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public enum PinControllerState
{
    IDLE, ANALYSIS, PRERESET, RESETTING, HARDRESET
}

public static class PinController
{
    public static float stoppedTolerance = 0.4f;
    public static float standingDotTolerance = 0.1f;
    public static float waitResetTime = 3.0f;
    public static float maxAfterThrownTime = 10.0f;
    public static float localResetHeight = 50f;

    public static PinControllerState state;
}

public class pinController : MonoBehaviour {

    StateManager sm;
    GameObject alleyObject;
    GameObject ball;

    GameObject pin1_1;
    GameObject pin2_1;
    GameObject pin2_2;
    GameObject pin3_1;
    GameObject pin3_2;
    GameObject pin3_3;
    GameObject pin4_1;
    GameObject pin4_2;
    GameObject pin4_3;
    GameObject pin4_4;

    GameObject[] pins;
    Vector3[] initialPinPositions;
    Vector3[] initialLocalPinPositions;
    bool[] hitPins;

    int nbrPins;

    float accTimeReset;
    float accAfterThrownTime;

    bool waitAnimationFinished;

    GameObject animatedPin;

    int hitPinsCount;

    string withinMessage;

    // Use this for initialization
    void Start () {
        sm = TrackerManager.Instance.GetStateManager();
        alleyObject = GameObject.Find("AlleyPlane");
        ball = GameObject.Find("ball");

        nbrPins = 10;
        pins = new GameObject[nbrPins];
        initialPinPositions = new Vector3[nbrPins];
        initialLocalPinPositions = new Vector3[nbrPins];
        hitPins = new bool[nbrPins];

        pin1_1 = GameObject.Find("pin1-1");
        pin2_1 = GameObject.Find("pin2-1");
        pin2_2 = GameObject.Find("pin2-2");
        pin3_1 = GameObject.Find("pin3-1");
        pin3_2 = GameObject.Find("pin3-2");
        pin3_3 = GameObject.Find("pin3-3");
        pin4_1 = GameObject.Find("pin4-1");
        pin4_2 = GameObject.Find("pin4-2");
        pin4_3 = GameObject.Find("pin4-3");
        pin4_4 = GameObject.Find("pin4-4");

        pins[0] = pin1_1;
        pins[1] = pin2_1;
        pins[2] = pin2_2;
        pins[3] = pin3_1;
        pins[4] = pin3_2;
        pins[5] = pin3_3;
        pins[6] = pin4_1;
        pins[7] = pin4_2;
        pins[8] = pin4_3;
        pins[9] = pin4_4;

        for (int i = 0; i < nbrPins; ++i)
        {
            initialPinPositions[i] = pins[i].transform.position;
            initialLocalPinPositions[i] = pins[i].transform.localPosition;
            pins[i].GetComponent<Animation>()["LoweringPin"].wrapMode = WrapMode.Once;
            hitPins[i] = false;
        }

        foreach (GameObject pin in pins)
        {
            pin.GetComponent<Rigidbody>().mass = 0.2f;
        }

        Alley.displayed = false;

        PinController.state = PinControllerState.IDLE;

        animatedPin = null;

        ball.SetActive(false);       

        withinMessage = "Place ball and pins within view!";
    }

    // Update is called once per frame
    void Update()
    {
        IList<TrackableBehaviour> activeTrackables = (IList<TrackableBehaviour>)sm.GetActiveTrackableBehaviours();

        // check if imaget target is tracked
        if (activeTrackables.Count == 2)
        {
            if (!Alley.displayed)
            {
                showAlley();
            }

            if (Ball.state == BallState.RELEASED)
            {
                unlockPins();
                Ball.state = BallState.THROWING;

                GUI.appliedBonus = GUI.lockedDistanceBonus * GUI.rollBonus;
            }

            if (PinController.state == PinControllerState.HARDRESET)
            {
                handleHardReset();
            }

            if (Ball.state == BallState.THROWN)
            {
                if (PinController.state == PinControllerState.IDLE)
                {
                    accAfterThrownTime = 0f;
                    PinController.state = PinControllerState.ANALYSIS;
                }
                
                if (PinController.state == PinControllerState.ANALYSIS)
                {
                    handleAnalysis();
                }

                if (PinController.state == PinControllerState.PRERESET)
                {
                    handlePrereset();
                }

                if (PinController.state == PinControllerState.RESETTING)
                {
                    handleResetting();
                }
            }
        }
        else
        {
            // hide alley
            if (Alley.displayed)
            {
                hideAlley();
                lockPins();
            }

            GUI.errorMessage = withinMessage;
        }
    }

    void showAlley()
    {
        alleyObject.SetActive(true);
        Alley.displayed = true;

        if (Ball.state == BallState.THROWING || Ball.state == BallState.THROWN)
        {
            unlockPins();
        }
    }

    void lockPins()
    {
        foreach (GameObject pin in pins)
        {
            pin.GetComponent<Rigidbody>().useGravity = false;
            pin.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
        }
    }

    void unlockPins()
    {
        foreach (GameObject pin in pins)
        {
            pin.GetComponent<Rigidbody>().useGravity = true;
            pin.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }
    }

    void handleAnalysis()
    {
        bool oneNotStopped = false;

        accAfterThrownTime += Time.deltaTime;

        foreach (GameObject pin in pins)
        {
            if (pin.GetComponent<Rigidbody>().velocity.magnitude > PinController.stoppedTolerance)
            {
                oneNotStopped = true;
                break;
            }
        }

        if (!oneNotStopped || accAfterThrownTime > PinController.maxAfterThrownTime)
        {
            hitPinsCount = 0;

            // count hit pins
            for (int i = 0; i < nbrPins; ++i)
            {
                GameObject pin = pins[i];

                // do not double count fallen pins
                if (hitPins[i] == false)
                {
                    if (Mathf.Abs(Vector3.Dot(pin.transform.up, alleyObject.transform.up) - 1.0f) > PinController.standingDotTolerance)
                    {
                        hitPinsCount++;
                        hitPins[i] = true;
                    }
                }
            }

            accTimeReset = 0f;

            // calculate additional score by hit pins and bonus
            float addScore = hitPinsCount * GUI.appliedBonus;

            // from previous spare or strike
            // decreases after each roll
            if (GUI.rollBonus > 1)
            {
                GUI.rollBonus--;
            }

            // strike gives rollbonus for two rolls
            if (GUI.remainingRollBonus)
            {
                GUI.rollBonus++;
                GUI.remainingRollBonus = false;
            }

            GUI.framePins += hitPinsCount;

            if (hitPinsCount == nbrPins && GUI.roll == 1)
            {
                GUI.message = "STRIKE";
                GUI.endFrame = true;
                GUI.rollBonus++;
                GUI.remainingRollBonus = true;
            }
            else if (hitPinsCount == nbrPins && GUI.roll == 3)
            {
                GUI.message = "STRIKE";
            }
            else if (GUI.framePins == nbrPins)
            {
                GUI.message = "SPARE";
                GUI.rollBonus++;
            }
            else if (hitPinsCount == 1)
            {
                GUI.message = hitPinsCount.ToString() + " Pin";
            }
            else
            {
                GUI.message = hitPinsCount.ToString() + " Pins";
            }

            GUI.message += "\n" + addScore.ToString("0.00");

            GUI.score += addScore;

            PinController.state = PinControllerState.PRERESET;
        }
    }

    void handlePrereset()
    {
        if (accTimeReset > PinController.waitResetTime)
        {
            accTimeReset = 0f;

            for (int i = 0; i < nbrPins; ++i)
            {
                resetPin(i);

                pins[i].transform.localPosition = new Vector3(initialLocalPinPositions[i].x, PinController.localResetHeight, initialLocalPinPositions[i].z);
                pins[i].GetComponent<Animation>().CrossFade("LoweringPin");
            }

            PinController.state = PinControllerState.RESETTING;
        }
        else
        {
            accTimeReset += Time.deltaTime;
        }
    }

    void handleResetting()
    {
        if (animatedPin != null)
        {
            if (!animatedPin.GetComponent<Animation>().IsPlaying("LoweringPin"))
            {
                animatedPin = null;
            }
        }
        else
        {
            PinController.state = PinControllerState.IDLE;
            Ball.state = BallState.RESET;
            GUI.message = null;

            nextRoll();
            lockPins();
        }
    }

    void handleHardReset()
    {
        for (int i = 0; i < nbrPins; ++i)
        {
            resetPin(i, true);
        }

        PinController.state = PinControllerState.IDLE;
    }

    void resetPin(int pinIndex, bool forceReset = false)
    {
        pins[pinIndex].GetComponent<Rigidbody>().useGravity = false;
        pins[pinIndex].GetComponent<Rigidbody>().velocity = Vector3.zero;

        if (forceReset || GUI.framePins == nbrPins || GUI.roll == GUI.MAX_ROLLS && GUI.frame != GUI.MAX_FRAMES)
        {
            pins[pinIndex].SetActive(true);
        }
        else if (hitPins[pinIndex] == true)
        {
            pins[pinIndex].SetActive(false);
        }

        pins[pinIndex].GetComponent<Rigidbody>().velocity = Vector3.zero;
        pins[pinIndex].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        pins[pinIndex].transform.rotation = Quaternion.Euler(Vector3.zero);
        pins[pinIndex].transform.localRotation = Quaternion.Euler(Vector3.zero);

        hitPins[pinIndex] = false;

        pins[pinIndex].transform.position = initialPinPositions[pinIndex];
        pins[pinIndex].transform.localPosition = initialLocalPinPositions[pinIndex];
    }

    void hideAlley()
    {
        alleyObject.SetActive(false);
        Alley.displayed = false;
    }

    void nextRoll()
    {
        // game ends when finished 3rd roll in last frame or if finished second frame and did not hit all pins
        if ((GUI.frame == GUI.MAX_FRAMES && GUI.roll == (GUI.MAX_ROLLS + 1)) || 
            (GUI.frame == GUI.MAX_FRAMES && GUI.roll == GUI.MAX_ROLLS && GUI.framePins < nbrPins))
        {
            Highscore.addHighscore(GUI.name, GUI.score);
            GUI.setState(GUIState.GAME_END);
        }
        else
        {
            // hit all pins in second roll of last frame -> allow a third roll
            if (GUI.frame == GUI.MAX_FRAMES && GUI.roll == GUI.MAX_ROLLS)
            {
                GUI.roll++;
                GUI.framePins = 0;
            }
            else if (GUI.roll < GUI.MAX_ROLLS)
            {
                if (GUI.framePins == nbrPins && GUI.frame < GUI.MAX_FRAMES)
                {
                    GUI.frame++;
                }
                else
                {
                    GUI.roll++;
                }
            }
            else if (GUI.roll == GUI.MAX_ROLLS)
            {
                GUI.frame++;
                GUI.roll = (GUI.roll + 1) % GUI.MAX_ROLLS;
                GUI.framePins = 0;
            }
        }
    }
}