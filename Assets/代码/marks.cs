using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
public class marks : MonoBehaviour
{
    public TMP_InputField[] input1;
    public TMP_InputField[] input2;
    public TMP_InputField[] input3;
    public TMP_InputField[] input4;
    TMP_InputField[][] input;

    List<string> OperationDefaults;
    List<string> DataDefaults;
    int OperationNum;
    int DataNum;
    public TMP_Text OperationResult;
    public TMP_Text DataResult;
    public TMP_Text OperationDefaultResult;
    public TMP_Text DataDefaultResult;
    public TMP_Text TotalResult;
    public string[] AllDefaults1;
    public string[] AllDefaults2;
    public string[] AllDefaults3;
    public string[] AllDefaults4;
    string[][] AllDataDefaults;
    public string[] AllOperationDefaults;
    bool[] AllOperationDefaultsFlag;
    private float OperationMarks;
    private float DataMarks;
    private float TotalMarks;
    static float OperationProportion = 0.6f;
    static float DataProportion = 0.4f;
    // Start is called before the first frame update
    public static float input1_fe = 0.05f;
    public static float input1_scn = 0.0002f;
    public static float input1_guang_min = 0.622f;
    public static float input1_guang_max = 0.643f;
    public static float input1_guangb = 1;
    public static float input1_fescnp = 0.0002f;
    public static float input1_fep = 0.0498f;
    public static float input1_scnp = 0;
    public static float input2_fe = 0.01f;
    public static float input2_scn = 0.0002f;
    public static float input2_guang_min = 0.373f;
    public static float input2_guang_max = 0.413f;
    public static float input2_guangb_min = 0.58f;
    public static float input2_guangb_max = 0.664f;
    public static float input2_fescnp_min = 0.000116f;
    public static float input2_fescnp_max = 0.0001328f;
    public static float input2_fep_min = 0.009867f;
    public static float input2_fep_max = 0.009884f;
    public static float input2_scnp_min = 0.0000672f;
    public static float input2_scnp_max = 0.000084f;
    public static float input2_kci_min = 139.716f;
    public static float input2_kci_max = 200.283f;
    public static float input3_fe = 0.005f;
    public static float input3_scn = 0.0002f;
    public static float input3_guang_min = 0.275f;
    public static float input3_guang_max = 0.279f;
    public static float input3_guangb_min = 0.428f;
    public static float input3_guangb_max = 0.448f;
    public static float input3_fescnp_min = 0.0000856f;
    public static float input3_fescnp_max = 0.0000896f;
    public static float input3_fep_min = 0.00491f;
    public static float input3_fep_max = 0.004914f;
    public static float input3_scnp_min = 0.0001104f;
    public static float input3_scnp_max = 0.0001114f;
    public static float input3_kci_min = 156.370f;
    public static float input3_kci_max = 165.294f;
    public static float input3_kc_min = 141.155f;
    public static float input3_kc_max = 175.162f;
    public static float input4_fe = 0.0025f;
    public static float input4_scn = 0.0002f;
    public static float input4_guang_min = 0.153f;
    public static float input4_guang_max = 0.175f;
    public static float input4_guangb_min = 0.238f;
    public static float input4_guangb_max = 0.281f;
    public static float input4_fescnp_min = 0.0000476f;
    public static float input4_fescnp_max = 0.0000562f;
    public static float input4_fep_min = 0.002444f;
    public static float input4_fep_max = 0.002452f;
    public static float input4_scnp_min = 0.0001438f;
    public static float input4_scnp_max = 0.0001524f;
    public static float input4_kci_min = 127.380f;
    public static float input4_kci_max = 159.910f;
    public static float marks_per_data = 2;
    public static float marks_per_operation = 10;

