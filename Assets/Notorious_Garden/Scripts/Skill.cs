using UnityEngine;

namespace Magotte.TooltipUI
{
    public abstract class Skill : ScriptableObject
    {
        [SerializeField] private new string name;
        [SerializeField] private int EctoplasmCost;
        [SerializeField] private double lethality;
        [SerializeField] private double stealth;
        [SerializeField] private int population;


        public string Name { get { return name; } }
        public abstract string Colourname { get; }
        public int EctoDmg { get { return EctoplasmCost; } }
        
        public abstract string GetTooltipInfoText();


    }
}