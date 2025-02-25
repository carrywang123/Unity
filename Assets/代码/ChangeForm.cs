using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeForm : MonoBehaviour
{
    public GameObject form1;
    public GameObject form2;
    public GameObject form1Andform2;
    public GameObject form3;
    // Start is called before the first frame update

    public void showform1()
    {
        form1.SetActive(true);
        form2.SetActive(false);
        form3.SetActive(false);
    }
    public void showform2()
    {
        form1.SetActive(false);
        form2.SetActive(true);
        form3.SetActive(false);

    }
    public void showform3()
    {
        form1Andform2.SetActive(false);
        form1.SetActive(false);
        form2.SetActive(false);
        form3.SetActive(true);
    }
    public void closeall()
    {
        form1.SetActive(false);
        form2.SetActive(false);
        form3.SetActive(false);
    }
}
