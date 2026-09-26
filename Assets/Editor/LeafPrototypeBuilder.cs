using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LeafGame.Editor
{
    public static class LeafPrototypeBuilder
    {
        public sealed class Core
        {
            public GameObject systems, tree, canvas;
            public Camera camera;
            public LeafController leaf;
            public WindManager wind;
            public AudioManager audio;
            public SpriteRenderer sky, ground;
            public SpriteRenderer[] foliage;
            public Text title, season, instructions;
            public Font font;
            public Sprite square, leafSprite, ring;
        }

        public static T Asset<T>(string path) where T : ScriptableObject
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value) return value;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }
        public static Sprite MakeSprite(string name, Func<int, int, Color> pixel, Vector2 pivot)
        {
            string path = "Assets/Art/Placeholders/" + name + ".png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++) texture.SetPixel(x, y, pixel(x, y));
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 128;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static AudioClip Tone(string name, float seconds, bool noise, float frequency)
        {
            string path = "Assets/Audio/Placeholders/" + name + ".wav";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                const int rate = 22050;
                int count = Mathf.CeilToInt(rate * seconds);
                var random = new System.Random(7);
                using (var writer = new BinaryWriter(File.Open(path, FileMode.Create)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                    writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                    float low = 0;
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / rate;
                        low = Mathf.Lerp(low, (float)random.NextDouble() * 2 - 1, 0.08f);
                        float signal = noise ? low : Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.38f + Mathf.Sin(2 * Mathf.PI * frequency * 1.5f * t) * 0.18f;
                        float envelope = seconds < 2 ? Mathf.Sin(Mathf.PI * t / seconds) * Mathf.Exp(-3f * t / seconds) : Mathf.Min(1, Mathf.Min(t, seconds - t));
                        writer.Write((short)(Mathf.Clamp(signal * envelope * 0.32f, -1, 1) * short.MaxValue));
                    }
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        public static GameObject Node(string name, Transform parent = null)
        {
            var go = new GameObject(name); if (parent) go.transform.SetParent(parent, false); return go;
        }
        public static SpriteRenderer SpriteNode(string name, Transform parent, Sprite sprite, Vector3 position, Vector3 scale, Color color, int order)
        {
            var go = Node(name, parent); go.transform.localPosition = position; go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; return renderer;
        }
        public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        public static Text Label(string name, Transform parent, Font font, string text, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var rect = Rect(name, parent, position, size); var label = rect.gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
            label.color = color; label.raycastTarget = false; label.supportRichText = true; return label;
        }
        public static GameObject SavePrefab(GameObject go, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        }
        private static AudioSource Source(Transform parent, string name, bool loop)
        {
            var s = Node(name, parent).AddComponent<AudioSource>(); s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0; return s;
        }
        public static WindDifficultyProfile SpringProfile()
        {
            var p = Asset<WindDifficultyProfile>("Assets/ScriptableObjects/Wind/Spring.asset");
            p.spawnInterval = 5.2f; p.arrowSpeed = 135; p.perfectWindow = 0.12f; p.goodWindow = 0.26f; p.windStrength = 15;
            EditorUtility.SetDirty(p); return p;
        }
        public static Core CreateCore()
        {
            var c = new Core();
            c.square = MakeSprite("PLACEHOLDER_Square", (x,y) => Color.white, new Vector2(0.5f, 0.5f));
            c.leafSprite = MakeSprite("PLACEHOLDER_PlayerLeaf", (x,y) =>
            {
                float u = x / 127f, v = (y - 64f) / 64f;
                float width = Mathf.Sin(Mathf.PI * Mathf.Clamp01((u - 0.12f) / 0.88f)) * 0.53f;
                if ((u < 0.2f && Mathf.Abs(v) < 0.025f) || (u >= 0.12f && Mathf.Abs(v) < width))
                    return Mathf.Abs(v) < 0.025f ? new Color(0.67f, 0.79f, 0.44f) : Color.white;
                return Color.clear;
            }, new Vector2(0, 0.5f));
            c.ring = MakeSprite("PLACEHOLDER_TimingRing", (x,y) => { float r = Vector2.Distance(new Vector2(x,y), new Vector2(63.5f,63.5f)); return r > 52 && r < 57 ? Color.white : Color.clear; }, new Vector2(0.5f,0.5f));
            var disc = MakeSprite("PLACEHOLDER_Foliage", (x,y) => Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f)) < 62 ? Color.white : Color.clear, new Vector2(0.5f,0.5f));
            c.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            c.systems = Node("LEAF — Systems");
            c.camera = Node("Main Camera").AddComponent<Camera>(); c.camera.tag = "MainCamera";
            c.camera.orthographic = true; c.camera.orthographicSize = 5.5f; c.camera.transform.position = new Vector3(0,0,-10);
            c.camera.clearFlags = CameraClearFlags.SolidColor; c.camera.backgroundColor = new Color(0.11f,0.18f,0.16f);
            c.camera.gameObject.AddComponent<AudioListener>();
            c.sky = SpriteNode("PLACEHOLDER_Background", null, c.square, new Vector3(0,-6,0), new Vector3(60,60,1), new Color(0.16f,0.27f,0.23f), -30);
            c.ground = SpriteNode("PLACEHOLDER_Ground", null, c.square, new Vector3(0,-17,0), new Vector3(50,4,1), new Color(0.15f,0.19f,0.12f), -10);
            c.tree = Node("Tree");
            SpriteNode("PLACEHOLDER_Tree_Main", c.tree.transform, c.square, new Vector3(-2,-4,0), new Vector3(1.5f,22,1), new Color(0.24f,0.2f,0.15f), 0);
            var right = SpriteNode("PLACEHOLDER_Branch_Right", c.tree.transform, c.square, new Vector3(0.7f,2.5f,0), new Vector3(5.2f,0.3f,1), new Color(0.32f,0.26f,0.18f), 1); right.transform.localEulerAngles = new Vector3(0,0,7);
            var left = SpriteNode("PLACEHOLDER_Branch_Left", c.tree.transform, c.square, new Vector3(-4,1.8f,0), new Vector3(3.6f,0.3f,1), right.color, 1); left.transform.localEulerAngles = new Vector3(0,0,-13);
            for (int i = 0; i < 5; i++)
            {
                var lower = SpriteNode("PLACEHOLDER_LowerBranch_" + i, c.tree.transform, c.square, new Vector3(i % 2 == 0 ? -0.4f : -3.9f,-2.5f-i*2.5f,0), new Vector3(4.8f-i*0.35f,0.24f,1), right.color, 0);
                lower.transform.localEulerAngles = new Vector3(0,0,i % 2 == 0 ? 14 : -12);
            }
            c.foliage = new SpriteRenderer[9];
            for (int i = 0; i < 9; i++) c.foliage[i] = SpriteNode("PLACEHOLDER_Foliage_" + i, c.tree.transform, disc, new Vector3(-6+i*1.6f,4.6f+Mathf.Sin(i*2)*0.65f,0), new Vector3(3.6f,2.2f,1), new Color(0.25f,0.42f,0.25f), -5);
            var leafGo = Node("PlayerLeaf"); leafGo.transform.position = new Vector3(3.05f,2.79f,0);
            c.leaf = leafGo.AddComponent<LeafController>();
            c.leaf.stemPivot = Node("StemPivot", leafGo.transform).transform;
            c.leaf.spriteRenderer = SpriteNode("PLACEHOLDER_PlayerLeaf", c.leaf.stemPivot, c.leafSprite, Vector3.zero, new Vector3(1.15f,1.15f,1), new Color(0.68f,0.82f,0.37f), 10);
            c.leaf.stemPivot.localEulerAngles = new Vector3(0,0,-90);
            c.audio = Node("AudioManager", c.systems.transform).AddComponent<AudioManager>();
            c.audio.library = Asset<AudioLibrary>("Assets/ScriptableObjects/Audio/AudioLibrary.asset");
            c.audio.library.gameplayMusic = Tone("PLACEHOLDER_CozyDrone", 12, false, 196);
            c.audio.library.placeholderFinalMusic = Tone("PLACEHOLDER_FinalDrone_NOT_Melon", 12, false, 146.83f);
            c.audio.library.ambient = Tone("PLACEHOLDER_WindBed", 8, true, 0);
            c.audio.library.rain = Tone("PLACEHOLDER_Rain", 8, true, 0);
            c.audio.library.sounds = new[] {
                new SoundEntry { type=SFXType.Success, clip=Tone("PLACEHOLDER_Success",0.18f,false,523.25f), volume=0.28f },
                new SoundEntry { type=SFXType.Miss, clip=Tone("PLACEHOLDER_Gust",0.8f,true,0), volume=0.5f },
                new SoundEntry { type=SFXType.LeafRustle, clip=Tone("PLACEHOLDER_Rustle",0.35f,true,0), volume=0.35f },
                new SoundEntry { type=SFXType.Detach, clip=Tone("PLACEHOLDER_Detach",0.45f,true,0), volume=0.25f },
                new SoundEntry { type=SFXType.Land, clip=Tone("PLACEHOLDER_Land",0.7f,true,0), volume=0.15f }};
            EditorUtility.SetDirty(c.audio.library);
            c.audio.music = Source(c.audio.transform,"Music",true); c.audio.musicCrossfade = Source(c.audio.transform,"Music Crossfade",true);
            c.audio.ambient = Source(c.audio.transform,"Ambient",true); c.audio.rain = Source(c.audio.transform,"Rain",true);
            c.audio.sfx = Source(c.audio.transform,"SFX",false); c.audio.voice = Source(c.audio.transform,"Voice",false);
            c.leaf.audioManager = c.audio;
            c.canvas = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            c.canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = c.canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = 0.5f;
            var cream = new Color(0.94f,0.91f,0.78f);
            c.title = Label("Title",c.canvas.transform,c.font,"L E A F",new Vector2(0,345),new Vector2(600,60),32,cream);
            c.season = Label("Season",c.canvas.transform,c.font,"S P R I N G",new Vector2(0,293),new Vector2(600,40),14,cream*new Color(1,1,1,0.75f));
            c.instructions = Label("Instructions",c.canvas.transform,c.font,"Press \u2190 or \u2192 as its arrow reaches the circle.",new Vector2(0,-100),new Vector2(900,40),19,cream);
            var timingRoot = Rect("WindTiming",c.canvas.transform,Vector2.zero,new Vector2(900,140));
            var ringRect = Rect("PLACEHOLDER_TimingCircle",timingRoot,Vector2.zero,new Vector2(72,72));
            var ringImage = ringRect.gameObject.AddComponent<Image>(); ringImage.sprite = c.ring; ringImage.raycastTarget = false;
            var feedback = timingRoot.gameObject.AddComponent<WindFeedback>(); feedback.timingCircle = ringImage; feedback.ringTransform = ringRect;
            var prefabRect = Rect("WindPrompt",timingRoot,Vector2.zero,new Vector2(56,56));
            var prompt = prefabRect.gameObject.AddComponent<WindPrompt>(); prompt.rect = prefabRect;
            prompt.glyph = Label("PLACEHOLDER_Arrow",prefabRect,c.font,"\u2190",Vector2.zero,new Vector2(56,56),42,cream);
            var prefab = SavePrefab(prompt.gameObject,"Assets/Prefabs/Wind/WindPrompt.prefab").GetComponent<WindPrompt>(); UnityEngine.Object.DestroyImmediate(prompt.gameObject);
            var spawner = Node("WindPromptSpawner",c.systems.transform).AddComponent<WindPromptSpawner>(); spawner.promptPrefab=prefab; spawner.promptParent=timingRoot;
            c.wind = Node("WindManager",c.systems.transform).AddComponent<WindManager>(); c.wind.spawner=spawner; c.wind.leaf=c.leaf; c.wind.audioManager=c.audio; c.wind.feedback=feedback;
            var inputPath="Assets/Input/LeafControls.inputactions";
            Directory.CreateDirectory("Assets/Input");
            if (!File.Exists(inputPath))
            {
                var actions=ScriptableObject.CreateInstance<InputActionAsset>(); var map=actions.AddActionMap("Leaf");
                map.AddAction("Left",InputActionType.Button,"<Keyboard>/leftArrow"); map.AddAction("Right",InputActionType.Button,"<Keyboard>/rightArrow");
                File.WriteAllText(inputPath,actions.ToJson()); UnityEngine.Object.DestroyImmediate(actions); AssetDatabase.ImportAsset(inputPath);
            }
            var input=AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
            foreach (var action in input.actionMaps[0].actions)
            {
                string refPath="Assets/Input/"+action.name+".asset";
                var reference=AssetDatabase.LoadAssetAtPath<InputActionReference>(refPath);
                if (!reference) { reference=InputActionReference.Create(action); AssetDatabase.CreateAsset(reference,refPath); }
                if(action.name=="Left")c.wind.leftAction=reference; else c.wind.rightAction=reference;
            }
            SpringProfile();
            return c;
        }
        [MenuItem("LEAF/Build Phase 1 Spring Prototype")]
        public static void BuildSpring()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var c=CreateCore(); var harness=c.systems.AddComponent<SpringPrototype>(); harness.wind=c.wind; harness.spring=SpringProfile();
            SavePrefab(c.leaf.gameObject,"Assets/Prefabs/PlayerLeaf/PlayerLeaf.prefab");
            SavePrefab(c.tree,"Assets/Prefabs/Tree/Tree.prefab");
            Directory.CreateDirectory("Assets/Scenes"); AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/SpringPrototype.unity");
            Debug.Log("LEAF Phase 1 built: SpringPrototype.unity");
        }
    }
}
