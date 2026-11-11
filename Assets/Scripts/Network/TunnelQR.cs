using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

public class TunnelQR : MonoBehaviour
{
    public Renderer qrQuad;

    // Use the full path if "cloudflared" isn't found:
    // @"C:\Program Files (x86)\cloudflared\cloudflared.exe"
    public string cloudflaredPath = "cloudflared";
    public int port = 8080;

    Process proc;
    volatile string tunnelUrl;
    bool shown;

    void Start()
    {
        var psi = new ProcessStartInfo
        {
            FileName = cloudflaredPath,
            Arguments = $"tunnel --url http://localhost:{port} --http-host-header localhost:{port}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        proc = new Process { StartInfo = psi };
        proc.OutputDataReceived += OnLine;
        proc.ErrorDataReceived += OnLine;
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
    }

    void OnLine(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;
        var m = Regex.Match(e.Data, @"https://[a-z0-9-]+\.trycloudflare\.com");
        if (m.Success) tunnelUrl = m.Value;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            qrQuad.gameObject.SetActive(!qrQuad.gameObject.activeSelf);

        if (shown || tunnelUrl == null) return;
        shown = true;
        UnityEngine.Debug.Log("Tunnel URL: " + tunnelUrl);
        qrQuad.material.mainTexture = MakeQR(tunnelUrl, 512);
    }

    Texture2D MakeQR(string text, int size)
    {
        var hints = new System.Collections.Generic.Dictionary<EncodeHintType, object>
    {
        { EncodeHintType.MARGIN, 2 }
    };
        var matrix = new MultiFormatWriter().encode(text, BarcodeFormat.QR_CODE, size, size, hints);

        var tex = new Texture2D(matrix.Width, matrix.Height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[matrix.Width * matrix.Height];
        for (int y = 0; y < matrix.Height; y++)
            for (int x = 0; x < matrix.Width; x++)
                // flip y: the matrix starts at the top, textures start at the bottom
                pixels[(matrix.Height - 1 - y) * matrix.Width + x] =
                    matrix[x, y] ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    void OnApplicationQuit() { Stop(); }
    void OnDestroy() { Stop(); }

    void Stop()
    {
        try { if (proc != null && !proc.HasExited) proc.Kill(); } catch { }
    }
}