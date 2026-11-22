using System;

namespace Glamaholic {
    internal enum PlateSlot : uint {
        MainHand = 0,
        OffHand = 1,
        Head = 2,
        Body = 3,
        Hands = 4,
        Legs = 5,
        Feet = 6,
        Ears = 7,
        Neck = 8,
        Wrists = 9,
        RightRing = 10,
        LeftRing = 11,
    }

    internal static class PlateSlotExt {
        internal static string Name(this PlateSlot slot) {
            return slot switch {
                PlateSlot.MainHand => "主手",
                PlateSlot.OffHand => "副手",
                PlateSlot.Head => "头部",
                PlateSlot.Body => "身体",
                PlateSlot.Hands => "手部",
                PlateSlot.Legs => "腿部",
                PlateSlot.Feet => "脚部",
                PlateSlot.Ears => "耳饰",
                PlateSlot.Neck => "项链",
                PlateSlot.Wrists => "手镯",
                PlateSlot.RightRing => "右戒",
                PlateSlot.LeftRing => "左戒",
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null),
            };
        }
    }
}
