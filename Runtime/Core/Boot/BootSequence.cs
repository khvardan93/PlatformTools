using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlatformTools.Boot
{
    public interface IBootStep
    {
        /// Lower runs first.
        int Order { get; }
        /// Share of the progress bar, relative to the other steps.
        float Weight { get; }
        /// Text or localization key for the loading screen.
        string Description { get; }
        UniTask RunAsync(IProgress<float> progress, CancellationToken ct);
    }

    public enum BootState
    {
        Idle,
        Running,
        Completed,
        Failed
    }

    public sealed class BootSequence
    {
        const float PlatformInitWeight = 0.1f;

        readonly IPlatformService _platform;
        readonly IReadOnlyList<IBootStep> _steps;
        bool _gameReadySent;

        public BootSequence(IPlatformService platform, List<IBootStep> steps)
        {
            _platform = platform;
            _steps = (steps ?? new List<IBootStep>()).OrderBy(s => s.Order).ToList();
        }

        public BootState State { get; private set; }
        public float Progress { get; private set; }
        public string CurrentStep { get; private set; }
        public Exception Error { get; private set; }

        public event Action<BootState> StateChanged;
        public event Action<float> ProgressChanged;
        public event Action<string> StepStarted;

        /// Safe to call again after Failed (retry). Steps should tolerate running twice.
        public async UniTask RunAsync(CancellationToken ct = default)
        {
            if (State == BootState.Running || State == BootState.Completed) return;

            Error = null;
            SetState(BootState.Running);
            SetProgress(0f);

            var total = PlatformInitWeight + _steps.Sum(s => Mathf.Max(0f, s.Weight));
            var done = 0f;

            try
            {
                if (!_platform.IsInitialized)
                {
                    BeginStep("boot_platform");
                    await _platform.InitAsync();
                }
                done += PlatformInitWeight;
                SetProgress(done / total);

                foreach (var step in _steps)
                {
                    ct.ThrowIfCancellationRequested();
                    BeginStep(step.Description);

                    var weight = Mathf.Max(0f, step.Weight);
                    var start = done;
                    var progress = Cysharp.Threading.Tasks.Progress.Create<float>(
                        p => SetProgress((start + Mathf.Clamp01(p) * weight) / total));

                    await step.RunAsync(progress, ct);

                    done += weight;
                    SetProgress(done / total);
                }

                SetProgress(1f);
                SetState(BootState.Completed);
            }
            catch (OperationCanceledException)
            {
                SetState(BootState.Idle);
                throw;
            }
            catch (Exception e)
            {
                Error = e;
                Debug.LogException(e);
                SetState(BootState.Failed);
            }
        }

        /// Call once the main menu is visible and interactive. Forwards to the platform only once.
        public void NotifyGameReady()
        {
            if (_gameReadySent) return;
            _gameReadySent = true;
            _platform.GameReady();
        }

        private void BeginStep(string description)
        {
            CurrentStep = description;
            StepStarted?.Invoke(description);
        }

        private void SetProgress(float value)
        {
            if (value < Progress && State == BootState.Running && value > 0f) return; // never go backwards
            Progress = value;
            _platform.SetLoadingProgress(value);
            ProgressChanged?.Invoke(value);
        }

        private void SetState(BootState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}