using System;

namespace BackpackMod
{
    /// <summary>
    /// Represents a single slot in the backpack inventory.
    /// </summary>
    public class BackpackSlot
    {
        /// <summary>
        /// The identifier of the item stored in this slot (maps to Identifiable.Id).
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Display name shown in the UI.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Current stack count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Maximum items allowed per slot.
        /// </summary>
        public int MaxStack { get; set; }

        public BackpackSlot()
        {
            ItemId = 0;
            DisplayName = "Empty";
            Count = 0;
            MaxStack = 50;
        }

        public bool IsEmpty => Count <= 0 || ItemId == 0;

        /// <summary>
        /// Try to add amount to this slot. Returns the leftover that didn't fit.
        /// </summary>
        public int TryAdd(int itemId, string displayName, int amount)
        {
            if (IsEmpty)
            {
                ItemId = itemId;
                DisplayName = displayName;
            }

            if (ItemId != itemId)
                return amount; // wrong item type

            int canFit = MaxStack - Count;
            int toAdd = Math.Min(canFit, amount);
            Count += toAdd;
            return amount - toAdd;
        }

        /// <summary>
        /// Withdraw up to the requested amount. Returns actual amount withdrawn.
        /// </summary>
        public int Withdraw(int amount)
        {
            int toTake = Math.Min(Count, amount);
            Count -= toTake;
            if (Count <= 0)
            {
                ItemId = 0;
                DisplayName = "Empty";
                Count = 0;
            }
            return toTake;
        }
    }
}