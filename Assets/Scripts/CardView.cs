using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Cards.Card cardData;
    public Image image;
    private void Awake()
    {
        image = GetComponent<Image>();
        Show();
    }

    private bool isHidden = false;
    public bool isPlayable = false;

    public void Hide()
    {
        isHidden = true;
        image.sprite = CardImageLoader.instance.GetSprite("CardBack");
    }

    public void Show()
    {
        isHidden = false;
        image.sprite = getCardImage();
    }

    public Sprite getCardImage()
    {
        return CardImageLoader.instance.GetSprite(cardData.Name);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHidden) return;
        ZoomedCard.instance.setImage(image.sprite);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHidden) return;
        ZoomedCard.instance.setImage(null);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isHidden) return;
        RPCManager.instance.RPCSelectCardForBottom(cardData);
        if (!isPlayable) return;
        if (cardData.currentZone == Zone.Hand)
        {
            RPCManager.instance.RpcAddCardToStack(cardData);
        }
        // Else check for activated abilities that can be activated from the card's current zone.
    }
    
 
}
