using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;

namespace HeliCam
{
    public class Minimap
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float LeftX { get; set; }
        public float BottomY { get; set; }
        public float RightX { get; set; }
        public float TopY { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float UnitX { get; set; }
        public float UnitY { get; set; }
    }

    public class MinimapAnchor : BaseScript
    {
        public static Minimap GetMinimapAnchor()
        {
            float SafezoneScale = API.GetSafeZoneSize();
            float SafezoneX = 1.0f / 20.0f;
            float SafezoneY = 1.0f / 20.0f;

            float AspectRatio = API.GetAspectRatio(false);
            if (AspectRatio > 2) AspectRatio = 16f / 9f;

            int ResolutionX = 1920;
            int ResolutionY = 1080;
            API.GetActiveScreenResolution(ref ResolutionX, ref ResolutionY);

            float ScaleX = 1.0f / ResolutionX;
            float ScaleY = 1.0f / ResolutionY;

            Minimap anchor = new Minimap
            {
                Width = ScaleX * (ResolutionX / (4 * AspectRatio)),
                Height = ScaleY * (ResolutionY / 5.674f),

                LeftX = ScaleX * (ResolutionX * (SafezoneX * ((Math.Abs(SafezoneScale - 1)) * 10)))
            };

            if (AspectRatio > 2)
            {
                anchor.LeftX += anchor.Width * 0.845f;
                anchor.Width *= 0.76f;
            }
            else if (AspectRatio > 1.8f)
            {
                anchor.LeftX += anchor.Width * 0.2225f;
                anchor.Width *= 0.995f;
            }

            anchor.BottomY = 1 - ScaleY * (ResolutionY * (SafezoneY * ((Math.Abs(SafezoneScale - 1)) * 10)));
            anchor.RightX = anchor.LeftX + anchor.Width;
            anchor.TopY = anchor.BottomY - anchor.Height;
            anchor.X = anchor.LeftX;
            anchor.Y = anchor.TopY;
            anchor.UnitX = ScaleX;
            anchor.UnitY = ScaleY;

            return anchor;
        }
    }
}
