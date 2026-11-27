using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「Tools/Higashi/READYのプレハブとシーンを作る」：Art/Sprites/READY.png を使った READY? コールのプレハブ（Level/UI/ReadyCall.prefab）と、
/// それを置いてくり返し再生する確認用のシーン（Higashi_ReadyCallScene）を作るエディタ用メニュー。
/// Personal/Higashi の外は触らない。
/// </summary>
public static class ReadyCallBuilder
{
    private const string MENU_PATH = "Tools/Higashi/READYのプレハブとシーンを作る";
    private const string SPRITE_PATH = "Assets/Personal/Higashi/Art/Sprites/READY.png";
    private const string PREFAB_PATH = "Assets/Personal/Higashi/Level/UI/ReadyCall.prefab";
    private const string SCENE_PATH = "Assets/Personal/Higashi/Level/Scenes/Higashi_ReadyCallScene.unity";
    private const string CANVAS_NAME = "ReadyCallCanvas";
    private const string DIALOG_TITLE = "READY";

    // 画像の横幅（Canvas 上の単位。1920×1080 基準）。縦は画像の比率に合わせる
    private const float IMAGE_WIDTH = 1000f;

    // 画像（2894×1771）の中で、文字の下端がある高さ（ピクセル。上が 0）。Pivot をここに置いて、着地の潰れと揺れの支点にする。絵が変わったら測り直す
    private const float ART_HEIGHT = 1771f;
    private const float ART_CONTENT_BOTTOM = 1610f;

    // 確認用のシーンで、もう一度出すまでの時間（秒）
    private const float PREVIEW_LOOP_DELAY = 1f;

    private static readonly Color BACKGROUND_COLOR = new Color32(0x2E, 0x3B, 0x68, 0xFF);

    /// <summary> プレハブ（あれば作り直すか確認する）と、確認用のシーンを作る。 </summary>
    [MenuItem(MENU_PATH)]
    private static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab != null && EditorUtility.DisplayDialog(DIALOG_TITLE, "ReadyCall のプレハブがすでにあります。作り直しますか？\n（作り直すと、プレハブで直したところは消えます）", "作り直す", "そのまま使う"))
        {
            prefab = null;
        }
        if (prefab == null) prefab = CreatePrefab();
        if (prefab == null) return;

        BuildScene(prefab);
    }

    /// <summary>
    /// プレハブを作る。構成：ReadyCall（画面いっぱい・ReadyCallEffect）→ Image（READY の画像。Pivot は文字の下端）。
    /// 文字の見た目の中心が画面の真ん中に来るよう、Pivot をずらした分だけ下に置く。作れなかったら null を返す。
    /// </summary>
    private static GameObject CreatePrefab()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        if (sprite == null)
        {
            EditorUtility.DisplayDialog(DIALOG_TITLE, $"READY の画像を Sprite として読み込めませんでした。\n{SPRITE_PATH}", "OK");
            return null;
        }

        UnityEngine.SceneManagement.Scene workScene = EditorSceneManager.NewPreviewScene();
        try
        {
            var holder = new GameObject("PrefabCanvas", typeof(RectTransform), typeof(Canvas));
            holder.layer = LayerMask.NameToLayer("UI");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, workScene);

            RectTransform root = CreateUIObject("ReadyCall", holder.transform, Vector2.zero, Vector2.zero);
            Stretch(root);

            float height = IMAGE_WIDTH * sprite.rect.height / sprite.rect.width;
            float pivotY = (ART_HEIGHT - ART_CONTENT_BOTTOM) / ART_HEIGHT;
            RectTransform imageRect = CreateUIObject("Image", root, Vector2.zero, new Vector2(IMAGE_WIDTH, height));
            imageRect.pivot = new Vector2(0.5f, pivotY);
            imageRect.anchoredPosition = new Vector2(0f, (pivotY - 0.5f) * height);
            var image = imageRect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var effect = root.gameObject.AddComponent<ReadyCallEffect>();
            var serialized = new SerializedObject(effect);
            serialized.FindProperty("_image").objectReferenceValue = imageRect;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PREFAB_PATH, out bool isSaved);
            if (!isSaved)
            {
                EditorUtility.DisplayDialog(DIALOG_TITLE, $"プレハブを保存できませんでした。\n{PREFAB_PATH}", "OK");
                return null;
            }
            return prefab;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(workScene);
        }
    }

    /// <summary> 確認用のシーンを作り、プレハブを置いてくり返し再生するようにする（くり返しはシーンでの上書き）。シーンがあれば作り直すか確認する。 </summary>
    private static void BuildScene(GameObject prefab)
    {
        UnityEngine.SceneManagement.Scene scene;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) != null)
        {
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != CANVAS_NAME) continue;
                if (!EditorUtility.DisplayDialog(DIALOG_TITLE, $"{CANVAS_NAME} がすでにあります。削除して作り直しますか？", "作り直す", "やめる")) return;
                Undo.DestroyObjectImmediate(root);
            }
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
        }

        Transform canvas = CreateCanvas();
        RectTransform background = CreateUIObject("Background", canvas, Vector2.zero, Vector2.zero);
        Stretch(background);
        var backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = BACKGROUND_COLOR;
        backgroundImage.raycastTarget = false;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
        Stretch((RectTransform)instance.transform);
        var effect = instance.GetComponent<ReadyCallEffect>();
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("_loopDelay").floatValue = PREVIEW_LOOP_DELAY;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(effect);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = instance;
    }

    /// <summary> 1920×1080 基準で画面サイズに合わせて伸び縮みする Canvas を作る。 </summary>
    private static Transform CreateCanvas()
    {
        var go = new GameObject(CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Undo.RegisterCreatedObjectUndo(go, "Create Ready Call Canvas");
        return go.transform;
    }

    /// <summary> 親の下に RectTransform 付きの空オブジェクトを作る。 </summary>
    private static RectTransform CreateUIObject(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;

        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    /// <summary> 親いっぱいに広げる。 </summary>
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
