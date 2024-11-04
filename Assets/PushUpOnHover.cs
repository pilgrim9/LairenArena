using UnityEngine;
using UnityEngine.EventSystems;

public class PushUpOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
{ 
    public float offset = 10f;
    public bool inverted;

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.Translate(Vector3.up * offset * (inverted?-1:1));
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.Translate(Vector3.down * offset * (inverted?-1:1));
    }
}
