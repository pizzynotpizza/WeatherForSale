using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WeatherForSale.Suction;

namespace WeatherForSale.EditorTools
{
    /// <summary>
    /// Gives the timer of every existing obby scene the small wooden plate of the boss arena, in place (no course is
    /// rebuilt). New builds get the same plate from <see cref="ObbyTestSceneBuilder"/>.
    /// </summary>
    public static class TimerStyleApplier
    {
        /// <summary>Menu entry.</summary>
        [MenuItem("Weather For Sale/Apply Small Timer To Obby Scenes")]
        public static void Apply()
        {
            Sprite plateSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Sheet/CapsuleWood.png");
            foreach (string guid in AssetDatabase.FindAssets("t:Scene Obby_", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Obby_")) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Text timer = null;
                foreach (Text t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (t.name == "TimerText") { timer = t; break; }
                }
                if (timer == null) { Debug.LogWarning("[TimerStyleApplier] No TimerText in " + path); continue; }
                if (timer.transform.parent != null && timer.transform.parent.name == "TimerPlate") { Debug.Log("[TimerStyleApplier] " + path + " already styled."); continue; }

                Transform canvas = timer.transform.parent;
                var plateGo = new GameObject("TimerPlate", typeof(RectTransform), typeof(Image));
                plateGo.transform.SetParent(canvas, false);
                plateGo.transform.SetSiblingIndex(timer.transform.GetSiblingIndex());
                var plate = (RectTransform)plateGo.transform;
                plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 1f);
                plate.pivot = new Vector2(0.5f, 1f);
                plate.anchoredPosition = new Vector2(0f, -12f);
                plate.sizeDelta = ObbyTestSceneBuilder.TimerPlateSize;
                var image = plateGo.GetComponent<Image>();
                image.sprite = plateSprite;
                image.type = Image.Type.Sliced;
                image.raycastTarget = false;

                var rt = timer.rectTransform;
                rt.SetParent(plate, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                timer.fontSize = ObbyTestSceneBuilder.TimerFontSize;
                timer.alignment = TextAnchor.MiddleCenter;
                timer.fontStyle = FontStyle.Bold;
                var shadow = timer.GetComponent<Shadow>();
                if (shadow != null) shadow.effectDistance = new Vector2(2f, -2f);

                // The suction hides the timer: it must hide the plate now.
                foreach (SuctionStage stage in Object.FindObjectsByType<SuctionStage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(stage);
                    SerializedProperty list = so.FindProperty("hideDuringSuction");
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        if (list.GetArrayElementAtIndex(i).objectReferenceValue == timer.gameObject) list.GetArrayElementAtIndex(i).objectReferenceValue = plateGo;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[TimerStyleApplier] Styled " + path);
            }
        }
    }
}