    private void Start()
    {
        AllDataDefaults = new string[4][] { AllDefaults1,AllDefaults2,AllDefaults3,AllDefaults4 };
        input = new TMP_InputField[4][] {input1,input2,input3,input4 };
        OperationDefaults = new List<string>();
        DataDefaults = new List<string>();
        OperationNum = 0;
        DataNum = 0;
        OperationMarks = 100;
        DataMarks = 100;
        TotalMarks = 0;
        AllOperationDefaultsFlag = new bool[AllOperationDefaults.Length];
        for(int i = 0;i< AllOperationDefaults.Length;i++)
        {
            AllOperationDefaultsFlag[i] = false;
        }
    }

    private void Update()
    {
        Debug.Log(DataDefaults.Count);
    }
    public void ComputeMarks()
    {
        float inputvalue;
        for(int i = 0;i<4;i++)
            for(int m = 0; m < input[i].Length; m++)
            {
                if (input[i][m]!=null)
                {
                    if (float.TryParse(input[i][m].text,out inputvalue))
                    {
                        if(i == 0)
                        {
                            if (m == 0 && inputvalue == input1_fe)
                                continue;
                            else if (m == 1 && inputvalue == input1_scn)
                                continue;
                            else if (m == 2 && (inputvalue >= input1_guang_min && inputvalue <= input1_guang_max))
                                continue;
                            else if (m == 3 && inputvalue == input1_guangb)
                                continue;
                            else if (m == 4 && inputvalue == input1_fescnp)
                                continue;
                            else if (m == 5 && inputvalue == input1_fep)
                                continue;
                            else if (m == 6 && inputvalue == input1_scnp)
                                continue;
                            else
                            {
                                ReduceDataMarks(marks_per_data);
                                AddDataDefalt(AllDataDefaults[i][m]);
                            }
                        }
                        else if(i == 1)
                        {
                            if (m == 0 && inputvalue == input2_fe)
                                continue;
                            else if (m == 1 && inputvalue == input2_scn)
                                continue;
                            else if (m == 2 && (inputvalue >= input2_guang_min && inputvalue <= input2_guang_max))
                                continue;
                            else if (m == 3 && (inputvalue >= input2_guangb_min && inputvalue <= input2_guangb_max))
                                continue;
                            else if (m == 4 && (inputvalue >= input2_fescnp_min && inputvalue <= input2_fescnp_max))
                                continue;
                            else if (m == 5 && (inputvalue >= input2_fep_min && inputvalue <= input2_fep_max))
                                continue;
                            else if (m == 6 && (inputvalue >= input2_scnp_min && inputvalue <= input2_scnp_max))
                                continue;
                            else if (m == 7 && (inputvalue >= input2_kci_min && inputvalue <= input2_kci_max))
                                continue;
                            else
                            {
                                ReduceDataMarks(marks_per_data);
                                AddDataDefalt(AllDataDefaults[i][m]);
                            }
                        }
                        else if (i == 2)
                        {
                            if (m == 0 && inputvalue == input3_fe)
                                continue;
                            else if (m == 1 && inputvalue == input3_scn)
                                continue;
                            else if (m == 2 && (inputvalue >= input3_guang_min && inputvalue <= input3_guang_max))
                                continue;
                            else if (m == 3 && (inputvalue >= input3_guangb_min && inputvalue <= input3_guangb_max))
                                continue;
                            else if (m == 4 && (inputvalue >= input3_fescnp_min && inputvalue <= input3_fescnp_max))
                                continue;
                            else if (m == 5 && (inputvalue >= input3_fep_min && inputvalue <= input3_fep_max))
                                continue;
                            else if (m == 6 && (inputvalue >= input3_scnp_min && inputvalue <= input3_scnp_max))
                                continue;
                            else if (m == 7 && (inputvalue >= input3_kci_min && inputvalue <= input3_kci_max))
                                continue;
                            else if (m == 8 && (inputvalue >= input3_kc_min && inputvalue <= input3_kc_max))
                                continue;
                            else
                            {
                                ReduceDataMarks(marks_per_data);
                                AddDataDefalt(AllDataDefaults[i][m]);
                            }
                        }
                        else if(i == 3)
                        {
                            if (m == 0 && inputvalue == input4_fe)
                                continue;
                            else if (m == 1 && inputvalue == input4_scn)
                                continue;
                            else if (m == 2 && (inputvalue >= input4_guang_min && inputvalue <= input4_guang_max))
                                continue;
                            else if (m == 3 && (inputvalue >= input4_guangb_min && inputvalue <= input4_guangb_max))
                                continue;
                            else if (m == 4 && (inputvalue >= input4_fescnp_min && inputvalue <= input4_fescnp_max))
                                continue;
                            else if (m == 5 && (inputvalue >= input4_fep_min && inputvalue <= input4_fep_max))
                                continue;
                            else if (m == 6 && (inputvalue >= input4_scnp_min && inputvalue <= input4_scnp_max))
                                continue;
                            else if (m == 7 && (inputvalue >= input4_kci_min && inputvalue <= input4_kci_max))
                                continue;
                            else
                            {
                                ReduceDataMarks(marks_per_data);
                                AddDataDefalt(AllDataDefaults[i][m]);
                            }
                        }
                    }
                    else
                    {
                        ReduceDataMarks(marks_per_data);
                        AddDataDefalt(AllDataDefaults[i][m]);
                    }
                    
                }
                else
                {
                    ReduceDataMarks(marks_per_data);
                    AddDataDefalt(AllDataDefaults[i][m]);
                }
            }
    }

