﻿using TheArchive.Core.Models;
using UnityEngine;

namespace TheArchive.Utilities
{
    public static class SColorExtensions
    {
        public static Color ToUnityColor(this SColor col)
        {
            return new Color
            {
                r = col.R,
                g = col.G,
                b = col.B,
                a = col.A,
            };
        }

        public static Color? ToUnityColor(this SColor? col)
        {
            if (col.HasValue)
                return col.Value.ToUnityColor();
            return null;
        }

        public static SColor ToSColor(this Color col)
        {
            return new SColor
            {
                R = col.r,
                G = col.g,
                B = col.b,
                A = col.a,
            };
        }

        public static SColor FromHexString(string col)
        {
            if(!col.StartsWith("#"))
                col = $"#{col}";

            if (ColorUtility.TryParseHtmlString(col, out var color))
            {
                return color.ToSColor();
            }
            return SColor.WHITE;
        }

        public static string ToHexString(this SColor col)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(col.ToUnityColor())}";
        }

        public static string ToShortHexString(this SColor col)
        {
            return $"#{ComponentToHex(col.R)}{ComponentToHex(col.G)}{ComponentToHex(col.B)}";
        }

        public static string ComponentToHex(float component)
        {
            return string.Format("{0:X1}", Mathf.Clamp((int)(component * 16f), 0, 16));
        }
    }
}
