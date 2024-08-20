using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class PieChartUpdater : MonoBehaviour
{
    public Image[] imagesPieChart;
    public GameObject pieChartGameObject;
    public Slider startSlider, endSlider;
    public Slider startTimeSlider, endTimeSlider;
    public TextMeshProUGUI startDateText, endDateText;
    public Button updateButton;
    private int currentMid = 1;
    private DateTime baseDate = new DateTime(2024, 1, 1);
    private int dateRange = 365; // 更改為365天範圍

    void Start()
    {
        pieChartGameObject.SetActive(true);

        startSlider.minValue = 0;
        startSlider.maxValue = dateRange - 1;
        endSlider.minValue = 0;
        endSlider.maxValue = dateRange - 1;
        startTimeSlider.minValue = 0;
        startTimeSlider.maxValue = 86399;
        endTimeSlider.minValue = 0;
        endTimeSlider.maxValue = 86399;

        startSlider.onValueChanged.AddListener(UpdateStartDate);
        startTimeSlider.onValueChanged.AddListener(UpdateStartTime);
        endSlider.onValueChanged.AddListener(UpdateEndDate);
        endTimeSlider.onValueChanged.AddListener(UpdateEndTime);

        updateButton.onClick.AddListener(() => FetchDataAndUpdateChartForMachine(currentMid, startDateText.text, endDateText.text));

        UpdateStartDate(startSlider.value);
        UpdateStartTime(startTimeSlider.value);
        UpdateEndDate(endSlider.value);
        UpdateEndTime(endTimeSlider.value);
    }

    void UpdateStartDate(float value)
    {
        DateTime selectedStartDate = baseDate.AddDays((int)value);
        UpdateDateTimeDisplay(startDateText, selectedStartDate, (int)startTimeSlider.value);
    }

    void UpdateStartTime(float seconds)
    {
        UpdateDateTimeDisplay(startDateText, baseDate.AddDays((int)startSlider.value), (int)seconds);
    }

    void UpdateEndDate(float value)
    {
        DateTime selectedEndDate = baseDate.AddDays((int)value);
        UpdateDateTimeDisplay(endDateText, selectedEndDate, (int)endTimeSlider.value);
    }

    void UpdateEndTime(float seconds)
    {
        UpdateDateTimeDisplay(endDateText, baseDate.AddDays((int)endSlider.value), (int)seconds);
    }

    void UpdateDateTimeDisplay(TextMeshProUGUI textMesh, DateTime date, int seconds)
    {
        DateTime time = date.Date.AddSeconds(seconds);
        textMesh.text = time.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void FetchDataAndUpdateChartForMachine(int mid, string startDate, string endDate)
    {
        Debug.Log($"Fetching data for MID: {mid}, Start Date: {startDate}, End Date: {endDate}");
        currentMid = mid;
        StartCoroutine(FetchDataAndUpdateChart(mid, startDate, endDate));
    }

    IEnumerator FetchDataAndUpdateChart(int mid, string startDate, string endDate)
    {
    string url = $"https://p9eqfsm35e.execute-api.us-east-1.amazonaws.com/a-20240001-smt/data?type=process&mid={mid}&start={Uri.EscapeDataString(startDate)}&end={Uri.EscapeDataString(endDate)}";
        Debug.Log($"Request URL: {url}");
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"JSON Response: {jsonResponse}");

            AWSResponse awsResponse = JsonUtility.FromJson<AWSResponse>(jsonResponse);
            Debug.Log($"Parsed Body: {awsResponse.body}");

            SignalCounts signalCounts = JsonUtility.FromJson<SignalCounts>(awsResponse.body);
            UpdateChart(signalCounts);
        }
        else
        {
            Debug.LogError("Failed to fetch data: " + request.error);
        }
    }

    void UpdateChart(SignalCounts signalCounts)
    {
        float total = signalCounts.red + signalCounts.yellow + signalCounts.green + signalCounts.black;

        if (total > 0)
        {
            float[] percentages = {
                signalCounts.green / total,
                signalCounts.yellow / total,
                signalCounts.red / total,
                signalCounts.black / total
            };
            float cumulativePercentage = 0f;

            int loopLimit = Mathf.Min(imagesPieChart.Length, percentages.Length);
            for (int i = 0; i < loopLimit; i++)
            {
                cumulativePercentage += percentages[i];
                imagesPieChart[i].fillAmount = cumulativePercentage;
            }
        }
    }

    public void TogglePieChart()
    {
        pieChartGameObject.SetActive(!pieChartGameObject.activeSelf);
        if (pieChartGameObject.activeSelf && currentMid != 0)
        {
            StartCoroutine(FetchDataAndUpdateChart(currentMid, startDateText.text, endDateText.text));
        }
    }

    [Serializable]
    public class SignalCounts
    {
        public float green;
        public float yellow;
        public float red;
        public float black;
    }

    [Serializable]
    public class AWSResponse
    {
        public string body;
    }
}
