using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Cards.Card cardData;
    public Image image;
    public Outline outline;
    private void Awake()
    {
        Show();
    }

    private void Start()
    {
        GameController.instance.GameStateUpdated += UpdateView;
    }

    private bool isHidden = false;
    public bool isPlayable => cardData.CanBePlayedByOwner();

    public void Hide()
    {
        isHidden = true;
        image.sprite = CardImageLoader.instance.GetSprite("CardBack");
    }


    public void Show()
    {
        isHidden = false;
        image.sprite = getCardImage();
        gameObject.SetActive(true);
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
        if (cardData.currentZone == Zone.Reserve)
        {
            RPCManager.instance.RpcPay(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        }
        if (GameController.instance.gameState.currentPhase == Phase.Mulligan)
        {
            Debug.Log($"Selected card {cardData.Name} for bottom of deck functionality");
            RPCManager.instance.RPCSelectCardForBottom(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        }

        if (cardData.currentZone == Zone.Regroup)
        {
            Debug.Log($"Attempting to move card {cardData.Name} from regroup to attack");
            RPCManager.instance.RpcSelectAttacker(cardData.InGameId, GameController.instance.GetLocalPlayerId());
            RPCManager.instance.RpcSelectBlocker(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        }
        if (cardData.currentZone == Zone.Attackers)
        {
            if (cardData.IsOwnerLocal())
            {
                Debug.Log($"Attempting to move card {cardData.Name} from attack to regroup");
                RPCManager.instance.RpcSelectAttacker(cardData.InGameId, GameController.instance.GetLocalPlayerId());
            }
            else
            {
                Debug.Log($"Slected card {cardData.Name} as block target.");
                RPCManager.instance.RpcSelectBlockTarget(cardData.InGameId, GameController.instance.GetLocalPlayerId());
            }
        }
        if (cardData.currentZone == Zone.Hand && isPlayable)
        {
            Debug.Log($"Attempting to play card {cardData.Name} from hand to stack");
            RPCManager.instance.RpcAddCardToStack(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        }
        if (cardData.currentZone == Zone.Blockers)
        {
            RPCManager.instance.RpcSelectBlocker(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        }

        if (GameController.instance.gameState.state == State.AwaitingTarget)
        {
            RPCManager.instance.RpcSelectTarget(cardData.InGameId, GameController.instance.GetLocalPlayerId());
        }
        Debug.Log($"Card {cardData.Name} is in {cardData.currentZone} zone, checking for activated abilities");
    }

    public void UpdateView(GameState old, GameState _new)
    {

        if (_new == null) return;
        outline.effectColor = Color.clear;
        if (isHidden)
        {
            return;
        }
        if (cardData.Owner != GameController.instance.GetLocalPlayerId())
        {
            UpdateOpponentCards(_new);
            return;
        }
        UpdatePlayerCards(_new);
      
    }
    private void UpdatePlayerCards(GameState _new)
    {
        if (cardData.currentZone == Zone.Reserve && cardData.getOwner().AmountToPay > 0)
        {
            // highlight as usable
            outline.effectColor = Color.yellow;
            return;
        }
        if (_new.currentPhase == Phase.Mulligan && cardData.getOwner().AwaitingBottomDecision)
        {
            outline.effectColor = Color.yellow;
            return;
        }
        if (_new.currentPhase == Phase.DeclareAttackers &&
            !cardData.getOwner().hasDeclaredAttack &&
            (cardData.currentZone == Zone.Attackers || cardData.currentZone == Zone.Regroup))
        {
            outline.effectColor = Color.yellow;
        }

        if (cardData.Owner == _new.GetInActivePlayerID() &&
            _new.currentPhase == Phase.DeclareBlockers &&
            !cardData.getOwner().hasDeclaredBlock &&
            (cardData.currentZone == Zone.Regroup || Zone.Blockers == cardData.currentZone))
        {
            outline.effectColor = Color.yellow;
        }

        if (cardData.currentZone == Zone.Hand && isPlayable)
        {
            outline.effectColor = Color.yellow;
        }

        if (_new.state == State.AwaitingTarget && _new.CurrentTargetInfo.IsValidTarget(cardData.InGameId, _new.GetActivePlayer()))
        {
            outline.effectColor = Color.red;
        }
    }
    private void UpdateOpponentCards(GameState _new)
    {
        if (cardData.Owner == _new.ActivePlayer &&
            _new.currentPhase == Phase.DeclareBlockers &&
            !cardData.getOwner().hasDeclaredBlock &&
            cardData.currentZone == Zone.Attackers)
        {
            outline.effectColor = Color.yellow;
        }
    }
}
