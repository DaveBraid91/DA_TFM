using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;

public class PerformanceStatsOverlay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("FPS settings")]
    [SerializeField] private float updateInterval = 1.0f;

    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _verticesRecorder;

    private float _accumTime;
    private int _frames;

    private void OnEnable()
    {
        _drawCallsRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render,
            "Draw Calls Count");      // métrica del módulo Render[web:312]

        _verticesRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render,
            "Vertices Count");        // la usamos para aproximar tris[web:312]

        _accumTime = 0f;
        _frames = 0;
    }

    private void OnDisable()
    {
        _drawCallsRecorder.Dispose();
        _verticesRecorder.Dispose();
    }

    private void Update()
    {
        if (!statsText) return;

        _accumTime += Time.unscaledDeltaTime;
        _frames++;

        if (_accumTime < updateInterval)
            return;

        var avgDeltaTime = _accumTime / _frames;
        var fps = 1f / Mathf.Max(avgDeltaTime, 0.0001f);
        var frameMs = avgDeltaTime * 1000f;

        var drawCalls = _drawCallsRecorder.Valid ? _drawCallsRecorder.LastValue : 0;
        var vertices  = _verticesRecorder.Valid  ? _verticesRecorder.LastValue  : 0;
        var trisApprox = vertices / 3;

        var sb = new StringBuilder(128);
        sb.AppendLine($"FPS: {fps:0.0}");
        sb.AppendLine($"Frame: {frameMs:0.0} ms");
        sb.AppendLine($"Draw Calls: {drawCalls}");
        sb.AppendLine($"Tris (approx): {trisApprox}");

        statsText.text = sb.ToString();

        _accumTime = 0f;
        _frames = 0;
    }
}