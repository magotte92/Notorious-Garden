using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Magotte.TooltipUI
{
    public class UltimateSkill : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Skill skill;
        [SerializeField] private TooltipPopup tooltipPopup;

        private GameObject btn;
        private Button Btn;
        private Button UnlBtn;
        private Image sprite;
        public int updateCost;
        public void OnPointerEnter(PointerEventData eventData)
        {
            tooltipPopup.DisplayInfo(skill);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipPopup.HideInfo();
        }

        private void Awake()
        {
            sprite = GetComponent<Image>();
            
            btn = this.gameObject;
            Btn = btn.GetComponent<Button>();
            Btn.interactable = false;
            sprite.color = Color.gray;
            
        }
        public void Lock()
        {
            sprite.color = Color.gray;
            Btn.interactable = false;

        }

        public void Unlock()
        {
            sprite.color = Color.white;
            Btn.interactable = true;

        }

        public void RemoveEctoplasm()
        {

            if (Player.instance.score - skill.EctoDmg >= 0.000000001 && Btn.interactable != false)
            {
                Player.instance.score -= skill.EctoDmg;
                Lock();
                Btn.interactable = false;     


            }


        }

        public void OnPointerClick(PointerEventData eventData)
        {
            RemoveEctoplasm();

        }
    }

}
