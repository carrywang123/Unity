using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Changepage : MonoBehaviour
{
    public GameObject SYYLandSYMD;
    public GameObject SYMD;
    public GameObject SYYL;
    public GameObject KS;
    public GameObject bg;
    public GameObject panel;

    public GameObject step;
    private ChangeTextColor changeTextColor;

    private void Start()
    {
        changeTextColor = step.GetComponent<ChangeTextColor>();
    }

    public void TOSYYLpage()
    {
        SYYLandSYMD.SetActive(true);
        SYYL.SetActive(true);
        SYMD.SetActive(false);
        KS.SetActive(false);
        bg.SetActive(false);
        panel.SetActive(true);
    }
    public void TOSYMDpage()
    {
        SYYLandSYMD.SetActive(true);
        SYYL.SetActive(false);
        SYMD.SetActive(true);
        KS.SetActive(false);
        bg.SetActive(false);
        panel.SetActive(true);
    }
    public void TOKSpage()
    {
        SYYLandSYMD.SetActive(true);
        SYYL.SetActive(false);
        SYMD.SetActive(false);
        KS.SetActive(true);
        bg.SetActive(false);
        panel.SetActive(true);
        
    }

    public void TObgpage()
    {
        SYYLandSYMD.SetActive(false);
        SYYL.SetActive(false);
        SYMD.SetActive(false);
        KS.SetActive(false);
        bg.SetActive(true);
        panel.SetActive(true);
        switch (changeTextColor.currenttext)
        {
            case 10:
            case 14:
            case 18:
            case 22:
                {
                    changeTextColor.Changetext();
                    break;
                }
        }
    }
    public void CloseAll()
    {
        SYYLandSYMD.SetActive(false);
        SYYL.SetActive(false);
        SYMD.SetActive(false);
        KS.SetActive(false);
        bg.SetActive(false);
        panel.SetActive(false);
    }
    public void ChangeScene()
    {
        SceneManager.LoadScene(1);
    }
}