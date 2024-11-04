using UnityEngine;
using UnityEngine.UI;

public class FollowMouse : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        transform.position = Input.mousePosition;    

        // Ensure the image is within screen bounds
        Vector3 mousePosition = Input.mousePosition;
        RectTransform rectTransform = GetComponent<RectTransform>();

        // Calculate half width and half height of the image
        float halfWidth = rectTransform.rect.width / 2;
        float halfHeight = rectTransform.rect.height / 2;

        // Clamp the position to keep the image within screen bounds
        mousePosition.x = Mathf.Clamp(mousePosition.x, halfWidth, Screen.width - halfWidth);
        mousePosition.y = Mathf.Clamp(mousePosition.y, halfHeight, Screen.height - halfHeight);

        transform.position = mousePosition;
    }
}
