using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Slider3Controller : MonoBehaviour
{
    public GameObject panel;
    public Slider slider1;
    public Slider slider2;
    public Slider slider3;
    Text valueText1;
    Text valueText2;
    Text valueText3;
    public Text titleText;
    public static Slider3Controller instance;

    private void Start()
    {
        instance = this;
        valueText1 = slider1.GetComponentInChildren<Text>();
        valueText2 = slider2.GetComponentInChildren<Text>();
        valueText3 = slider3.GetComponentInChildren<Text>();
    }
    public void SetSliderFunction(string title, UnityAction<Vector3> action, Vector3 min, Vector3 max, Vector3 currentValue)
    {
        ResetSlider(slider1, valueText1, min.x, max.x, currentValue.x);
        ResetSlider(slider2, valueText2, min.y, max.y, currentValue.y);
        ResetSlider(slider3, valueText3, min.z, max.z, currentValue.z);

        slider1.onValueChanged.AddListener((v) =>
        {
            currentValue.x = v;
            action(currentValue);
            valueText1.text = v.ToString();
        });
        slider2.onValueChanged.AddListener((v) =>
        {
            currentValue.y = v;
            action(currentValue);
            valueText2.text = v.ToString();
        });
        slider3.onValueChanged.AddListener((v) =>
        {
            currentValue.z = v;
            action(currentValue);
            valueText3.text = v.ToString();
        });

        panel.SetActive(true);
    }
    public void SetSliderFunction(string title, UnityAction<Color> action, Vector3 min, Vector3 max, Color currentValue)
    {
        ResetSlider(slider1, valueText1, min.x, max.x, currentValue.r);
        ResetSlider(slider2, valueText2, min.y, max.y, currentValue.g);
        ResetSlider(slider3, valueText3, min.z, max.z, currentValue.b);

        slider1.onValueChanged.AddListener((v) =>
        {
            currentValue.r = v;
            action(currentValue);
            valueText1.text = v.ToString();
        });
        slider2.onValueChanged.AddListener((v) =>
        {
            currentValue.g = v;
            action(currentValue);
            valueText2.text = v.ToString();
        });
        slider3.onValueChanged.AddListener((v) =>
        {
            currentValue.b = v;
            action(currentValue);
            valueText3.text = v.ToString();
        });
        titleText.text = title;
        panel.SetActive(true);
    }

    public void SetSliderFunction(string title, UnityAction<Vector2> action, Vector2 min, Vector2 max, Vector2 currentValue)
    {
        ResetSlider(slider1, valueText1, min.x, max.x, currentValue.x);
        ResetSlider(slider2, valueText2, min.y, max.y, currentValue.y);

        slider1.onValueChanged.AddListener((v) =>
        {
            currentValue.x = v;
            action(currentValue);
            valueText1.text = v.ToString();
        });
        slider2.onValueChanged.AddListener((v) =>
        {
            currentValue.y = v;
            action(currentValue);
            valueText2.text = v.ToString();
        });
        slider3.gameObject.SetActive(false);
        valueText3.gameObject.SetActive(false);
        titleText.text = title;
        panel.SetActive(true);
    }

    public void SetSliderFunction(string title, UnityAction<float> action, float min, float max, float currentValue)
    {
        ResetSlider(slider1, valueText1, min, max, currentValue);

        slider1.onValueChanged.AddListener((v) =>
        {
            action(v);
            valueText1.text = v.ToString();
        });

        slider2.gameObject.SetActive(false);
        valueText2.gameObject.SetActive(false);
        slider3.gameObject.SetActive(false);
        valueText3.gameObject.SetActive(false);
        titleText.text = title;
        panel.SetActive(true);
    }

    public void SetSliderFunction(string title, UnityAction<int> action, int min, int max, int currentValue)
    {
        ResetSlider(slider1, valueText1, min, max, currentValue);

        slider1.onValueChanged.AddListener((v) =>
        {
            action((int)v);
            valueText1.text = v.ToString();
        });
        slider1.wholeNumbers = true;
        slider2.gameObject.SetActive(false);
        valueText2.gameObject.SetActive(false);
        slider3.gameObject.SetActive(false);
        valueText3.gameObject.SetActive(false);
        titleText.text = title;
        panel.SetActive(true);
    }
    void ResetSlider(Slider slider, Text valueText, float min, float max, float currentValue)
    {
        slider.onValueChanged.RemoveAllListeners();

        slider.maxValue = max;
        slider.minValue = min;
        slider.wholeNumbers = false;
        slider.value = currentValue;
        valueText.text = currentValue.ToString();

        slider.gameObject.SetActive(true);
        valueText.gameObject.SetActive(true);
    }
}
