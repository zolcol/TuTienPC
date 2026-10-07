using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.UI
{
    public interface IUIModal
    {
        bool IsOpen { get; }
        void Close();
    }

    public class UIModalManager : MonoBehaviour
    {
        public static UIModalManager Instance { get; private set; }

        private static readonly List<IUIModal> activeModals = new List<IUIModal>(8);
        public static event Action<bool> OnModalStateChanged;

        public static bool IsAnyModalOpen => activeModals.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            Instance = null;
            activeModals.Clear();
            OnModalStateChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureInstance();
        }

        public static void EnsureInstance()
        {
            if (Instance != null) return;
            var existing = FindObjectOfType<UIModalManager>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }
            var go = new GameObject("[UIModalManager]");
            Instance = go.AddComponent<UIModalManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseTopModal();
            }
            else if (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
            {
                if (IsAnyModalOpen)
                {
                    CloseTopModal();
                }
            }
        }

        public static void PushModal(IUIModal modal)
        {
            if (modal == null) return;
            EnsureInstance();
            if (!activeModals.Contains(modal))
            {
                activeModals.Add(modal);
                if (activeModals.Count == 1)
                {
                    OnModalStateChanged?.Invoke(true);
                }
            }
        }

        public static void PopModal(IUIModal modal)
        {
            if (modal == null) return;
            if (activeModals.Remove(modal))
            {
                if (activeModals.Count == 0)
                {
                    OnModalStateChanged?.Invoke(false);
                }
            }
        }

        public static bool CloseTopModal()
        {
            for (int i = activeModals.Count - 1; i >= 0; i--)
            {
                var modal = activeModals[i];
                if (modal != null && modal.IsOpen)
                {
                    modal.Close();
                    return true;
                }
                activeModals.RemoveAt(i);
            }
            return false;
        }
    }
}
