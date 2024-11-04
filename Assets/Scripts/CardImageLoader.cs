using System;
using System.Collections.Generic;
using UnityEngine;

public class CardImageLoader : MonoBehaviour
{
    public static CardImageLoader instance;
    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

    private void Awake()
    {
        instance = this;
    }

    public Sprite GetSprite(string name)
    {
        if (_sprites.ContainsKey(name))
        {
            return _sprites[name];
        }

        var path = $"Cards/{name}";
        var sprite = Resources.Load<Sprite>(path);
        if (!sprite)
        {
            return null;
        }
        _sprites.Add(name, sprite);

        return sprite;
    }
}
