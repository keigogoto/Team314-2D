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
        StartCoroutine(WaitFrames(_onReady));
        _onReady = null;
    }

    private IEnumerator WaitFrames(System.Action onReady)
    {
        yield return new WaitForSeconds(0.1f);
        onReady?.Invoke();
    }

    public void Play(System.Action onReady = null)
    {
        if (videoPlayer.isPrepared)
        {
            videoPlayer.Play();
            StartCoroutine(WaitFrames(onReady));
        }
        else
        {
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
