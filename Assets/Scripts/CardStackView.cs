using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardStackView : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData){}
    public Zone zone;
    public int Player;
    public bool IsHidden;
    public bool IsSingleStack;
    private Image image;
    public bool isOwner;

    protected virtual int GetPlayer()
    {
        if (isOwner) return GameController.instance.GetLocalPlayerId();
        return 1 - GameController.instance.GetLocalPlayerId();
    }
    protected virtual List<int> GetCardList(GameState _new)
    {
        return  _new.Players[GetPlayer()].GetZone(zone);
    }
    GameObject cardViewPrefab;
    private void Awake()
    {
        image = GetComponent<Image>();
        cardViewPrefab = Resources.Load("CardVisual") as GameObject;
    }

    private void Start()
    {
        GameController.instance.GameStateUpdated += UpdateView;
        Debug.Log("Started CardStackView " + zone + " " + GetPlayer());
    }
    
    void UpdateView(GameState old, GameState _new)
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
        HandleCount(_new);
        
        if (IsSingleStack)  {
            HandleSingleStackView(_new);
            return;
        }
        List<int> cardList = GetCardList(_new);
        while (cardList.Count > transform.childCount) NewCardView();
        for (int i = 0; i < transform.childCount; i++)
        {
            CardView view = transform.GetChild(i).GetComponent<CardView>();
            if (i >= cardList.Count) {
                view.gameObject.SetActive(false);
                continue;
            }
            Cards.Card card = Cards.getCardFromID(cardList[i]);
            view.cardData = card;
            if (IsHidden) view.Hide(); 
            else view.Show();
            
            if (card.CanBePlayedBy(GetPlayer()) && 
                card.CanBePlayedFrom(zone) && 
                _new.Players[GetPlayer()].CanPay(card)) view.isPlayable = true; 
            
            if (card.CanActivateAbilities()) view.isPlayable = true;
        }
    }

    public TMPro.TextMeshProUGUI countText;
    private void HandleCount(GameState _new) {
        if (countText == null) return;
        countText.text = GetCardList(_new).Count.ToString();
    }

    private void NewCardView() {
        CardView view = Instantiate(cardViewPrefab).GetComponent<CardView>();
        view.transform.SetParent(transform, false);
    }
    private void HandleSingleStackView(GameState _new) {
        if (GetCardList(_new).Count == 0)
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
            Cards.Card topCard = _new.Players[GetPlayer()].getTopCardFrom(zone);
            image.sprite = CardImageLoader.instance.GetSprite(topCard.Name);;
            image.color = Color.white;
        }
    }
    
}
