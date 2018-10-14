using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ArrowTest : MonoBehaviour
{
    public GameObject goTarget;
    //public GameObject visualizer;
    public Camera cam;
    private Transform arrowIndicator;
    private Image image;
    private Image arrowImage;
    public float borderPad = 0.1f;


    private void Start()
    {
        image = GetComponent<Image>();
        arrowIndicator = this.gameObject.transform.GetChild(0);
        arrowImage = arrowIndicator.gameObject.GetComponent<Image>();
    }

    void Update()
    {
        PositionArrow();
    }

    void PositionArrow()
    {
        Vector3 screenPos = cam.WorldToScreenPoint(goTarget.transform.position + Vector3.up * .5f);

        //visualizer.transform.position = screenPos;

        if (screenPos.z > 0 && screenPos.x > 0 && screenPos.y > 0 && screenPos.x < Screen.width && screenPos.y < Screen.height)
        {
            image.enabled = false;
            arrowImage.enabled = false;
            
            //On screen indicators

        }
        else
        {
            image.enabled = true;
            arrowImage.enabled = true;

            //Off screen indicators
            if (screenPos.z < 0) screenPos *= -1; //If target is behind camera, invert value

            Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0) / 2;

            Vector3 screenBounds = new Vector3(screenCenter.x - borderPad, 
                                                screenCenter.y - borderPad, 
                                                0f);

            screenPos = new Vector3(Mathf.Clamp(screenPos.x, screenCenter.x - screenBounds.x, screenCenter.x + screenBounds.x), 
                                    Mathf.Clamp(screenPos.y, screenCenter.y - screenBounds.y, screenCenter.y + screenBounds.y), 
                                    screenPos.z);

            transform.position = screenPos;
        }

    }
}
