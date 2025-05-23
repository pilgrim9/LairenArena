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
        if (cardData == null) Debug.Log("No card data");
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
        if (isHidden)
        {
            Debug.Log($"Card {cardData.Name} is hidden, ignoring click");
            return;
        }

        Debug.Log($"Selected card {cardData.Name} for bottom of deck functionality");
        RPCManager.instance.RPCSelectCardForBottom(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        if (cardData.currentZone == Zone.Reserve)
        {
            RPCManager.instance.RpcPay(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        }

        if (!isPlayable)
        {
            Debug.Log($"Card {cardData.Name} is not playable, ignoring play attempt");
            return;
        }

        if (cardData.currentZone == Zone.Hand)
        {
            Debug.Log($"Attempting to play card {cardData.Name} from hand to stack");
            RPCManager.instance.RpcAddCardToStack(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        }
        else
        {
            Debug.Log($"Card {cardData.Name} is in {cardData.currentZone} zone, checking for activated abilities");
        }
    }
    
 
}