    public void EndOperationResult()
    {
        ArrangeOperationDefaults();
        StringBuilder sb = new StringBuilder();
        int j = 1;
        sb.Append("扣分原因：");
        if (OperationDefaults.Count == 0)
            sb.Append("    无");
        else
            foreach (string st in OperationDefaults)
            {
                if (sb.Length > 0)
                {
                    sb.Append("\n");
                }

                sb.Append(j.ToString() + "." + st);
                j++;
            }
        OperationDefaultResult.text = sb.ToString();
        if (OperationMarks < 0)
            OperationMarks = 0;
        TotalMarks += OperationMarks * OperationProportion;
        TotalResult.text = TotalMarks.ToString() + "分";
        OperationResult.text = OperationMarks.ToString() + "分";
    }

    private void ArrangeOperationDefaults()
    {
        for(int i = 0;i < AllOperationDefaults.Length;i++)
        {
            if (AllOperationDefaultsFlag[i])
            {
                OperationMarks -= marks_per_operation;
                AddOperationDefalt(AllOperationDefaults[i]);
            }
        }
    }

    public void EndDataResult()
    {
        StringBuilder sb = new StringBuilder();
        int j = 1;
        sb.Append("扣分原因：");
        if (DataDefaults.Count == 0)
            sb.Append("    无");
        else
            foreach (string st in DataDefaults)
            {
                if (sb.Length > 0)
                {
                    sb.Append("\n");
                }

                sb.Append(j.ToString() + "." + st);
                j++;
            }
        DataDefaultResult.text = sb.ToString();
        if (DataMarks < 0)
            DataMarks = 0;
        TotalMarks += DataMarks * DataProportion;
        TotalResult.text = TotalMarks.ToString() + "分";
        DataResult.text = DataMarks.ToString() + "分";
    }
    public void AddOperationDefalt(string defaults)
    {
        OperationDefaults.Add(defaults);
    }
    public void AddDataDefalt(string defaults)
    {
        DataDefaults.Add(defaults);
    }

    public void ReduceOperationMarks(float marks)
    {
        OperationMarks -= marks;
    }
    public void ReduceDataMarks(float marks)
    {
        DataMarks -= marks;
    }
    public void ChangeAllOperationDefaltsFlag(int val)
    {
        AllOperationDefaultsFlag[val] = true;
    }

}
