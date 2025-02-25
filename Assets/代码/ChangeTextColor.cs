using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ChangeTextColor : MonoBehaviour
{
    public GameObject textgameobject;
    public TMP_Text textComponent;
    public TMP_Text currentstep;
    public GameObject scrollView;
    public GameObject audioobject;
    public string[] allrichtext;
    private AudioController audioController;
    private ScrollRect scrollRect;
    private string originalText;  // ���ڴ洢ԭʼ�ı�
    private string colorTagStart = "<color=red>";  // ��ʼ��ǩ������Ϊ��ɫ
    private string colorTagEnd = "</color>";  // ������ǩ
    int repeat;//��¼�ظ����� 0 1 2:ˮ�ĵ絼����  3 4 5:��Һ�����μ�ˮ  6 7 8:

    public int currenttext { get; set; }
    void Start()
    {
        audioController = audioobject.GetComponent<AudioController>();
        scrollRect = scrollView.gameObject.GetComponent<ScrollRect>();
        normalizeSteps();
        originalText = textComponent.text;  // �洢ԭʼ�ı�
        repeat = 0;
        currenttext = 0;
        TextColorChange(allrichtext[currenttext],currenttext);
    }

    public void Changetext()
    {
        if (currenttext == 9 && repeat < 2)
        {
            currenttext = 6;
            repeat++;
        }
        else if (currenttext == 13 && repeat < 4 && repeat >= 2)
        {
            currenttext = 10;
        }
        else if (currenttext == 17 && repeat < 6 && repeat >= 4)
        {
            currenttext = 14;
            repeat++;
        }
        else if (currenttext == 21 && repeat < 8 && repeat >= 6)
        {
            currenttext = 18;
            repeat++;
        }
        else currenttext++;
        TextColorChange(allrichtext[currenttext],currenttext);
        openaudio(currenttext);
        ScrollToHighlightedText(allrichtext[currenttext],currenttext);
    }

    public void TextColorChange(string highlightedText, int j)
    {
        // �����ı���ֻ�ı�ָ�����ֵ���ɫ
        string normalizeHighLightText = " " + (j + 1).ToString() + "." + highlightedText;
        textComponent.text = originalText.Replace(normalizeHighLightText, colorTagStart + normalizeHighLightText + colorTagEnd);
        currentstep.text = normalizeHighLightText;
    }

    void ScrollToHighlightedText(string highlightedText,int j)
    {
        string normalizeHighLightText = " " + (j + 1).ToString() + "." + highlightedText;
        int index = textComponent.text.IndexOf(normalizeHighLightText);
        if (index != -1)  // ȷ���ı�ȷʵ���ҵ�
        {
            // ���²�����ȷ��textInfo�����µ�
            textComponent.ForceMeshUpdate();
            TMP_TextInfo textInfo = textComponent.textInfo;
            try
            {
                if (index < textInfo.characterInfo.Length)  // ���index�Ƿ���characterInfo����Ч��Χ��
                {
                    int line = textInfo.characterInfo[index].lineNumber;
                    float linePosition = (float)line / (float)(textInfo.lineCount - 1);
                    textComponent.ForceMeshUpdate();
                    scrollRect.verticalNormalizedPosition = 1.0f - linePosition;
                }
            }
            catch(Exception e)
            {
                Debug.Log(e);
            }
            
        }
    }
    public void changeStep()
    {
        switch(currenttext)
        {
            case 9:
            case 13:
            case 17:
            case 21:
                {
                    Changetext();
                    break;
                }
        }
    }
    private void normalizeSteps()
    {
        StringBuilder sb = new StringBuilder();
        int j = 1;
        if (allrichtext.Length == 0)
            Debug.Log("�޲���");
        else
            foreach (string st in allrichtext)
            {
                sb.Append(" " +j.ToString() + "." + st +"\n");
                j++;
            }
        textComponent.text = sb.ToString();
    }
    public void showAllStep()
    {
        if (textgameobject.activeSelf)
        {
            textgameobject.SetActive(false);
        }
        else
        {
            textgameobject.SetActive(true);
            if (currenttext != 0)
                ScrollToHighlightedText(allrichtext[currenttext],currenttext);
        }
    }

    public void openaudio(int num)
    {
        audioController.audioStart(num);
    }

    public void stopaudio()
    {
        audioController.audioStop();
    }
}
