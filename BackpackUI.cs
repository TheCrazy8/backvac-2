using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Player;
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

        // Maps IdentifiableType.GetInstanceID() → IdentifiableType so items deposited
        // from the vac can be identified by their stored int key when withdrawn later.
        private static readonly Dictionary<int, IdentifiableType> _typeRegistry = new();

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
        /// Gets the player's current PlayerState.
        /// Returns null if the player object isn't available yet.
        /// </summary>
        private static PlayerState GetPlayerState()
        {
            try
            {
                var sceneCtx = SRSingleton<SceneContext>.Instance;
                if (sceneCtx == null) return null;
                PlayerState playerState = sceneCtx.PlayerState;
                return playerState;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds an unlocked vac slot that already holds <paramref name="identType"/>,
        /// or the first empty unlocked slot if none matches. Returns null if the vac is full.
        /// </summary>
        private static AmmoSlot FindAvailableVacSlot(IEnumerable<AmmoSlot> slots, IdentifiableType identType)
        {
            AmmoSlot emptySlot = null;
            foreach (var s in slots)
            {
                if (!s.IsUnlocked) continue;
                if (s.Id == identType) return s;
                if (s.Id == null && emptySlot == null) emptySlot = s;
            }
            return emptySlot;
        }

        /// <summary>
        /// Deposit every item from the vac-pack into the backpack.
        /// </summary>
        public static void DepositAll()
        {
            var player = GetPlayerState();
            if (player == null) { ShowStatus("Cannot access inventory right now."); return; }
            var ammo = player.Ammo;

            int deposited = 0;

            // Iterate over every vac slot
            for (int i = 0; i < ammo.Slots.Count; i++)
            {
                var slotData = ammo.Slots[i];
                if (slotData == null || slotData.Id == null || slotData.Count <= 0) continue;

                var identType = slotData.Id;
                int itemKey = identType.GetInstanceID();
                _typeRegistry[itemKey] = identType;

                string name = identType.name;
                int count = slotData.Count;

                int remaining = count;
                // Try to fit into existing or empty backpack slots
                foreach (var bpSlot in Slots)
                {
                    if (remaining <= 0) break;
                    remaining = bpSlot.TryAdd(itemKey, name, remaining);
                }

                int stored = count - remaining;
                if (stored > 0)
                {
                    // Remove the stored amount from the vac slot directly
                    slotData.Count -= stored;
                    if (slotData.Count <= 0)
                        slotData.Id = null;
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
            var player = GetPlayerState();
            if (player == null) { ShowStatus("Cannot access inventory right now."); return; }
            var ammo = player.Ammo;

            int withdrawn = 0;

            foreach (var bpSlot in Slots)
            {
                if (bpSlot.IsEmpty) continue;
                if (!_typeRegistry.TryGetValue(bpSlot.ItemId, out var identType)) continue;

                var vacSlot = FindAvailableVacSlot(ammo.Slots, identType);
                if (vacSlot == null) continue;

                int canFit = vacSlot.MaxCount - vacSlot.Count;
                if (canFit <= 0) continue;

                int toMove = Math.Min(bpSlot.Count, canFit);
                if (vacSlot.Id == null) vacSlot.Id = identType;
                vacSlot.Count += toMove;
                bpSlot.Withdraw(toMove);
                withdrawn += toMove;
            }

            ShowStatus(withdrawn > 0 ? $"Withdrew {withdrawn} item(s)." : "Vac is full or backpack empty.");
        }

        /// <summary>
        /// Withdraw a specific amount from one backpack slot into the vac.
        /// </summary>
        public static void WithdrawFromSlot(BackpackSlot slot, int amount)
        {
            var player = GetPlayerState();
            if (player == null) { ShowStatus("Cannot access inventory right now."); return; }
            var ammo = player.Ammo;

            if (!_typeRegistry.TryGetValue(slot.ItemId, out var identType))
            {
                ShowStatus("Cannot identify item type.");
                return;
            }

            var vacSlot = FindAvailableVacSlot(ammo.Slots, identType);

            if (vacSlot == null) { ShowStatus("Vac is full!"); return; }

            int canFit = vacSlot.MaxCount - vacSlot.Count;
            int toMove = Math.Min(Math.Min(amount, slot.Count), canFit);
            if (toMove <= 0) { ShowStatus("Vac is full!"); return; }

            if (vacSlot.Id == null) vacSlot.Id = identType;
            vacSlot.Count += toMove;
            slot.Withdraw(toMove);

            ShowStatus($"Took {toMove}.");
        }

        private static void ShowStatus(string msg)
        {
            _statusMessage = msg;
            _statusTimer = 3f;
        }
    }
}
