//--------------------------------------
//
//  OptionUI.cs
//
//  概要
//  オプションパネルの表示・音量・マウスの速度調整
//
//  更新履歴
//
//  2026/07/04  作成
//  2026/09/15  BGMのスライダーを動かした時にSEと違い音を鳴らしていなかったのでSEの音を確認用に再生するように変更。
//
//--------------------------------------
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class OptionUI : MonoBehaviour
{
    [Header("オプションパネル")]
    [Tooltip("オプションパネルのゲームオブジェクト")]
    [SerializeField] private GameObject optionPanel;

    [Header("BGM設定")]
    [SerializeField] private Slider bgmSlider;

    [Header("SE設定")]
    [SerializeField] private Slider seSlider;

    [SerializeField]private TitleManager titleManager;

    private void Start()
    {
        optionPanel.SetActive(false);

        // まずリスナーを全部外す
        bgmSlider.onValueChanged.RemoveAllListeners();
        seSlider.onValueChanged.RemoveAllListeners();

        // リスナーなしの状態で値を設定
        bgmSlider.minValue = 0;
        bgmSlider.maxValue = 100;
        bgmSlider.wholeNumbers = true;
        bgmSlider.value = AudioManager.Instance.BgmVolume;

        seSlider.minValue = 0;
        seSlider.maxValue = 100;
        seSlider.wholeNumbers = true;
        seSlider.value = AudioManager.Instance.SeVolume;

        // 値設定後にリスナーを登録
        bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        seSlider.onValueChanged.AddListener(OnSeSliderChanged);

        // SEスライダーのPointerUp
        seSlider.gameObject.AddComponent<EventTrigger>();
        EventTrigger seTrigger = seSlider.GetComponent<EventTrigger>();
        EventTrigger.Entry seEntry = new EventTrigger.Entry();
        seEntry.eventID = EventTriggerType.PointerUp;
        seEntry.callback.AddListener((data) => titleManager?.PlayButtonSE());
        seTrigger.triggers.Add(seEntry);

        // BGMスライダーのPointerUp
        bgmSlider.gameObject.AddComponent<EventTrigger>();
        EventTrigger bgmTrigger = bgmSlider.GetComponent<EventTrigger>();
        EventTrigger.Entry bgmEntry = new EventTrigger.Entry();
        bgmEntry.eventID = EventTriggerType.PointerUp;
        bgmEntry.callback.AddListener((data) => titleManager?.PlayButtonSE());
        bgmTrigger.triggers.Add(bgmEntry);


    }

    // オプションボタンを押したら呼ぶ
    public void OpenOption()
    {
        titleManager.PlayButtonSE();
        optionPanel.SetActive(true);
    }

    // 閉じるボタンを押したら呼ぶ
    public void CloseOption()
    {
        titleManager.PlayButtonSE();
        optionPanel.SetActive(false);
    }

    private void OnBgmSliderChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value / 10f) * 10;  // 10刻みに丸める
        bgmSlider.value = intValue;
        AudioManager.Instance.SetBgmVolume(intValue);
    }

    private void OnSeSliderChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value / 10f) * 10;
        seSlider.value = intValue;
        AudioManager.Instance.SetSeVolume(intValue);
    }
}
