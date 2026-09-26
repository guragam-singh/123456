# LEAF — Unity 6 prototype

## Play

Open **Assets/Scenes/Main.unity**, press **Play**, and focus the Game View.
Press **Left / Right Arrow** when the matching arrow reaches the centre ring.
The symbol shows the key to press; a left arrow approaches from the left, a right arrow from the right.
There is no score, health display or failure menu. Early/wrong presses cause a small gust; holding a key does not repeatedly score.
At the ending, **Enter/R** restarts and **Escape** quits (stops Play Mode in the Editor). Mouse buttons offer the same options.

Unity **6000.4.3f1**, URP 17.4 and Input System 1.19 are already present. Main is the first enabled Build Settings scene.
No package installation or manual scene wiring is required. Build for PC using Unity's Build Profiles.

## Pacing

| Segment | Seconds | Cumulative |
|---|---:|---:|
| Spring | 50 | 50 |
| Summer | 65 | 115 |
| Autumn, including its 12-second final challenge | 65 | 180 |
| Scripted release and fall | 58 | 238 |
| Quiet ending and menu reveal | 12 | **250 / 4:10** |

Dialogue and transitions overlap play; neither pauses the season clock. A hard guard reserves the final fall and ending before the 300-second ceiling. Use **LEAF asset validation** before shipping altered timing: arbitrary bad Inspector values can still ruin pacing. Normal configured play always progresses, even with no inputs or every prompt hit.

### Detachment design decision

The brief asks both for ordinary threshold detachment and for the protagonist to remain attached throughout Autumn. `LeafController.protectUntilFinal` resolves that conflict: **Main enables the narrative tether**; misses accumulate strain and increasingly violent motion without releasing the leaf early. `SpringPrototype.unity` retains ordinary ±175° threshold detachment for isolated mechanic work. The final controller uses a separate deterministic path, never ordinary falling physics.

## Where artists/designers edit

| Change | Asset / Inspector reference |
|---|---|
| Leaf artwork | `Assets/Prefabs/PlayerLeaf/PlayerLeaf.prefab` → StemPivot → PLACEHOLDER_PlayerLeaf → SpriteRenderer |
| Leaf animation | `Assets/Animations/Leaf/Leaf.controller`; replace five state motions |
| Tree/branches/canopy | `Assets/Prefabs/Tree/Tree.prefab` |
| Seasonal palette and sprites | `Assets/ScriptableObjects/Seasons/{Spring,Summer,Autumn}.asset` |
| Length and dialogue times | Season assets: duration, dialogueEvents |
| Wind speed, timing windows, gusts, sequences | `Assets/ScriptableObjects/Wind/*.asset` |
| Wind glyph presentation | `Assets/Prefabs/Wind/WindPrompt.prefab` |
| Rain and wind visuals | `Assets/Prefabs/Rain/Rain.prefab`, `Assets/Prefabs/Wind/WindStreaks.prefab` |
| Decorative leaves | `Assets/Prefabs/FallingLeaves/FallingLeaf.prefab` |
| Dialogue words, speakers, voice clips | `Assets/ScriptableObjects/Dialogue/*.asset` |
| Music, ambient, rain, rustle and cues | `Assets/ScriptableObjects/Audio/AudioLibrary.asset` |
| Final recording | AudioLibrary → **Final Music** |
| Final poetry, voice clips, path, rotation and ending line | `Assets/ScriptableObjects/FinalSequence/FinalSequence.asset` |
| UI appearance | `Assets/Prefabs/UI/LeafUI.prefab` |
| Camera offsets, smoothing, zoom | Main Camera → LeafCameraController |
| Leaf inertia and feedback balance | PlayerLeaf → LeafController |

Leaf art is authored pointing along +X, with its pivot at the **stem** (left centre). The parent StemPivot supplies the -90° hanging pose. Keep the pivot convention or offset the visual child to align replacement artwork. Animator clips drive only the visual child; they must not key the root's path or stem transform. Sprite-sheet clips can replace the placeholder motions directly. Seasonal color tint can be set to white for pre-colored final art.

All placeholder visuals and sounds have `PLACEHOLDER_` names. The generated drone **is not** El Testament d'Amèlia. Assign the supplied recording to Final Music to use it. The required credit is stored verbatim in AudioLibrary. Synthetic voice placeholders are explicitly named and should be replaced by the intended heavy, calm narrator. Empty voice fields leave readable subtitles and do not break progression.

## Main hierarchy

```
LEAF — Systems
  GameFlow (component)
  TreeEnvironment (component; seasonal renderer references)
  AudioManager
    Music / Music Crossfade / Ambient / Rain / SFX / Voice
  WindManager
  WindPromptSpawner
  PLACEHOLDER_WindStreaks
  SeasonManager
  RainManager / PLACEHOLDER_Rain
  DialogueManager
  Autumn Background Leaves
  NarrationManager
  FinalSequenceController
Main Camera (Camera, AudioListener, LeafCameraController)
PLACEHOLDER_Background / PLACEHOLDER_BackgroundCrossfade
PLACEHOLDER_Ground
Tree (prefab instance: trunk, branches, foliage, decorative spawn points)
PlayerLeaf (prefab instance)
  StemPivot
    PLACEHOLDER_PlayerLeaf (SpriteRenderer, Animator)
UI (screen-space Canvas, scaler, LeafUI)
  Title / Season / Instructions
  WindTiming / PLACEHOLDER_TimingCircle / spawned WindPrompts
  DialoguePanel / Speaker / Line
  NarrationPanel / Narration subtitle
  EndingPanel / FinalLine / buttons / MusicCredit
EventSystem (InputSystemUIInputModule)
```

