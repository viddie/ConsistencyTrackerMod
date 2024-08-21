using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public static class Extensions {

        public static void AddDescription(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description) {
            TextMenuExt.EaseInSubHeaderExt descriptionText = new TextMenuExt.EaseInSubHeaderExt(description, false, containingMenu) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };

            subMenu.Add(descriptionText);

            subMenuItem.OnEnter += () => descriptionText.FadeVisible = true;
            subMenuItem.OnLeave += () => descriptionText.FadeVisible = false;
        }

        public static string ToReadableString(this AreaMode mode) {
            switch (mode) {
                case AreaMode.Normal:
                    return "A-Side";
                case AreaMode.BSide:
                    return "B-Side";
                case AreaMode.CSide:
                    return "C-Side";
                default:
                    return "wha-Side";
            }
        }

        public static JsonRectangle ToJsonRectangle(this Rectangle rect) {
            return new JsonRectangle {
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height
            };
        }

        public static JsonCircle ToJsonCircle(this Circle circle) {
            return new JsonCircle {
                X = circle.Position.X,
                Y = circle.Position.Y,
                Radius = circle.Radius
            };
        }

        public static JsonVector2 ToJsonVector2(this Vector2 vec) {
            return new JsonVector2 {
                X = vec.X,
                Y = vec.Y
            };
        }

        public static int ToCeilingFrames(this float seconds) {
            return (int)Math.Ceiling(seconds / Engine.RawDeltaTime / Engine.TimeRateB);
        }

        public static int ToFloorFrames(this float seconds) {
            return (int)Math.Floor(seconds / Engine.RawDeltaTime / Engine.TimeRateB);
        }

        public static string ToHex(this Color color) {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }
    }
    
    internal static class DynamicDataExtensions {
        private static readonly ConditionalWeakTable<object, DynamicData> cached = new ConditionalWeakTable<object, DynamicData>();

        public static DynamicData GetDynamicDataInstance(this object obj) {
            return cached.GetValue(obj, key => new DynamicData(key));
        }
    }
}
