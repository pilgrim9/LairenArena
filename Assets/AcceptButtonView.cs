using System.Collections;
using System.Collections.Generic;
using Edgegap.Editor;
using UnityEngine;

public class AcceptButtonView : BasePlayerView
{
    void Awake()
    {
        isOwner = true;
    }
    public GameObject AcceptButton;
    public GameObject DeclineButton;
    protected override void UpdateView(GameState old, GameState _new)
    {
        if (_new.playerWithPriority == GetPlayer() || _new.currentPhase == Phase.Mulligan)
        {
            AcceptButton.SetActive(true);
            DeclineButton.SetActive(true);
        }
        else
        {
            AcceptButton.SetActive(false);
            DeclineButton.SetActive(false);
        }
    }
}