Scene-specific cross-object references are wired in Main. Prefabs store reusable presentation; their scene instances supply references to managers. There are no runtime name lookups or Resources asset loads.

## Script responsibilities

### Core and wind
- `GameFlow`: explicit Intro → Spring → Summer → Autumn → FinalSequence → Ending, session guard, menu timing.
- `SpringPrototype`: isolated Phase 1 harness; not in Main.
- `WindDifficultyProfile`: all per-season prompt/gust/timing balance.
- `WindManager`: keyboard edges, matching nearest prompt, expiry, result dispatch, input lock.
- `WindPromptSpawner`: scheduled singles/sequences, concurrent cap, prefab lifecycle.
- `WindPrompt`: one arrow's visual approach and immutable timing snapshot.
- `WindTimingEvaluator`: pure symmetric inclusive Perfect/Good boundary checks.
- `WindFeedback`: color/scale pulse and gust particles, never result text.
- `LeafController`: damped spring motion, inertia, instability, cumulative strain, state/animation and audio cues; independent ordinary detach path.

### Seasons and atmosphere
- `SeasonData`: duration, art, colors, wind/music references, rain, decorative interval, dialogue milestones and final challenge.
- `SeasonManager`: continuous unscaled clock and immediate automatic transitions.
- `TreeEnvironment`: sub-second color/background crossfade and foliage reduction.
- `RainManager`: removable optional modifier, Summer emission/audio/weight, cleanup on disable.
- `DecorativeLeaf`: non-physical drift/rotation/lifetime.
- `BackgroundLeafSpawner`: Autumn-only decoration with live-object cap and cleanup.
- `LeafCameraController`: fixed close-up, subtle miss movement, slow final tracking and ground framing.

### Dialogue, audio and ending
- `DialogueData`: short external speaker/text/duration/voice arrays.
- `DialogueManager`: one short beat at a time, milestones, fades; never pauses play.
- `AudioLibrary`: replaceable music/ambient/rain/SFX clips and exact credit.
- `AudioManager`: separate music, ambient, SFX and voice channels, crossfade, rain layer, final attenuation and fade-out.
- `FinalSequenceData`: configurable cinematic duration, drift/descent/rotation curves, narration timeline and ending text.
- `FinalSequenceController`: atomic gameplay shutdown, pose-driven fall, music/narration/camera coordination, exact landing.
- `NarrationManager`: final-only voice cues and subtitles.
- `LeafUI`: onboarding, season label, quiet line, mouse/keyboard ending controls.

### Editor tooling
- `LeafPrototypeBuilder`: placeholder PNG/WAV creation, Input Actions, core hierarchy, standalone Spring scene.
- `LeafSceneBuilder`: complete prewired Main scene, prefab assets, configs and five-state Animator.
- `LeafVerification`: meaningful asset/timing/path assertions and full real-time integration probes. Results are written to `Temp/LeafVerification/`.
- `Tools/GeneratePlaceholderNarration.ps1`: Windows speech synthesis for replaceable voice WAV placeholders.

## Input and animation

`Assets/Input/LeafControls.inputactions`: Leaf map, Left and Right Button actions bound to keyboard arrow keys. Serialized InputActionReference assets connect WindManager. Up/down are reserved; no extra mechanic is assigned. InputSystemUIInputModule handles mouse UI; ending keyboard shortcuts are explicit.

Animator integer **State** maps 0 ATTACHED, 1 UNSTABLE, 2 DETACHING, 3 FALLING, 4 LANDED. AnyState transitions blend 0.18s and cannot self-transition. Root motion is unused. Deterministic falling curves remain in FinalSequenceData, independently replaceable from the sprite animation.

## Implementation/rebuild order

1. Build/play `SpringPrototype`: geometry, stem motion, ring and input, audio hooks.
2. Evaluate timing windows, cumulative miss motion, success recovery, subtle feedback/camera.
3. Add 50/65/65-second SeasonData and automatic palette/difficulty transitions.
4. Connect Summer rain; disabling RainManager leaves wind/progression intact.
5. Connect four short dialogue beats, each 7s and overlapping gameplay.
6. Connect sparse Autumn leaf decoration and final challenge profile.
7. Connect the 58s deterministic fall and 12s ending with voice/music slots.
8. Verify the complete 250s run, replace placeholders, mix audio, inspect final framing.

The generated scene is already complete. **Builder menu commands are regeneration tools**, not needed to play. They overwrite generated balance/dialogue/prefab content; don't run them over artist-edited assets without backing up your changes.

## Verification usage

Open Main outside Play Mode. Through an Editor script/MCP call:

```csharp
LeafGame.Editor.LeafVerification.ValidateAssets();
LeafGame.Editor.LeafVerification.StartRun(false); // uninterrupted no-input / all-misses run
// On a separate fresh Play session:
LeafGame.Editor.LeafVerification.StartRun(true);  // auto-submits each prompt at its target time
```

The probe checks season order, timing boundaries, continuous downward path, no missing scripts, rain, decorative leaves, attached story leaf, final input lock and prompt cleanup, exact landing, and ending within 240–300 seconds. It does not accelerate time. `Temp/LeafVerification/*.txt` records the observed timing. Play normally for subjective feel and narrative evaluation.
