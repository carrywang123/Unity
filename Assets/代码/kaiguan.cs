using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IClickable
{
    void OnClick();
}
public class kaiguan : MonoBehaviour, IClickable
{
    public GameObject step;
    public GameObject Power;
    public ChangeTemperature ChangeTemperature;
    private ChangeTextColor changeTextColor;
    // Start is called before the first frame update
    void Start()
    {
        changeTextColor = step.GetComponent<ChangeTextColor>();
    }

    public void OnClick()
    {
        OnMouseDown();
    }
    private void OnMouseDown()
    {
        if ((changeTextColor.currenttext == 4 && this.gameObject.tag == "µ÷Áã") ||
            (changeTextColor.currenttext == 1 && this.gameObject.tag == "kaiguan"))
        {
            if(this.gameObject.tag == "µ÷Áã")
            {
                ChangeTemperature.changeresult("0.000");
            }
            if (this.gameObject.tag == "kaiguan")
            {
                Power.SetActive(false);
            }
            changeTextColor.Changetext();

        }
    }
}
