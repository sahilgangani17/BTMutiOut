using System;
using System.Collections.Generic;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace BTMultiOut
{
    /// <summary>
    /// Captures audio from a chosen source device (WASAPI loopback) and writes
    /// the same buffer to multiple selected output devices in parallel.
    ///
    /// FIX 1 – Latency  : buffer trimmed to 150 ms; WasapiOut latency 50 ms.
    ///                     A drain thread keeps the buffer from growing.
    /// FIX 2 – Double-play: the user picks an explicit capture source.
    ///                     The source device is tagged so the UI can warn if
    ///                     it is also selected as an output.
    /// </summary>
    public class MultiOutputEngine : IDisposable
    {
        // ── Capture ───────────────────────────────────────────────────────────
        private WasapiLoopbackCapture? _capture;
        private string?                _captureDeviceId;

        // ── Per-device output ─────────────────────────────────────────────────
        private class OutputTarget : IDisposable
        {
            public string DeviceId     { get; }
            public string FriendlyName { get; }

            private WasapiOut?            _out;
            private BufferedWaveProvider? _buf;
            private WaveFormat?           _fmt;
            private bool                  _ready;

            // Drain thread keeps buffer at ≤ TARGET_MS so latency stays low
            private Thread?              _drainThread;
            private volatile bool        _draining;
            private const int            TARGET_MS  = 80;   // target buffer depth
            private const int            LATENCY_MS = 50;   // WasapiOut latency

            public OutputTarget(string id, string name)
            { DeviceId = id; FriendlyName = name; }

            public void Init(WaveFormat captureFmt)
            {
                MMDevice? dev = FindDevice(DeviceId);
                if (dev == null) return;

                _fmt = captureFmt;   // WASAPI shared-mode accepts the capture format directly

                _buf = new BufferedWaveProvider(_fmt)
                {
                    DiscardOnBufferOverflow = true,
                    // Keep buffer small – only 150 ms worth
                    BufferDuration = TimeSpan.FromMilliseconds(150)
                };

                try
                {
                    _out = new WasapiOut(dev, AudioClientShareMode.Shared, true, LATENCY_MS);
                    _out.Init(_buf);
                    _out.Play();
                    _ready = true;

                    // Drain thread: if the buffer grows past TARGET_MS, drop the excess
                    _draining = true;
                    _drainThread = new Thread(DrainLoop)
                    { IsBackground = true, Name = $"Drain-{FriendlyName}" };
                    _drainThread.Start();
                }
                catch
                {
                    // Device rejected the format – skip silently
                    _out?.Dispose();
                    _out = null;
                    _ready = false;
                }
            }

            public void Write(byte[] data, int offset, int count, WaveFormat captureFmt)
            {
                if (!_ready || _buf == null || _fmt == null) return;
                try
                {
                    if (captureFmt.Equals(_fmt))
                    {
                        _buf.AddSamples(data, offset, count);
                    }
                    else
                    {
                        // Format differs (e.g. BT device uses different sample-rate) — convert
                        byte[] conv = Convert(data, offset, count, captureFmt, _fmt);
                        _buf.AddSamples(conv, 0, conv.Length);
                    }
                }
                catch { /* keep other devices alive */ }
            }

            // ── Drain loop ────────────────────────────────────────────────────
            private void DrainLoop()
            {
                while (_draining)
                {
                    try
                    {
                        if (_buf != null && _fmt != null)
                        {
                            // How many bytes correspond to TARGET_MS?
                            int targetBytes = (int)(_fmt.AverageBytesPerSecond * TARGET_MS / 1000.0);
                            int excess      = _buf.BufferedBytes - targetBytes;
                            if (excess > 0)
                            {
                                // Read & discard the excess — this trims latency buildup
                                var discard = new byte[excess];
                                _buf.Read(discard, 0, excess);
                            }
                        }
                    }
                    catch { /* ignore */ }
                    Thread.Sleep(20);
                }
            }

            // ── Helpers ───────────────────────────────────────────────────────

            private static MMDevice? FindDevice(string id)
            {
                using var e = new MMDeviceEnumerator();
                foreach (var d in e.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                    if (d.ID == id) return d;
                return null;
            }

            private static byte[] Convert(
                byte[] data, int offset, int count,
                WaveFormat src, WaveFormat dst)
            {
                using var raw       = new RawSourceWaveStream(data, offset, count, src);
                using var resampler = new MediaFoundationResampler(raw, dst) { ResamplerQuality = 60 };
                var mem = new System.IO.MemoryStream();
                var tmp = new byte[4096];
                int n;
                while ((n = resampler.Read(tmp, 0, tmp.Length)) > 0)
                    mem.Write(tmp, 0, n);
                return mem.ToArray();
            }

            public void Dispose()
            {
                _draining = false;
                _ready    = false;
                _out?.Stop();
                _out?.Dispose();
                _out = null;
            }
        }

        // ── Engine state ──────────────────────────────────────────────────────
        private readonly List<OutputTarget> _targets = new();
        public  bool IsRunning { get; private set; }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>All active render endpoints.</summary>
        public static List<(string Id, string Name)> GetOutputDevices()
        {
            var list = new List<(string, string)>();
            using var e = new MMDeviceEnumerator();
            foreach (var d in e.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                list.Add((d.ID, d.FriendlyName));
            return list;
        }

        /// <summary>
        /// Returns the ID of the current Windows default playback device —
        /// used so the UI can highlight it.
        /// </summary>
        public static (string Id, string Name) GetDefaultOutputDevice()
        {
            using var e = new MMDeviceEnumerator();
            var d = e.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return (d.ID, d.FriendlyName);
        }

        /// <summary>
        /// Start the engine.
        /// <paramref name="captureDeviceId"/> is the device whose loopback we capture.
        /// If null, the Windows default device is used.
        /// <paramref name="selectedDevices"/> are the devices audio is forwarded to.
        /// </summary>
        public void Start(
            string? captureDeviceId,
            IEnumerable<(string Id, string Name)> selectedDevices)
        {
            if (IsRunning) return;

            // Pick capture device
            MMDevice captureDev;
            using var enumerator = new MMDeviceEnumerator();
            if (captureDeviceId != null)
            {
                captureDev = null!;
                foreach (var d in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                    if (d.ID == captureDeviceId) { captureDev = d; break; }
                captureDev ??= enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            else
            {
                captureDev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }

            _captureDeviceId = captureDev.ID;
            _capture = new WasapiLoopbackCapture(captureDev);
            WaveFormat fmt = _capture.WaveFormat;

            foreach (var (id, name) in selectedDevices)
            {
                var t = new OutputTarget(id, name);
                t.Init(fmt);
                _targets.Add(t);
            }

            _capture.DataAvailable    += OnData;
            _capture.RecordingStopped += OnStopped;
            _capture.StartRecording();
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            _capture?.StopRecording();
        }

        // ── Events ────────────────────────────────────────────────────────────

        private void OnData(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0) return;
            var fmt = _capture!.WaveFormat;
            foreach (var t in _targets)
                t.Write(e.Buffer, 0, e.BytesRecorded, fmt);
        }

        private void OnStopped(object? sender, StoppedEventArgs e)
        {
            foreach (var t in _targets) t.Dispose();
            _targets.Clear();
            _capture?.Dispose();
            _capture = null;
        }

        public void Dispose()
        {
            Stop();
            Thread.Sleep(200);
        }
    }
}
