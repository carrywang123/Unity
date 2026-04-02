
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace game_2
{
public class ButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Sprite sprite;
    Sprite originalSprite;
    Color color;
    bool flag;
    private void Start()
    {
        if (this.gameObject.tag == "主视角")
            flag = true;
        else
            flag = false;
        sprite = Resources.Load<Sprite>("2");
    }

    public bool ChangeFlag(bool flag)
    {
        this.flag = flag;
        if (this.flag == flag)
            return true;
        else
            return false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!flag)
        {
            originalSprite = GetComponent<Button>().image.sprite;
            // 改变按钮的样式
            GetComponent<Button>().image.sprite = sprite;
            Transform childTransform = transform.Find("Text (TMP)");
            if (childTransform != null)
            {
                color = childTransform.gameObject.GetComponent<TMP_Text>().color;
                childTransform.gameObject.GetComponent<TMP_Text>().color = Color.white;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!flag)
        {
            // 恢复按钮的样式
            GetComponent<Button>().image.sprite = originalSprite;
            Transform childTransform = transform.Find("Text (TMP)");
            if (childTransform != null)
            {
                childTransform.gameObject.GetComponent<TMP_Text>().color = color;
            }
        }
        
    }
}
}
