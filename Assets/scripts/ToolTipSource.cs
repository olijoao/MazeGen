using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToolTipSource : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler{

    public bool showOnRightSide;
    public string tooltipText;

    public void OnPointerEnter(PointerEventData eventData){
        var obj_tooltip = GameObject.Find("ToolTip");

        //change text + update RectTransform of pbj_tooltip
        obj_tooltip.GetComponent<Text>().text = tooltipText;
        Canvas.ForceUpdateCanvases();

        //set position of tooltip + make sure it doesn't go over screen + padding
        var pos_toolTip = obj_tooltip.transform.position;
        pos_toolTip.y = transform.position.y - obj_tooltip.GetComponent<RectTransform>().rect.height/2;
        pos_toolTip.y = Mathf.Max(20+pos_toolTip.y, 20+obj_tooltip.GetComponent<RectTransform>().rect.height / 2); 
        obj_tooltip.transform.position = pos_toolTip;

        // show tooltip on right vs left side
        var local = obj_tooltip.transform.localPosition;
        local.x = Mathf.Abs(local.x);
        if (!showOnRightSide) { 
            local.x = -local.x;
            obj_tooltip.GetComponent<Text>().alignment = TextAnchor.UpperRight;
        }else
            obj_tooltip.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
        obj_tooltip.transform.localPosition = local;
    }


    public void OnPointerExit(PointerEventData eventData){
        GameObject.Find("ToolTip").GetComponent<Text>().text = "";
    }

    void Start()    { /*...*/}
    void Update()   { /*...*/}

}