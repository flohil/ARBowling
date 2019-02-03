using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Alley
{
    public static bool displayed;
    public static float leftAlleyBorder;
    public static float rightAlleyBorder;
    public static float border = 4f; // border around image target added to alley plane
    public static float farthestPositioningZ; // area in which ball can be positioned
    public static float nearestPositioningZ;
    public static bool tooClose = false;
    public static float allowedPinAreaZ;
}

public class alleyController : MonoBehaviour
{
    GameObject ball;
    GameObject ballTarget;
    GameObject pinsTarget;
    GameObject throwLine;
    Renderer renderer;
    Shader diffuseShader;
    Shader transparentShader;

    float textureRatio;
    float textureWidth;
    float zScaleFactor;
    float maxErrorDistance;

    Color alleyMaterialColor;
    Color alleyMaterialErrorColor;
    float minOpacity;
    float maxOpacity;

    string tooCloseMessage;
    string alignMessage;

    // Use this for initialization
    void Start()
    {
        ball = GameObject.Find("ball");
        ballTarget = GameObject.Find("BallTarget");
        pinsTarget = GameObject.Find("PinsTarget");
        throwLine = GameObject.Find("ThrowLine");

        renderer = GetComponent<Renderer>();       

        minOpacity = 0.5f;
        maxOpacity = 0.7f;
        alleyMaterialColor = new Color(208f / 255f, 169f / 255f, 125f / 255f, 1f);
        alleyMaterialErrorColor = new Color(1f, 0f, 0f, minOpacity);
        renderer.material.color = alleyMaterialColor;

        textureRatio = 1920f / 1080f;

        zScaleFactor = transform.localScale.z / renderer.bounds.size.z;

        Alley.leftAlleyBorder = -renderer.bounds.extents.x;
        Alley.rightAlleyBorder = renderer.bounds.extents.x;
        maxErrorDistance = Alley.rightAlleyBorder * 3f;

        diffuseShader = Shader.Find("Legacy Shaders/Diffuse");
        transparentShader = Shader.Find("Transparent/VertexLit with Z");

        tooCloseMessage = "Ball too close to pins!";
        alignMessage = "Place ball within alley!";

        throwLine.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Alley.displayed)
        {
            this.setAlleyShader();
            this.setAlleyLengthAndTiling();
        }
    }

    void setAlleyLengthAndTiling()
    {
        // calculate z distance between the two image targets
        float zDistance = Mathf.Abs(ballTarget.transform.position.z - pinsTarget.transform.position.z);

        // adapt scale in z axis to distance between the two image targets
        float newLength = zDistance + BallTarget.size.z + 2 * Alley.border;

        // set center of alley plane to middle between the two targets
        transform.position = new Vector3(transform.position.x, transform.position.y, pinsTarget.transform.position.z - newLength/2 + BallTarget.size.z/2 + Alley.border);
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, zScaleFactor * newLength);

        Vector3 bounds = renderer.bounds.size;
        float height = bounds.x;
        float width = bounds.z;
        float ratio = width / height;
        float tiling = ratio / textureRatio;

        // make smoother
        renderer.material.mainTextureScale = new Vector2(1, tiling);

        Alley.farthestPositioningZ = ballTarget.transform.position.z - BallTarget.size.z * 0.5f - Alley.border;
        Alley.nearestPositioningZ = ballTarget.transform.position.z + BallTarget.size.z * 0.5f + Alley.border;

        throwLine.transform.position = new Vector3(throwLine.transform.position.x, throwLine.transform.position.y, Alley.nearestPositioningZ);

        Alley.allowedPinAreaZ = pinsTarget.transform.position.z - BallTarget.size.z;

        if (Alley.nearestPositioningZ > Alley.allowedPinAreaZ)
        {
            Alley.tooClose = true;
            GUI.errorMessage = tooCloseMessage;
            GUI.distanceBonus = 0;
        }
        else
        {
            float minLength = 2.75f * BallTarget.size.z + 2f * Alley.border;

            Alley.tooClose = false;
            GUI.distanceBonus = newLength / minLength;
            GUI.lockedDistanceBonus = GUI.distanceBonus;
        }
    }

    void setAlleyShader()
    {
        if (ballTarget.transform.position.x < Alley.leftAlleyBorder || ballTarget.transform.position.x > Alley.rightAlleyBorder || Alley.tooClose)
        {
            if (renderer.material.shader == diffuseShader)
            {
                renderer.material.shader = transparentShader;
                ball.SetActive(false);
                throwLine.SetActive(false);
            }

            // the closer the ball gets to the alley, the more natural and opaque the texture should look
            float xErrorDistance = Mathf.Abs(ballTarget.transform.position.x) - Mathf.Abs(Alley.leftAlleyBorder);
            float opacity = minOpacity;
            float closeness = 0;

            if (xErrorDistance < maxErrorDistance)
            {
                closeness = 1 - xErrorDistance / maxErrorDistance;

                float error = 1 - closeness;

                opacity = minOpacity + closeness * (maxOpacity - minOpacity);
                Color color = closeness * alleyMaterialColor + error * alleyMaterialErrorColor;

                renderer.material.color = new Color(color.r, color.g, color.b, opacity);
            }

            GUI.errorMessage = alignMessage;
        }
        else
        {
            if (renderer.material.shader == transparentShader)
            {
                renderer.material.shader = diffuseShader;
                renderer.material.color = alleyMaterialColor;

                ball.SetActive(true);
            }

            throwLine.SetActive(true);
        }
    }
}
