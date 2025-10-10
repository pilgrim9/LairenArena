using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class YieldThroughButton : MonoBehaviour
{
    Toggle toggle;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnValueChanged);
        GameController.instance.GameStateUpdated += OnUpdate;
    }

    void OnValueChanged(bool isOn)
    {
        RPCManager.instance.YieldThroughRpc(isOn, GameController.instance.GetLocalPlayerId());
    }
    void OnUpdate(GameState _, GameState state)
    {
        toggle.isOn = state.Players[GameController.instance.GetLocalPlayerId()].autoSkip;
    }
    
    
    
}
