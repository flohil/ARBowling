using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public struct Movement
{
    public float time;
    public Vector3 movement;

    public Movement(Vector3 _movement, float _time)
    {
        time = _time;
        movement = _movement;
    }
}

public enum BallState
{
    IDLE, DRAGGING, RELEASED, THROWING, THROWN, RESET
}

public static class Ball
{
    public static float DRAG_SPEED = 20f;
    public static float movementTolerance = 0.1f; // a milimeter
    public static float stoppedTolerance = 0.5f; // a milimeter
    public static bool needsReset;
    public static bool displayed;
    public static BallState state;
    public static float xOffset = 0f;
    public static float zOffset = 0f;
    public static float yOffset = 0.000f;

    public static float MAX_THROW_SPEED = 60f;
    public static float MIN_THROW_SPEED = 6f;

    public static bool determinedSampling = false;
    public static float targetSamplingTime = 0.1f;
    public static int dragMovementSamples = 6; // default value assuming 60 fps - throw speed will be determined over last x frames
    public static float requiredStoppedTime = 0.1f;

    public static float noMovementThreshold = 0.5f;
}

public class ballController : MonoBehaviour
{
    GameObject alleyObject;
    GameObject ballTarget;
    GameObject pinsTarget;
    Rigidbody rigidbody;

    Vector3 lastPosition;
    Movement[] dragMovements;
    float noMovementTime;

    float accStopTime;
    bool stopped;

    float yOffset;

    // Use this for initialization
    void Start()
    {
        alleyObject = GameObject.Find("AlleyPlane");
        ballTarget = GameObject.Find("BallTarget");
        pinsTarget = GameObject.Find("PinsTarget");
        rigidbody = this.GetComponent<Rigidbody>();

        Ball.needsReset = false;
        Ball.displayed = false;

        Ball.state = BallState.IDLE;

        dragMovements = new Movement[Ball.dragMovementSamples];
        accStopTime = 0f;

        stopped = true;
        noMovementTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Ball.state == BallState.DRAGGING)
        {
            handleDragging();
        }
        else if (Alley.displayed && Ball.displayed && Ball.state == BallState.IDLE)
        {
            GUI.appliedBonus = 0;
            followAlleyPlane();
        }
        else if (Ball.state == BallState.THROWING)
        {
            handleThrowing();
        }
    }

    void OnMouseDown()
    {
        if (Ball.state == BallState.IDLE)
        {
            Ball.state = BallState.DRAGGING;
            lastPosition = transform.position;
            dragMovements = new Movement[Ball.dragMovementSamples];
        }
    }

    void OnMouseUp()
    {
        if (Ball.state == BallState.DRAGGING)
        {
            float totalTime = 0;
            Vector3 totalMovement = new Vector3(0f, 0f, 0f);

            foreach (Movement movement in dragMovements)
            {
                totalTime += movement.time;
                totalMovement += movement.movement;
            }

            float sampledTime = totalTime / Ball.dragMovementSamples;
            Vector3 sampledMovement = totalMovement / Ball.dragMovementSamples;

            float forwardSpeed = sampledMovement.z / sampledTime;
            float rightSpeed = sampledMovement.x / sampledTime;

            if (Mathf.Abs(sampledMovement.x) >= Mathf.Abs(sampledMovement.z) || forwardSpeed < Ball.MIN_THROW_SPEED || noMovementTime > Ball.noMovementThreshold)
            {
                // ball was moved mainly along x axis - not a throw but just positioning
                Ball.state = BallState.IDLE;
                Ball.xOffset = transform.position.x;
                Ball.zOffset = - (ballTarget.transform.position.z - transform.position.z);
            }
            else
            {
                forwardSpeed = Mathf.Min(Ball.MAX_THROW_SPEED, forwardSpeed);
                rightSpeed = Mathf.Min(Ball.MAX_THROW_SPEED, rightSpeed);

                rigidbody.useGravity = true;
                rigidbody.velocity += new Vector3(0, 0, 1) * forwardSpeed;
                rigidbody.velocity += new Vector3(1, 0, 0) * rightSpeed;

                Ball.state = BallState.RELEASED;

                stopped = false;
            }
        }
    }

    void followAlleyPlane()
    {
        // sometimes ball gets stuck in alley - reset local y
        transform.localPosition = new Vector3(0, 0, 0);

        Vector3 targetPosition = new Vector3(ballTarget.transform.position.x + Ball.xOffset, Ball.yOffset, ballTarget.transform.position.z + Ball.zOffset);

        float distance = Vector3.Distance(transform.position, targetPosition);

        // image targets moved since last update: set new lerp parameters
        if (distance > Ball.movementTolerance)
        {
            // transform.position = new Vector3(transform.position.x, 0, transform.position.z);
            transform.position = targetPosition;
        }
    }

    // ball follows mouse/finger if it is being dragged
    void handleDragging()
    {
        float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 rayPoint = ray.GetPoint(distance);

        float x = Mathf.Min(rayPoint.x, Alley.rightAlleyBorder);
        x = Mathf.Max(x, Alley.leftAlleyBorder);

        float z = Mathf.Min(rayPoint.z, Alley.nearestPositioningZ);
        z = Mathf.Max(z, Alley.farthestPositioningZ);

        // ball may only be moved on plane
        Vector3 target = new Vector3(x, Ball.yOffset, z);

        transform.position = Vector3.Lerp(this.transform.position, target, Ball.DRAG_SPEED * Time.deltaTime);

        Vector3 movement = target - lastPosition;

        if ((Mathf.Abs(movement.x) + Mathf.Abs(movement.z)) >= 0.1) // no movement
        {
            for (int i = 1; i < Ball.dragMovementSamples; ++i)
            {
                dragMovements[i] = dragMovements[i - 1];
            }

            dragMovements[0] = new Movement(movement, Time.deltaTime);
            noMovementTime = 0;
        }
        else
        {
            noMovementTime += Time.deltaTime;
        }

        lastPosition = target;
    }

    void handleThrowing()
    {
        float alleyEnd = pinsTarget.transform.position.z + BallTarget.size.z / 2 + Alley.border;

        if (GetComponent<Rigidbody>().velocity.magnitude < Ball.stoppedTolerance)
        {
            if (accStopTime > Ball.requiredStoppedTime)
            {
                stopped = true;
                accStopTime = 0f;
            }

            accStopTime += Time.deltaTime;
        }

        Debug.Log("velocity: " + GetComponent<Rigidbody>().velocity.magnitude);

        // ball left alley or stopped moving
        if (transform.position.x > Alley.rightAlleyBorder ||
            transform.position.x < Alley.leftAlleyBorder ||
            transform.position.z > alleyEnd ||
            stopped
            )
        {
            Ball.state = BallState.THROWN;
        }

        lastPosition = transform.position;
    }
}
