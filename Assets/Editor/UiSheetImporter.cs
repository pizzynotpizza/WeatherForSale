using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Sets the import settings of the sliced UI sprite sheet (Assets/Art/UI/Sheet): Sprite (2D and UI),
    /// no mip maps, uncompressed with bilinear filtering (the pieces are small, so block compression showed
    /// as broken edges when they are scaled up), and 9-slice borders on the stretchable panels, buttons and bars.
    /// The pieces were cut from design/ref/Weather for Sale UI Sprite Sheet.png.
    /// </summary>
    public static class UiSheetImporter
    {
        private const string Folder = "Assets/Art/UI/Sheet";
        private const string LogoPath = "Assets/Art/UI/WeatherForSale_Logo.png";

        // Borders are (left, bottom, right, top) in pixels of the cut sprite.
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            // UI v2 (design/ref, 2026-11-28): honey wood (BtnWood*) and leaf green (BtnBlue*) pill buttons,
            // 760 px wide; only the round ends are fixed.
            { "BtnWoodA", new Vector4(100, 0, 100, 0) }, { "BtnWoodB", new Vector4(100, 0, 100, 0) }, { "BtnGrey", new Vector4(44, 30, 44, 30) },
            { "BtnBlueA", new Vector4(100, 0, 100, 0) }, { "BtnBlueB", new Vector4(100, 0, 100, 0) }, { "BtnBlueGrey", new Vector4(44, 30, 44, 30) },
            // UI v2 parchment panel, 1200 x 740: wood frame 78-82 px, 152 px at the top with the crest.
            { "PanelLarge", new Vector4(80, 75, 82, 152) }, { "PanelSmall", new Vector4(80, 75, 82, 152) },
            // The shop dialog keeps the first-sheet wood frame (the user's choice).
            { "DialogFrame", new Vector4(54, 54, 54, 54) },
            { "AchievementRow", new Vector4(40, 28, 40, 28) }, { "CapsuleWood", new Vector4(38, 30, 38, 30) },
            { "CapsuleSmall", new Vector4(30, 24, 30, 24) }, { "WoodBarLong", new Vector4(40, 24, 40, 24) },
            { "WoodBarSmall", new Vector4(30, 20, 30, 20) }, { "SliderTrack", new Vector4(30, 18, 30, 18) },
            { "PlayerFrame", new Vector4(70, 30, 70, 30) }, { "PlayerFillGreen", new Vector4(30, 20, 30, 20) }, { "PlayerEmpty", new Vector4(30, 20, 30, 20) },
        };

        // Pixels per unit above the default 100: the v2 buttons are drawn at half size so their round ends
        // (100 px) take 50 canvas units on a 100-120 unit tall button.
        private static readonly Dictionary<string, float> PixelsPerUnit = new Dictionary<string, float>
        {
            { "BtnWoodA", 200f }, { "BtnWoodB", 200f }, { "BtnBlueA", 200f }, { "BtnBlueB", 200f },
        };

        /// <summary>Menu entry: (re)applies the import settings.</summary>
        [MenuItem("Weather For Sale/Apply UI Sheet Import Settings")]
        public static void Apply()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                Borders.TryGetValue(name, out Vector4 border);
                Configure(path, border);
            }
            Configure(LogoPath, Vector4.zero);
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/UI/Boss" }))
            {
                string bossPath = AssetDatabase.GUIDToAssetPath(guid);
                Borders.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(bossPath), out Vector4 bossBorder);
                Configure(bossPath, bossBorder);
            }
            Configure("Assets/Art/UI/Button_Start.png", Vector4.zero);
            Configure("Assets/Art/UI/Button_Quit.png", Vector4.zero);
            foreach (string folder in new[] { "Assets/Art/UI/Maps", "Assets/Art/UI/Bottles" })
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                    Configure(AssetDatabase.GUIDToAssetPath(guid), Vector4.zero);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Configure(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = PixelsPerUnit.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(path), out float ppu) ? ppu : 100f;
            importer.spriteBorder = border;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }
}
