using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChangeTemperature : MonoBehaviour
{
    public TMP_Text resultbochang;
    // Start is called before the first frame update

    public void changeresult(string result)
    {
        resultbochang.text = result;
    }

}
