using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeImage : MonoBehaviour
{
    public Sprite[] imgs;
    int currentimg;
    Image myimage;
    private void Start()
    {
        currentimg = 0;
        myimage = GetComponent<Image>();
    }

    public void Rightimgs()
    {
        if(currentimg<imgs.Length-1)
        {
            currentimg++;
            myimage.sprite = imgs[currentimg];
        }
    }

    public void Leftimgs()
    {
        if (currentimg > 0)
        {
            currentimg--;
            myimage.sprite = imgs[currentimg];
        }
    }

}
