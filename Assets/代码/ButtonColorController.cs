using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class ButtonColorController : MonoBehaviour
{
    public Button[] Buttons;
    public Sprite[] images;
    public TMP_Text[] Texts;
    private Color[] FontColor;
    bool flag;
    // Start is called before the first frame update
    void Start()
    {
        flag = false;
        FontColor = new Color[2];
        FontColor[0] = Color.black;
        FontColor[1] = Color.white;
        foreach(Button button in Buttons)
        {
            if (button.gameObject.GetComponent<ButtonHoverHandler>() == null)
                button.gameObject.AddComponent<ButtonHoverHandler>();
        }
    }

    public void ChangeButtonColor(int value)
    {
        if (value < 0) return ;
        if(value < 4)//视角按钮切换
        {
            ExclusiveChoice(0, 3, value);
        }
        else if(value == 14 || value == 15)
        {
            ExclusiveChoice(14, 15, value);
        }
        else if (value >= 16 && value <= 18)
        {
            ExclusiveChoice(16, 18, value);
        }
        else if (value >= 22 && value <= 23)
        {
            ExclusiveChoice(22, 23, value);
        }
        else
        {
            if(!flag)
            {
                Buttons[value].image.sprite = images[1];
                Texts[value].color = FontColor[1];
            }
        }
    }

    private void ExclusiveChoice(int min,int max,int value)
    {
        for (int i = min; i <= max; i++)
        {
            Buttons[i].image.sprite = images[0];
            Texts[i].color = FontColor[0];
            if (!ChangeButtonFlag(i, false))
                Debug.Log("没有找到按钮的鼠标悬停组件");
        }
        Buttons[value].image.sprite = images[1];
        Texts[value].color = FontColor[1];
        if (!ChangeButtonFlag(value, true))
            Debug.Log("没有找到按钮的鼠标悬停组件");
    }
    public void ToOrigin(int value)
    {
        Buttons[value].image.sprite = images[0];
        Texts[value].color = FontColor[0];
    }
    public bool ChangeButtonFlag(int i,bool flag)
    {
        if (Buttons[i].gameObject.GetComponent<ButtonHoverHandler>() != null)
        {
            Debug.Log(Buttons[i].gameObject.GetComponent<ButtonHoverHandler>().ChangeFlag(flag));
            return true;
        }
        else
            return false;
    }
}
