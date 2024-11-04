using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Mirror;
using StackObjects;

public static class Cards
{
    [Serializable]
    public class Card : Stackable
    {
        [SyncVar]
        public string Name;
        [SyncVar]
        public List<string> Types = new();
        [SyncVar]
        public List<string> Subtypes = new();
        [SyncVar]
        public List<string> Supertypes = new();
        [SyncVar]
        public int Power = 0;
        [SyncVar]
        public int Resistance = 0;
        [SyncVar]
        public int Cost = 0;
        [SyncVar]
        public int Damage = 0;
        [SyncVar]
        public List<Card> Blockers = new();
        [SyncVar]
        public List<string> StaticAbilities = new();
        [SyncVar]
        public List<string> TriggeredAbilities = new();
        [SyncVar]
        public List<string> ActivatedAbilities = new();
        [SyncVar]
        public int Points = 0;
        [SyncVar]
        public Zone OnResolutionTargetZone = Zone.Discard;
        [SyncVar]
        public Zone currentZone;
        [NonSerialized] public CardView view;
        [SyncVar]
        public List<Zone> PlayableFrom = new() { Zone.Hand };

        [SyncVar] public int controller;

        public bool CanBePlayedFrom(Zone zone)
        {
            return PlayableFrom.Contains(zone);
        }

        public bool CanBePlayedBy(int player)
        {
            return player == Owner;
        }

        public List<Card> getControllerZone()
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
