using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using MelonLoader;
using UnityEngine;

namespace BackpackMod
{
    /// <summary>
    /// Handles drawing the backpack GUI and interaction with the player's vac inventory.
    /// </summary>
    public static class BackpackUI
    {
        // ---- Configuration ----
        public const int SlotCount = 20;
        public const int Columns = 5;
        public const KeyCode ToggleKey = KeyCode.B;

        // ---- State ----
        public static bool IsOpen { get; private set; }
        public static List<BackpackSlot> Slots { get; private set; } = new();

        private static Vector2 _scrollPos;
        private static string _statusMessage = "";
        private static float _statusTimer;

        // ---- Initialization ----
        public static void Init()
        {
            Slots.Clear();
            for (int i = 0; i < SlotCount; i++)
                Slots.Add(new BackpackSlot());

            IsOpen = false;
            Melon<BackpackMelonMod>.Logger.Msg($"Backpack initialized with {SlotCount} slots.");
        }

        // ---- Called every frame (from MelonMod.OnUpdate) ----
        public static void OnUpdate()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                Toggle();
            }

            if (_statusTimer > 0f)
                _statusTimer -= Time.deltaTime;
        }

        public static void Toggle()
        {
            IsOpen = !IsOpen;

            // Lock / unlock cursor so the player can click the UI
            if (IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Pause the game's time so slimes don't run away while browsing
                Time.timeScale = 0f;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f;
            }
        }

        // ---- Called from MelonMod.OnGUI ----
        public static void DrawGUI()
        {
            if (!IsOpen) return;

            float winW = 520f;
            float winH = 460f;
            float x = (Screen.width - winW) / 2f;
            float y = (Screen.height - winH) / 2f;

            GUI.Box(new Rect(x, y, winW, winH), "");
            GUILayout.BeginArea(new Rect(x, y, winW, winH));
            GUILayout.Space(5);

            // ---- Title bar ----
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=20><b>🎒  Backpack</b></size>");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(30)))
                Toggle();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // ---- Action buttons ----
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Deposit All from Vac ➜ Backpack"))
                DepositAll();
            if (GUILayout.Button("Withdraw All ➜ Vac"))
                WithdrawAll();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // ---- Slot grid ----
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            int col = 0;
            GUILayout.BeginHorizontal();

            foreach (var slot in Slots)
            {
                DrawSlot(slot);
                col++;
                if (col >= Columns)
                {
                    col = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            // ---- Status message ----
            if (_statusTimer > 0f && !string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Space(4);
                GUILayout.Label($"<color=yellow>{_statusMessage}</color>");
            }

            GUILayout.EndArea();
        }

        // ---- Draw a single slot ----
        private static void DrawSlot(BackpackSlot slot)
        {
            GUILayout.BeginVertical("box", GUILayout.Width(90), GUILayout.Height(80));

            if (slot.IsEmpty)
            {
                GUILayout.Label("<color=#888>Empty</color>", GUILayout.Height(40));
            }
            else
            {
                GUILayout.Label($"<b>{slot.DisplayName}</b>\nx{slot.Count}", GUILayout.Height(40));
                if (GUILayout.Button("Take 1"))
                    WithdrawFromSlot(slot, 1);
            }

            GUILayout.EndVertical();
        }

        // ============================================================
        //  Inventory bridge helpers
        //  These use the SR2 Ammo (vac-pack inventory) API.
        // ============================================================

        /// <summary>
        /// Gets the player's current Ammo (vac-pack inventory) component.
        /// Returns null if the player object isn't available yet.
        /// </summary>
        private static Ammo GetPlayerAmmo()
        {
            try
            {
                // SRSingleton<SceneContext>.Instance.PlayerState.Ammo
                var sceneCtx = SRSingleton<SceneContext>.Instance;
                if (sceneCtx == null) return null;
                PlayerState playerState = sceneCtx.PlayerState;
                if (playerState == null) return null;
                return playerState.Ammo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deposit every item from the vac-pack into the backpack.
        /// </summary>
        public static void DepositAll()
        {
            var ammo = GetPlayerAmmo();
            if (ammo == null)
            {
                ShowStatus("Cannot access inventory right now.");
                return;
            }

            int deposited = 0;

            // Iterate over every vac slot
            for (int i = 0; i < ammo.ammoModel.slots.Count; i++)
            {
                var slotData = ammo.ammoModel.slots[i];
                if (slotData == null || slotData.count <= 0) continue;

                var id = slotData.id;
                string name = id.ToString();
                int count = slotData.count;

                int remaining = count;
                // Try to fit into existing or empty backpack slots
                foreach (var bpSlot in Slots)
                {
                    if (remaining <= 0) break;
                    remaining = bpSlot.TryAdd((int)id, name, remaining);
                }

                int stored = count - remaining;
                if (stored > 0)
                {
                    // Remove the stored amount from the vac
                    ammo.DecrementSlot(i, stored);
                    deposited += stored;
                }
            }

            ShowStatus(deposited > 0 ? $"Deposited {deposited} item(s)." : "Nothing to deposit or backpack full.");
        }

        /// <summary>
        /// Withdraw everything from the backpack into the vac.
        /// </summary>
        public static void WithdrawAll()
        {
            var ammo = GetPlayerAmmo();
            if (ammo == null)
            {
                ShowStatus("Cannot access inventory right now.");
                return;
            }

            int withdrawn = 0;

            foreach (var slot in Slots)
            {
                if (slot.IsEmpty) continue;

                var id = (Identifiable.Id)slot.ItemId;

                // MaybeAddToSlot returns true if it could fit
                bool added = ammo.MaybeAddToSlot(id, null);
                if (added)
                {
                    // One at a time since the API adds one per call
                    slot.Withdraw(1);
                    withdrawn++;
                }
            }

            ShowStatus(withdrawn > 0 ? $"Withdrew {withdrawn} item(s)." : "Vac is full or backpack empty.");
        }

        /// <summary>
        /// Withdraw a specific amount from one backpack slot into the vac.
        /// </summary>
        public static void WithdrawFromSlot(BackpackSlot slot, int amount)
        {
            var ammo = GetPlayerAmmo();
            if (ammo == null)
            {
                ShowStatus("Cannot access inventory right now.");
                return;
            }

            int withdrawn = 0;
            for (int i = 0; i < amount && !slot.IsEmpty; i++)
            {
                var id = (Identifiable.Id)slot.ItemId;
                bool added = ammo.MaybeAddToSlot(id, null);
                if (!added) break;
                slot.Withdraw(1);
                withdrawn++;
            }

            ShowStatus(withdrawn > 0 ? $"Took {withdrawn}." : "Vac is full!");
        }

        private static void ShowStatus(string msg)
        {
            _statusMessage = msg;
            _statusTimer = 3f;
        }
    }
}
