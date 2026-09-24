//--------------------------------------
//
//  TitleVideoPlayer.cs
//
//  概要
//  タイトル動画をシーンをまたいで保持するシングルトン
//
//  更新履歴
//
//  2026/09/15  作成
//
//--------------------------------------
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class TitleVideoPlayer : MonoBehaviour
{
    public static TitleVideoPlayer Instance { get; private set; }

    [SerializeField] private VideoPlayer videoPlayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, "title.mp4");
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.Prepare();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private System.Action _onReady;

    private void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
        StartCoroutine(WaitFrames());
    }

    private IEnumerator WaitFrames()
    {
        yield return new WaitForSeconds(0.1f);  // 数フレーム待つ
        _onReady?.Invoke();
        _onReady = null;
    }

    private IEnumerator WaitAndInvoke()
    {
        yield return new WaitForEndOfFrame();  // 1フレーム待って動画が映ってからフェードイン
        _onReady?.Invoke();
        _onReady = null;
    }

    public void Play(System.Action onReady = null)
    {
        if (videoPlayer.isPrepared)
        {
            // 準備済みならそのまま再生
            videoPlayer.Play();
            onReady?.Invoke();
        }
        else
        {
            // 準備できていない場合だけPrepareする
            videoPlayer.Stop();
            videoPlayer.prepareCompleted -= OnPrepared;
            _onReady = onReady;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.Prepare();
        }
    }

    public void Stop()
    {
        videoPlayer.Stop();
    }

    public bool IsPrepared => videoPlayer.isPrepared;
}
