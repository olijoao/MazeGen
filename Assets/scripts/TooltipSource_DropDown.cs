using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class TooltipSource_DropDown : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler{

    public void OnPointerEnter(PointerEventData eventData){
        var label = GetComponentInChildren<TMP_Text>();
        Debug.Assert(label != null);

        var obj_tooltip = GameObject.Find("ToolTip");

        //change text + update RectTransform of pbj_tooltip
        obj_tooltip.GetComponent<Text>().text = "<b>" + label.text + "</b>\n" + WinCondition.winConditions.Find(x => x.name == label.text).tooltip;
        Canvas.ForceUpdateCanvases();   //update RectTransform pf pbj_tooltip

        //set position of tooltip + make sure it doesn't go over screen + padding
        var pos = obj_tooltip.transform.position;
        pos.y = transform.position.y - obj_tooltip.GetComponent<RectTransform>().rect.height / 2;
        pos.y = Mathf.Max(20 + pos.y, 20 + obj_tooltip.GetComponent<RectTransform>().rect.height / 2);
        obj_tooltip.transform.position = pos;

        // make sure always shown on left side
        var local = obj_tooltip.transform.localPosition;
        local.x = -Mathf.Abs(local.x);
        obj_tooltip.GetComponent<Text>().alignment = TextAnchor.UpperRight;
        obj_tooltip.transform.localPosition = local;
    }

    
    public void OnPointerExit(PointerEventData eventData){
        GameObject.Find("ToolTip").GetComponent<Text>().text = "";
    }

    void Start() { /*...*/}
    void Update() { /*...*/}
}
