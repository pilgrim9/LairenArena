using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using StackObjects;

public static class Cards
{ 
    public static Card getCardFromID(int id) {
        return GameController.instance.gameState.cards[id];
    }
    [Serializable]
    public class Card : Stackable
    {
       
        public List<string> Types = new();
        public List<string> Subtypes = new();
        public List<string> Supertypes = new();
        public int InGameId = 0;
        public int Power = 0;
        public int Resistance = 0;
        public int Cost = 0;
        public int Damage = 0;
        public List<int> Blockers = new();
        public List<string> StaticAbilities = new();
        public List<string> TriggeredAbilities = new();
        public List<string> ActivatedAbilities = new();
        public int Points = 0;
        public Zone OnResolutionTargetZone = Zone.Discard;
        public Zone currentZone;
        public List<Zone> PlayableFrom = new() { Zone.Hand };
        public int controller;

        public bool CanBePlayedFrom(Zone zone)
        {
            return PlayableFrom.Contains(zone);
        }

        public bool CanBePlayedBy(int player)
        {
            return player == Owner;
        }

        public List<int> getControllerZone()
        {
            return GameController.instance.gameState.Players[Caster].GetZone(currentZone);
        }

        public bool CanActivateAbilities()
        {
            foreach (var ability in ActivatedAbilities)
            {
                return Abilities.ActivatedAbilities[ability].PlayableFrom.Contains(currentZone);
            }
            return false;
        }

        public Card Clone()
        {
            // Ugly, ugly deep clone.
            using MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
            stream.Position = 0;
            return (Card)formatter.Deserialize(stream);
        }
    }

    public static Card TreasureGenerico = new() {
        Name = "TESORO GENERICO",
        Types = new List<string> { CardTypes.TREASURE },
        Points = -1
    };
    public static Card ROJO_FUGAZ = new(){
        Name = "ROJO FUGAZ",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CardTypes.ANIMAL},
        Power = 2,
        Resistance = 2,
        Cost = 1,
        StaticAbilities = new List<string> {"ROJO FUGAZ"},
    };
}
