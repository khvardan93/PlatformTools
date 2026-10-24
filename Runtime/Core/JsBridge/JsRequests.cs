using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using Cysharp.Threading.Tasks;

namespace PlatformTools.JsBridge
{
    public readonly struct JsResult
    {
        public readonly bool Ok;
        public readonly string Data;
        public JsResult(bool ok, string data) { Ok = ok; Data = data; }
        public static readonly JsResult Timeout = new JsResult(false, "timeout");
    }

    /// Shared request/response plumbing for all jslib bridges.
    public static class JsRequests
    {
        public delegate void Callback(int id, int ok, IntPtr data);

        static int _nextId;
        static readonly Dictionary<int, UniTaskCompletionSource<JsResult>> Pending =
            new Dictionary<int, UniTaskCompletionSource<JsResult>>();

        public static (int id, UniTask<JsResult> task) Create()
        {
            var tcs = new UniTaskCompletionSource<JsResult>();
            var id = ++_nextId;
            Pending[id] = tcs;
            return (id, tcs.Task);
        }

        /// Pass this to every DllImport that takes a callback.
        [MonoPInvokeCallback(typeof(Callback))]
        public static void OnResult(int id, int ok, IntPtr data)
        {
            var str = data == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(data);
            if (Pending.Remove(id, out var tcs))
                tcs.TrySetResult(new JsResult(ok == 1, str));
        }

        /// Realtime delay, so it still runs while the game is paused for an ad.
        public static async UniTask<JsResult> WithTimeout(int id, UniTask<JsResult> task, float seconds)
        {
            var (finished, result) = await UniTask.WhenAny(
                task, UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.Realtime));

            if (finished) return result;

            Pending.Remove(id); // a late JS callback is ignored
            return JsResult.Timeout;
        }
    }
}