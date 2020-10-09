using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelProcess : MonoBehaviour
{
    public GameObject plane;
    public TextMeshProUGUI text;
    public TextMeshProUGUI numer;
    public Slider slider;

    private void Awake()
    {
        if(!plane.activeSelf)
        {
            plane.SetActive(true);
        }
    }

    public void setActive(bool v)
    {
        if(plane!=null)
        {
            plane.SetActive(v);
        }
        
    }

    public void setNumer(string s)
    {
        if (numer != null)
        {
            numer.text = s;
        }
    }

    public void setLabel(string s)
    {
        if (text != null)
        {
            text.text = s;
        }
    }

    public void setValue(float v)
    {
        if (slider != null)
        {
            slider.value = v;
        }
    }

}
