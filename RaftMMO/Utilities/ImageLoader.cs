using RaftMMO.ModEntry;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RaftMMO.Utilities
{
    public class ImageLoader
    {
        public static Sprite MayaImage { get; set; } = null;
        public static Sprite RouhiImage { get; set; } = null;


        private static List<Sprite> sprites = new List<Sprite>();
        private static List<Texture2D> textures = new List<Texture2D>();

        private static Sprite LoadImage(byte[] data)
        {
            try
            {
                Texture2D tex = new Texture2D(2, 2);
                textures.Add(tex);
                if (ImageConversion.LoadImage(tex, data))
                {
                    var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    sprites.Add(sprite);
                    return sprite;
                }
            }
            catch (System.Exception e)
            {
                RaftMMOLogger.LogWarning("ImageLoader.LoadImage caught exception: " + e);
            }

            return WhiteSprite();
        }

        private static Sprite WhiteSprite()
        {
            var tex = Texture2D.whiteTexture;
            var sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            sprites.Add(sprite);
            return sprite;
        }

        public static void Initialize()
        {
            MayaImage = LoadEmbeddedImage("maya.png");
            RouhiImage = LoadEmbeddedImage("rouhi.png");
        }

        public static Sprite LoadFileImage(string filepath)
        {
            try
            {
                if (File.Exists(filepath))
                {
                    return LoadImage(File.ReadAllBytes(filepath));
                }
            }
            catch (System.Exception e)
            {
                RaftMMOLogger.LogWarning("ImageLoader.LoadFileImage caught exception: " + e);
            }
            return WhiteSprite();
        }

        public static Sprite LoadEmbeddedImage(string name)
        {
            return LoadImage(CommonEntry.ModDataGetter.GetDataFile(name));
        }

        public static void Destroy()
        {
            sprites.ForEach(s => Object.Destroy(s));
            textures.ForEach(t => Object.Destroy(t));
            sprites.Clear();
            textures.Clear();
            MayaImage = null;
            RouhiImage = null;
        }
    }
}
