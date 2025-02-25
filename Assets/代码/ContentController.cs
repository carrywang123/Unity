using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ContentController : MonoBehaviour
{
    public TMP_Text contentText;

    static float lineHeight = 43.5f;
    static float StepLineHeight = 41.25f;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ShowDisplayLineCount());
    }


    private IEnumerator ShowDisplayLineCount()
    {
        yield return null;
        int lineCount = contentText.textInfo.lineCount;
        //lineCount++;ʹ��������������������ı����ظ�һ��
        if(this.gameObject.tag == "ʵ�鲽��")
            this.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(this.gameObject.GetComponent<RectTransform>().sizeDelta.x, (lineCount+1) * StepLineHeight);
        else
            this.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(this.gameObject.GetComponent<RectTransform>().sizeDelta.x, (lineCount + 1) * lineHeight);
    }
}
