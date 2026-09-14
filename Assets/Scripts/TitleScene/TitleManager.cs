//-------------------------------------------------------
//
//  TitleManager.cs
//
//  概要
/// タイトルでUIをクリックした後の処理を行うスクリプト。
//
//  更新履歴
//
//  2026/06/28  作成
//  2026/07/04  ボタン選択時のSE再生を追加
//  2026/07/08  SE再生中は入力を受け付けないように変更
//  2026/09/15  mp4を実行時最初に読み込み始めるように変更
//
//-------------------------------------------------------
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class TitleManager : MonoBehaviour
{
    [Tooltip("オプションパネルのGameObject")]
    [SerializeField] private GameObject optionPanel;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource bgmAudioSource;

    [SerializeField] private AudioClip buttonSE;
    [SerializeField] private AudioClip TitleBGM;

    [SerializeField] private Fade fade;

    [SerializeField] private VideoPlayer videoPlayer;

    private bool _isTransitioning = false;  // 遷移中フラグ

    private void Start()
    {
        TitleVideoPlayer.Instance?.Play(() =>
        {
            fade.FadeIn(1.0f);
        });

        PlayTitleBGM();
    }


    public void OnClickGameStart()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
    //    TitleVideoPlayer.Instance?.Stop();
        fade.FadeOut(0.8f);

        StartCoroutine(LoadSceneAfterSE("GameScene"));
    }

    private IEnumerator LoadSceneAfterSE(string sceneName)
    {
        PlayButtonSE();
        yield return new WaitForSeconds(buttonSE.length);
        SceneManager.LoadScene(sceneName);
    }

    public void OnClickQuit()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        PlayButtonSE();
        Application.Quit();
    }

    public void OnClickOption()
    {
        if (_isTransitioning) return;
        PlayButtonSE();
        optionPanel.SetActive(true);
    }

    public void PlayButtonSE()
    {
        if (audioSource != null && buttonSE != null)
        {
            audioSource.PlayOneShot(buttonSE);
        }
    }
    private void PlayTitleBGM()
    {
        if (bgmAudioSource != null && TitleBGM != null)
        {
            bgmAudioSource.clip = TitleBGM;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
    }

    private void OnPrepareCompleted(VideoPlayer vp)
    {
        //動画の準備が完了してからフェードイン開始
        fade.FadeIn(1.0f);
        vp.Play();
    }
}
