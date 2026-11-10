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
 body{margin:0;min-height:100vh;display:grid;grid-template-columns:1fr 1fr;gap:1rem;padding:1rem;background:#111;box-sizing:border-box}
 button{font-size:2rem;border-radius:1rem;border:0;background:#2f855a;color:#fff;padding:2rem 0}
 button:active{background:#276749}
 button:disabled{background:#444}
</style></head><body>
<button onclick=""plant(this,'ash')"">Ash</button>
<button onclick=""plant(this,'aspen')"">Aspen</button>
<button onclick=""plant(this,'oak')"">Oak</button>
<button onclick=""plant(this,'pine')"">Pine</button>
<script>
function plant(b,t){
  fetch('/plant?type='+t,{method:'POST'});
  navigator.vibrate&&navigator.vibrate(30);
  var all=document.querySelectorAll('button');
  all.forEach(x=>x.disabled=true);
  setTimeout(()=>all.forEach(x=>x.disabled=false),3000);
}
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