using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardStackView : MonoBehaviour, IPointerClickHandler
{
    private RectTransform _transform;

    public void OnPointerClick(PointerEventData eventData){}
    public Zone zone;
    [HideInInspector] public int Player;
    public bool IsHidden;
    public bool IsSingleStack;
    private Image image;
    public bool isOwner;

    protected virtual int GetPlayer()
    {
        return Player;
    }
    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Start()
    {
        GameController.instance.GameStateUpdated += UpdateZone;
        Debug.Log("Started CardStackView " + zone + " " + GetPlayer());
    }

    void UpdateZone(GameState old, GameState _new)
    {
        if (_new == null) return;
        if (old != null)
        {
            // this helps avoid unnecessary updates
            if (_new.Players.Count == 0 || old.Players.Count == 0) return;
            if (_new.Players.Count <= GetPlayer() || old.Players.Count <= GetPlayer()) return;
            // if (_new.Players[GetPlayer()].GetZone(zone) == null || old.Players[GetPlayer()].GetZone(zone) == null) return;
            // if (_new.Players[GetPlayer()].GetZone(zone) == old.Players[GetPlayer()].GetZone(zone)) return;
        }
        if (IsSingleStack)
        {
            if (_new.Players[GetPlayer()].GetZone(zone).Count == 0)
            {
                image.sprite = null;
                image.color = Color.clear;
            }
            else if (IsHidden)
            {
                image.sprite = CardImageLoader.instance.GetSprite("CardBack");
                image.color = Color.white;
            }
            else if (!IsHidden)
            {
                image.sprite = _new.Players[GetPlayer()].GetZone(zone)[^1].view.getCardImage();
                image.color = Color.white;
            }
        }
        foreach (var card in _new.Players[GetPlayer()].GetZone(zone))
        {
            if (zone == Zone.Hand && name == "EnemyHand")
            {
                Debug.Log("something");

            }
            Debug.Log("Updating card " + card.view);
            card.view.transform.SetParent(transform, false);
            
            if (IsHidden) card.view.Hide(); 
            else card.view.Show();
            
            if (card.CanBePlayedBy(GetPlayer()) && 
                card.CanBePlayedFrom(zone) && 
                _new.Players[GetPlayer()].CanPay(card)) card.view.isPlayable = true; 
            
            if (card.CanActivateAbilities()) card.view.isPlayable = true;
        }
    }
    
}
