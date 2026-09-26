using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static LeafGame.Editor.LeafPrototypeBuilder;

namespace LeafGame.Editor
{
    public static class LeafSceneBuilder
    {
        private static readonly Color Cream = new Color(0.94f, 0.91f, 0.78f);

        private static WindDifficultyProfile Difficulty(string name, float interval, float speed, float perfect, float good, float strength, float gust, int maximum, float sequence)
        {
            var p = Asset<WindDifficultyProfile>("Assets/ScriptableObjects/Wind/" + name + ".asset");
            p.spawnInterval=interval; p.arrowSpeed=speed; p.perfectWindow=perfect; p.goodWindow=good; p.windStrength=strength;
            p.gustProbability=gust; p.maximumSimultaneousPrompts=maximum; p.sequenceProbability=sequence;
            EditorUtility.SetDirty(p); return p;
        }
        private static DialogueData Dialogue(string name, params string[] exchange)
        {
            var d = Asset<DialogueData>("Assets/ScriptableObjects/Dialogue/" + name + ".asset");
            d.lines = new DialogueLine[exchange.Length / 2];
            for (int i=0;i<d.lines.Length;i++) d.lines[i]=new DialogueLine { speaker=exchange[i*2],text=exchange[i*2+1],seconds=3.5f };
            EditorUtility.SetDirty(d); return d;
        }
        private static SeasonData Season(string name, SeasonKind kind, float duration, WindDifficultyProfile profile, Core c)
        {
            var d=Asset<SeasonData>("Assets/ScriptableObjects/Seasons/"+name+".asset");
            d.seasonName=name; d.kind=kind; d.duration=duration; d.windDifficulty=profile; d.backgroundSprite=c.square;
            d.foliageSprites=new[]{c.foliage[0].sprite}; d.ambientAudio=c.audio.library.ambient; d.music=c.audio.library.gameplayMusic;
            return d;
        }
        private static CanvasGroup Panel(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect=Rect(name,parent,position,size);
            var image=rect.gameObject.AddComponent<Image>(); image.color=new Color(0.04f,0.07f,0.06f,0.62f); image.raycastTarget=false;
            var group=rect.gameObject.AddComponent<CanvasGroup>(); group.alpha=0; group.blocksRaycasts=false; group.interactable=false;
            return group;
        }
        private static Button Button(string name, Transform parent, Font font, Vector2 position)
        {
            var r=Rect(name,parent,position,new Vector2(155,42));
            var image=r.gameObject.AddComponent<Image>(); image.color=new Color(0.18f,0.23f,0.17f,0.9f);
            var button=r.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.highlightedColor=new Color(0.75f,0.88f,0.62f); button.colors=colors;
            Label("Label",r,font,name,Vector2.zero,new Vector2(155,42),18,Cream);
            return button;
        }
        private static ParticleSystem Particles(string name, Sprite sprite, Transform parent, bool rain)
        {
            var go=Node(name,parent); go.transform.position=rain?new Vector3(0,6,0):new Vector3(-9,1,0);
            go.SetActive(false);
            var ps=go.AddComponent<ParticleSystem>();
            var main=ps.main; main.loop=true; main.playOnAwake=true; main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.useUnscaledTime=true; main.startLifetime=rain?2.8f:3f; main.startSpeed=0; main.maxParticles=350;
            main.startSize3D=true; main.startSizeX=rain?0.015f:0.7f; main.startSizeY=rain?0.24f:0.012f; main.startSizeZ=1f;
            main.startColor=rain?new Color(0.72f,0.82f,0.83f,0.24f):new Color(0.88f,0.91f,0.81f,0.28f);
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=rain?new Vector3(22,1,0):new Vector3(1,7,0);
            var emission=ps.emission; emission.rateOverTime=0;
            var velocity=ps.velocityOverLifetime; velocity.enabled=true; velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=rain?-0.8f:6f; velocity.y=rain?-5f:0.1f;
            var renderer=ps.GetComponent<ParticleSystemRenderer>(); renderer.renderMode=ParticleSystemRenderMode.Billboard; renderer.sortingOrder=rain?15:8;
            string matPath="Assets/Art/VFX/PLACEHOLDER_"+(rain?"Rain":"Wind")+".mat";
            Directory.CreateDirectory("Assets/Art/VFX");
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!mat) { mat=new Material(Shader.Find("Sprites/Default")); mat.mainTexture=sprite.texture; AssetDatabase.CreateAsset(mat,matPath); }
            renderer.sharedMaterial=mat;
            go.SetActive(true); return ps;
        }
        private static void AnimationSetup(LeafController leaf)
        {
            Directory.CreateDirectory("Assets/Animations/Leaf");
            string path="Assets/Animations/Leaf/Leaf.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(!controller)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(path);
                controller.AddParameter("State",AnimatorControllerParameterType.Int);
                var machine=controller.layers[0].stateMachine;
                string[] names={"ATTACHED","UNSTABLE","DETACHING","FALLING","LANDED"};
                for(int i=0;i<names.Length;i++)
                {
                    var clip=new AnimationClip { name="PLACEHOLDER_"+names[i],frameRate=30 };
                    // Only child scale is animated: the stem-pivot/path remains gameplay-owned.
                    float duration=i==1?0.7f:2.4f;
                    float amplitude=i==1?0.045f:(i==4?0f:0.015f);
                    clip.SetCurve("",typeof(Transform),"m_LocalScale.y",AnimationCurve.EaseInOut(0,1.15f,duration/2,1.15f+amplitude));
                    var curve=new AnimationCurve(new Keyframe(0,1.15f),new Keyframe(duration/2,1.15f+amplitude),new Keyframe(duration,1.15f));
                    clip.SetCurve("",typeof(Transform),"m_LocalScale.y",curve);
                    var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true; AnimationUtility.SetAnimationClipSettings(clip,settings);
                    AssetDatabase.CreateAsset(clip,"Assets/Animations/Leaf/PLACEHOLDER_"+names[i]+".anim");
                    var state=machine.AddState(names[i]); state.motion=clip; if(i==0)machine.defaultState=state;
                    var transition=machine.AddAnyStateTransition(state); transition.hasExitTime=false; transition.duration=0.18f; transition.canTransitionToSelf=false;
                    transition.AddCondition(AnimatorConditionMode.Equals,i,"State");
                }
            }
            leaf.animator=leaf.spriteRenderer.gameObject.AddComponent<Animator>(); leaf.animator.runtimeAnimatorController=controller;
        }

        [MenuItem("LEAF/Build Complete Prototype (generated assets)")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before building.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var c=CreateCore();
            c.leaf.protectUntilFinal=true;
            AnimationSetup(c.leaf);
            var camera=c.camera.gameObject.AddComponent<LeafCameraController>(); camera.targetCamera=c.camera; camera.leaf=c.leaf;
            c.wind.cameraController=camera;
            var windParticles=Particles("PLACEHOLDER_WindStreaks",c.square,c.systems.transform,false);
            c.wind.feedback.windStreaks=windParticles;
            SavePrefab(windParticles.gameObject,"Assets/Prefabs/Wind/WindStreaks.prefab");
            var environment=c.tree.AddComponent<TreeEnvironment>(); environment.background=c.sky; environment.foliage=c.foliage; environment.playerLeaf=c.leaf.spriteRenderer;
            environment.backgroundCrossfade=SpriteNode("PLACEHOLDER_BackgroundCrossfade",null,c.square,c.sky.transform.position,c.sky.transform.localScale,Color.clear,-29);
            var seasons=Node("SeasonManager",c.systems.transform).AddComponent<SeasonManager>(); seasons.wind=c.wind; seasons.environment=environment; seasons.audioManager=c.audio;
            var spring=Season("Spring",SeasonKind.Spring,50,SpringProfile(),c);
            spring.dialogueEvents=new[]{new SeasonDialogueEvent { atSeconds=5,dialogue=Dialogue("Spring_Holding","TREE","Still holding on?","LEAF","I like it here.") }};
            var summer=Season("Summer",SeasonKind.Summer,65,Difficulty("Summer",4.3f,180,0.09f,0.2f,21,0.22f,1,0),c);
            summer.backgroundColor=new Color(0.25f,0.32f,0.24f); summer.foliageColor=new Color(0.22f,0.38f,0.18f); summer.leafColor=new Color(0.62f,0.76f,0.26f);
            summer.rainIntensity=0.65f; summer.rainStartDelay=12; summer.rainInstabilityPerSecond=0.003f;
            summer.dialogueEvents=new[]{
                new SeasonDialogueEvent { atSeconds=13,dialogue=Dialogue("Summer_Rain","LEAF","Remember our first rain?","TREE","You called every drop a visitor.") },
                new SeasonDialogueEvent { atSeconds=43,dialogue=Dialogue("Summer_View","TREE","The whole sky, just for you.","LEAF","It's better from your branch.") }};
            var autumn=Season("Autumn",SeasonKind.Autumn,65,Difficulty("Autumn",3.6f,235,0.075f,0.17f,28,0.48f,2,0.3f),c);
            autumn.backgroundColor=new Color(0.26f,0.29f,0.28f); autumn.foliageColor=new Color(0.56f,0.35f,0.17f); autumn.leafColor=new Color(0.94f,0.59f,0.23f);
            autumn.foliageDensity=0.45f; autumn.fallingLeafInterval=3.5f;
            autumn.dialogueEvents=new[]{new SeasonDialogueEvent { atSeconds=9,dialogue=Dialogue("Autumn_Leaving","LEAF","They're all leaving.","TREE","I know. I'm still here.") }};
            autumn.finalChallengeSeconds=12;
            autumn.finalChallengeProfile=Difficulty("FinalChallenge",2.8f,265,0.07f,0.16f,34,0.75f,2,0.5f);
            seasons.seasons=new[]{spring,summer,autumn}; foreach(var s in seasons.seasons)EditorUtility.SetDirty(s);
            var rain=Node("RainManager",c.systems.transform).AddComponent<RainManager>(); rain.seasons=seasons; rain.leaf=c.leaf; rain.audioManager=c.audio;
            rain.particles=Particles("PLACEHOLDER_Rain",c.square,rain.transform,true); SavePrefab(rain.particles.gameObject,"Assets/Prefabs/Rain/Rain.prefab");
            var dialogue=Node("DialogueManager",c.systems.transform).AddComponent<DialogueManager>(); dialogue.seasons=seasons; dialogue.audioManager=c.audio;
            dialogue.panel=Panel("DialoguePanel",c.canvas.transform,new Vector2(0,-320),new Vector2(850,104));
            dialogue.speakerLabel=Label("Speaker",dialogue.panel.transform,c.font,"",new Vector2(0,28),new Vector2(750,26),14,Cream);
            dialogue.lineLabel=Label("Line",dialogue.panel.transform,c.font,"",new Vector2(0,-13),new Vector2(800,60),23,Cream);
            var decoration=Node("Autumn Background Leaves",c.systems.transform).AddComponent<BackgroundLeafSpawner>(); decoration.seasons=seasons;
            var decorativeGo=Node("PLACEHOLDER_FallingLeaf"); var decorative=decorativeGo.AddComponent<DecorativeLeaf>();
            decorative.spriteRenderer=decorativeGo.AddComponent<SpriteRenderer>(); decorative.spriteRenderer.sprite=c.leafSprite; decorative.spriteRenderer.sortingOrder=3;
            decoration.prefab=SavePrefab(decorativeGo,"Assets/Prefabs/FallingLeaves/FallingLeaf.prefab").GetComponent<DecorativeLeaf>(); Object.DestroyImmediate(decorativeGo);
            decoration.branchPoints=new Transform[6];
            for(int i=0;i<6;i++) { var p=Node("Leaf spawn "+i,c.tree.transform); p.transform.position=new Vector3(-6+i*1.8f,3.4f,0); decoration.branchPoints[i]=p.transform; }
            var finalData=Asset<FinalSequenceData>("Assets/ScriptableObjects/FinalSequence/FinalSequence.asset");
            finalData.fallDuration=58; finalData.endingDuration=12;
            finalData.narration=new[]{
                new NarrationCue { atSeconds=3,duration=6,text="You learned the sky from one small branch." },
                new NarrationCue { atSeconds=11,duration=6,text="The rain. The warm afternoons.\nThe familiar voice beneath you." },
                new NarrationCue { atSeconds=20,duration=6,text="When the wind came, you held on.\nAgain, and again." },
                new NarrationCue { atSeconds=29,duration=6,text="You thought leaving would undo\neverything that made this home." },
                new NarrationCue { atSeconds=39,duration=6,text="But the branch has not forgotten.\nAnd you have not failed it." },
                new NarrationCue { atSeconds=49,duration=6,text="What held you is still part of you.\nEven now. Even here." }};
            for(int i=0;i<finalData.narration.Length;i++)
                finalData.narration[i].voice=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/PLACEHOLDER_SyntheticNarration_"+(i+1).ToString("D2")+".wav");
            EditorUtility.SetDirty(finalData);
            var narration=Node("NarrationManager",c.systems.transform).AddComponent<NarrationManager>(); narration.audioManager=c.audio;
            narration.panel=Panel("NarrationPanel",c.canvas.transform,new Vector2(0,-310),new Vector2(1030,110));
            narration.subtitle=Label("Narration subtitle",narration.panel.transform,c.font,"",Vector2.zero,new Vector2(990,100),25,Cream);
            var ui=c.canvas.AddComponent<LeafUI>(); ui.title=c.title; ui.seasonLabel=c.season; ui.instructions=c.instructions;
            ui.endingPanel=Panel("EndingPanel",c.canvas.transform,new Vector2(0,160),new Vector2(1140,300));
            ui.endingLine=Label("FinalLine",ui.endingPanel.transform,c.font,"",new Vector2(0,94),new Vector2(1080,70),28,Cream);
            ui.menuText=Label("MenuHint",ui.endingPanel.transform,c.font,"ENTER · begin again       ESC · leave",new Vector2(0,30),new Vector2(900,40),16,Cream);
            ui.restartButton=Button("Begin again",ui.endingPanel.transform,c.font,new Vector2(-95,-20));
            ui.quitButton=Button("Leave",ui.endingPanel.transform,c.font,new Vector2(95,-20));
            ui.creditText=Label("MusicCredit",ui.endingPanel.transform,c.font,"",new Vector2(0,-99),new Vector2(1080,80),14,Cream);
            var eventSystem=Node("EventSystem"); eventSystem.AddComponent<EventSystem>(); eventSystem.AddComponent<InputSystemUIInputModule>();
            var final=Node("FinalSequenceController",c.systems.transform).AddComponent<FinalSequenceController>();
            final.data=finalData; final.leaf=c.leaf; final.wind=c.wind; final.rain=rain; final.dialogue=dialogue; final.audioManager=c.audio; final.narration=narration; final.cameraController=camera;
            var flow=c.systems.AddComponent<GameFlow>(); flow.seasons=seasons; flow.finalSequence=final; flow.backgroundLeaves=decoration; flow.ui=ui; flow.audioManager=c.audio;
            SavePrefab(c.leaf.gameObject,"Assets/Prefabs/PlayerLeaf/PlayerLeaf.prefab");
            // Save Tree visuals separately from scene-specific environment references.
            var backgroundCrossfade=environment.backgroundCrossfade;
            Object.DestroyImmediate(environment);
            SavePrefab(c.tree,"Assets/Prefabs/Tree/Tree.prefab");
            environment=c.systems.AddComponent<TreeEnvironment>(); environment.background=c.sky; environment.backgroundCrossfade=backgroundCrossfade;
            environment.foliage=c.foliage; environment.playerLeaf=c.leaf.spriteRenderer; seasons.environment=environment;
            SavePrefab(c.canvas,"Assets/Prefabs/UI/LeafUI.prefab");
            // Main is intentionally the first and only build scene.
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};
            PlayerSettings.productName="LEAF"; PlayerSettings.companyName="LEAF Jam"; PlayerSettings.runInBackground=true;
            Selection.activeGameObject=c.systems;
            Debug.Log("LEAF complete scene built. Planned session: "+flow.PlannedDuration+" seconds.");
        }
    }
}
