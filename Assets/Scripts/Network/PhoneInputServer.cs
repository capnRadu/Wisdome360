using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class PhoneInputServer : MonoBehaviour
{
    public TreeManager trees;

    const int Port = 8080;
    HttpListener listener;
    Thread thread;
    readonly ConcurrentQueue<string> presses = new ConcurrentQueue<string>();

    const string Html = @"<!doctype html><html><head>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<style>
 body{margin:0;min-height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:1.2rem;padding:1rem;background:#111;color:#fff;font-family:sans-serif;box-sizing:border-box}
 #msg{font-size:1.8rem;font-weight:bold;text-align:center}
 #grid{display:grid;grid-template-columns:1fr 1fr;gap:1rem;width:100%;max-width:28rem}
 button{font-size:2rem;border-radius:1rem;border:0;background:#2f855a;color:#fff;padding:2rem 0}
 button:active{background:#276749}
 button:disabled{background:#444;color:#999}
</style></head><body>
<div id='msg'>Plant a tree</div>
<div id='grid'>
 <button data-t='ash'>Ash</button>
 <button data-t='aspen'>Aspen</button>
 <button data-t='oak'>Oak</button>
 <button data-t='pine'>Pine</button>
</div>
<script>
var COOLDOWN = 10;
var msg = document.getElementById('msg');
var btns = document.querySelectorAll('button');
var endAt = 0;
try { endAt = parseInt(localStorage.getItem('endAt') || '0', 10); } catch (e) {}

function tick() {
  var left = Math.ceil((endAt - Date.now()) / 1000);
  if (left > 0) {
    msg.textContent = 'Plant again in ' + left + ' second' + (left === 1 ? '' : 's');
    btns.forEach(function (b) { b.disabled = true; });
    setTimeout(tick, 250);
  } else {
    msg.textContent = 'Plant a tree';
    btns.forEach(function (b) { b.disabled = false; });
  }
}

btns.forEach(function (b) {
  b.addEventListener('click', function () {
    if (Date.now() < endAt) return;
    fetch('/plant?type=' + b.dataset.t, { method: 'POST' });
    if (navigator.vibrate) navigator.vibrate(30);
    endAt = Date.now() + COOLDOWN * 1000;
    try { localStorage.setItem('endAt', endAt); } catch (e) {}
    tick();
  });
});

tick();
</script></body></html>";

    void Start()
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{Port}/");
        listener.Start();
        thread = new Thread(Listen) { IsBackground = true };
        thread.Start();
        Debug.Log("Server running on port " + Port);
    }

    void Listen()
    {
        while (listener.IsListening)
        {
            try
            {
                var ctx = listener.GetContext();
                if (ctx.Request.Url.AbsolutePath == "/plant")
                {
                    var type = ctx.Request.QueryString["type"];
                    if (!string.IsNullOrEmpty(type)) presses.Enqueue(type);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                }
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(Html);
                    ctx.Response.ContentType = "text/html";
                    ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    ctx.Response.Close();
                }
            }
            catch { }
        }
    }

    void Update()
    {
        while (presses.TryDequeue(out var type))
            trees.Plant(type);
    }

    void OnDestroy() { listener?.Stop(); }
}