using System;

// Summary: Flags enum for classifying items. An item can carry multiple tags.
// Used by Inventory.GetItems to filter by category.
[Flags]
public enum ItemTag
{
    None        = 0,
    KeyItem     = 1 << 0,
    Ingredient  = 1 << 1,
    Consumable  = 1 << 2,
    Throwable   = 1 << 3,
    Spell       = 1 << 4,
    Enemy       = 1 << 5,
}
