using System;
using MelonLoader;
using Ponder.Runtime;
using UnityEngine;

[assembly: MelonInfo(typeof(Ponder.Plugin), "Ponder", "1.0.0", "StriderDev")]
[assembly: MelonGame("CuriousOwlGames", "StellarDrive")]
[assembly: MelonAdditionalDependencies("StellarStriderLib")]
namespace Ponder
{
    public sealed class Plugin : MelonMod
    {
        private bool _ready;
        public override void OnInitializeMelon()
        {
            PonderRegistry.MainThread();
            PonderRegistry.Create("ponder", typeof(Plugin).Assembly);
            try
            {
                BuildMenuHooks.Install(HarmonyInstance);
                Application.focusChanged += GuideViewer.FocusChanged;
                _ready = true;
                LoggerInstance.Msg("Ponder ready. Hold Z for one second on a supported part in the build menu to view its guides.");
            }
            catch (Exception error)
            {
                HarmonyInstance.UnpatchSelf();
                LoggerInstance.Error("Ponder initialization failed: " + error);
            }
        }

        public override void OnUpdate()
        {
            if (!_ready)
            {
                return;
            }

            GuideViewer.Update();
            BuildMenuHooks.Update();
        }

        public override void OnLateUpdate()
        {
            if (_ready)
            {
                GuideViewer.LateUpdate();
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            GuideViewer.Close();
            BuildMenuHooks.Clear();
        }

        public override void OnDeinitializeMelon()
        {
            _ready = false;
            Application.focusChanged -= GuideViewer.FocusChanged;
            GuideViewer.Close();
            BuildMenuHooks.Clear();
            HarmonyInstance.UnpatchSelf();
        }
    }
}
