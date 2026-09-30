using UnityEngine;
using System.Text;

namespace Magotte.TooltipUI
{
    [CreateAssetMenu(fileName = "New Insect Skill", menuName = "Skills/InsectSkill")]
    public class InsectSkills : Skill
    {
        [SerializeField] private Rarity rarity;
        [SerializeField] private string effectText = "Something";
        public Rarity Rarity { get { return rarity; } }
        public string ColouredName
        {
            get
            {
                string hexColour = ColorUtility.ToHtmlStringRGB(rarity.TextColour);
                return $"<color=#{hexColour}>{Name}</color>";
            }
        }

        public override string Colourname
        {
            get
            {
                string hexColour = ColorUtility.ToHtmlStringRGB(rarity.TextColour);
                return $"<color=#{hexColour}>{Name}</color>";
            }
        }
        public override string GetTooltipInfoText()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(Rarity.Name).AppendLine();
            builder.Append("<color=green>Effect: ").Append(effectText).Append("</color>").AppendLine();
            builder.Append("Ectoplasm: <color=red>-").Append(EctoDmg).Append("</color>");

            return builder.ToString();
        }
    }
}
