using UnityEngine;
using UnityEngine.UI;

public class ZoomedCard : MonoBehaviour
{
    public static ZoomedCard instance;

    private void Awake()
    {
        instance = this;
    }

    private Image image;
    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
    }

    public void setImage(Sprite sprite)
    {
        image.color = sprite ? Color.white : Color.clear;
        image.sprite = sprite;
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            image.enabled = !image.enabled;
        }
    }
}
