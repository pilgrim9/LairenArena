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
        public override int GetRelatedCard()
        {
            return InGameId;
        }
        public List<string> Types = new();
        public List<string> Subtypes = new();
        public List<string> Supertypes = new();
        public int Power = 0;
        public int Resistance = 0;
        public int Cost = 0;
        public int Damage = 0;
        public List<int> Blockers = new();
        public List<string> StaticAbilities = new();
        public List<string> TriggeredAbilities = new();
        public List<string> ActivatedAbilities = new();
        public List<string> AdditionalCosts = new();

        public int Points = 0;
        Zone OnResolutionTargetZone = Zone.Discard;
        public Zone getResolutionTargetZone()
        {
            if (Types.Contains(CardTypes.ALLY)) return Zone.Regroup;
            else return OnResolutionTargetZone;
        }
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
        Subtypes = new List<string> {CreatureTypes.ANIMAL},
        Power = 2,
        Resistance = 2,
        Cost = 1,
        StaticAbilities = new List<string> {"FRENZY"},
    };
    public static Card SOMBRA_DEL_DESIERTO = new(){
        Name = "SOMBRA DEL DESIERTO",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CreatureTypes.BRUJA, CreatureTypes.DESERTOR},
        Power = 3,
        Resistance = 3,
        Cost = 2,
    };
    public static Card BRUJA_ELEMENTALISTA = new(){
        Name = "HECHICERA ELEMENTAL",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CreatureTypes.BRUJA},
        Supertypes = new List<string> {Supertypes.ROYALTY},
        Power = 2,
        Resistance = 3,
        Cost = 2,
    };
    public static Card ANCIANA_MAESTRA = new(){
        Name = "ANCIANA MAESTRA",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CreatureTypes.BRUJA},
        Power = 3,
        Resistance = 4,
        Cost = 5,
    };
    public static Card CUMULO_DE_HONGOS = new(){
        Name = "CUMULO DE HONGOS",
        Types = new List<string> {CardTypes.MONUMENT},
        Cost = 1,
    };
    public static Card CIUDAD_EN_LLAMAS = new(){
        Name = "CIUDAD EN LLAMAS",
        Types = new List<string> {CardTypes.ORDER, CardTypes.FAST},
        Cost = 3,
    };
    
    public static Card MUERTE_INMINENTE = new(){
        Name = "MUERTE INMINENTE",
        Types = new List<string> {CardTypes.ORDER, CardTypes.FAST},
        Cost = 1,
        AdditionalCosts = new List<string> {Costs.Pay2Life}
    };
    public static Card PLANES_FRUSTRADOS = new(){
        Name = "PLANES FRUSTRADOS",
        Types = new List<string> {CardTypes.ORDER, CardTypes.FAST},
        Cost = 2,
    };
    public static Card RITUAL_DE_NEGACION = new(){
        Name = "RITUAL DE NEGACION",
        Types = new List<string> {CardTypes.ORDER},
        Cost = 1,
        AdditionalCosts = new List<string> {Costs.DiscardACard}
    };
    public static Card LIDER_DE_LA_MANADA = new(){
        Name = "LIDER DE LA MANADA",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CreatureTypes.ANIMAL},
        Power = 3,
        Resistance = 3,
        Cost = 2,
    };
    public static Card FELINO_DE_LA_MONTANA = new(){
        Name = "FELINO DE LA MONTAÑA",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CreatureTypes.ANIMAL},
        Power = 1,
        Resistance = 1,
        Cost = 2,
    };
    public static Card GATITOS_DE_BRUJA = new(){
        Name = "GATITOS DE BRUJA",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CreatureTypes.ANIMAL, CreatureTypes.BRUJA},
        Power = 1,
        Resistance = 1,
        Cost = 1,
    };
    public static Card CASCABUFALO = new(){
        Name = "CASCABUFALO",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CreatureTypes.ANIMAL},
        Power = 1,
        Resistance = 2,
        Cost = 1,
    };
    public static Card NICOL_LA_APRENDIZ = new(){
        Name = "NICOL, LA APRENDIZ",
        Types = new List<string> {CardTypes.ALLY},
        Subtypes = new List<string> {CreatureTypes.BRUJA},
        Power = 1,
        Resistance = 2,
        Cost = 2,
    };
}