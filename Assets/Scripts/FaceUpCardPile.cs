using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceUpCardPile : MonoBehaviour
{
    public int previousChildCount = 0;
    private void OnTransformChildrenChanged()
    {
        if (previousChildCount < transform.childCount) return;
        int lastChildIndex = transform.childCount - 1;
        // bring the new child forward
        transform.GetChild(lastChildIndex).position += -Vector3.forward + transform.GetChild(lastChildIndex - 1).position;
    }
}
