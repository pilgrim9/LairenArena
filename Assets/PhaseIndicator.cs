using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhaseIndicator : MonoBehaviour
{
    private TextMeshProUGUI text;
    private Image image;
    
    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        image = GetComponent<Image>();
    }

    private void Start()
    {
        GameController.instance.GameStateUpdated += OnGameStateUpdated;
        text.enabled = false;
        image.enabled = false;
        
    }

    private void OnGameStateUpdated(GameState old, GameState _new)
    {
        if (old != null && _new.currentPhase == old.currentPhase) return;
        text.text = _new.currentPhase.ToString();
        text.enabled = true;
        image.enabled = true;
        Invoke(nameof(Disable), 1.5f);
    }
    

    private void Disable()
    {
        text.enabled = false;
        image.enabled = false;
    }
}
