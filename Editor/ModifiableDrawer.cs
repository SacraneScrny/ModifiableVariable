#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;

using UnityEditor;

using UnityEngine;

namespace ModifiableVariable.Editor
{
    [CustomPropertyDrawer(typeof(Modifiable<,>), true)]
    public class ModifiableDrawer : PropertyDrawer
    {
        // ── визуальные константы ───────────────────────────────────────────
        static readonly Color BgColor = new(0.25f, 0.55f, 0.90f, 0.13f);
        static readonly Color BorderColor = new(0.30f, 0.60f, 0.95f, 0.55f);
        static readonly Color PanelBg = new(0f, 0f, 0f, 0.18f);
        static readonly Color PanelBorder = new(1f, 1f, 1f, 0.06f);
        static readonly Color ResultBg = new(0f, 0f, 0f, 0.25f);

        const float OuterPad = 5f;
        const float InnerPad = 4f;
        const float HeaderH = 17f;
        const float PanelHeaderH = 14f;
        const float Gap = 5f;
        const float BorderW = 1f;

        // ── высота ────────────────────────────────────────────────────────
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var baseValueProp = property.FindPropertyRelative("_baseValue");
            var fieldH = baseValueProp != null
                ? EditorGUI.GetPropertyHeight(baseValueProp, true)
                : EditorGUIUtility.singleLineHeight;

            return OuterPad * 2 + HeaderH + Gap + PanelHeaderH + InnerPad + fieldH + InnerPad;
        }

        // ── отрисовка ─────────────────────────────────────────────────────
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // фон и рамка всего блока
            DrawFilledRect(position, BgColor);
            DrawBorder(position, BorderColor, BorderW);

            var inner = Shrink(position, OuterPad);

            // заголовок с именем поля
            var headerRect = new Rect(inner.x, inner.y, inner.width, HeaderH);
            EditorGUI.LabelField(headerRect, label, EditorStyles.boldLabel);

            // контентная зона под заголовком
            var contentY = inner.y + HeaderH + Gap;
            var contentH = inner.yMax - contentY;
            var halfW = (inner.width - Gap) * 0.5f;

            var leftRect = new Rect(inner.x, contentY, halfW, contentH);
            var rightRect = new Rect(inner.x + halfW + Gap, contentY, halfW, contentH);

            DrawBasePanel(leftRect, property);
            DrawResultPanel(rightRect, property);
        }

        // ── левая панель: редактируемый BaseValue ─────────────────────────
        void DrawBasePanel(Rect rect, SerializedProperty property)
        {
            DrawPanelBackground(rect);

            var titleRect = GetPanelTitleRect(rect);
            EditorGUI.LabelField(titleRect, "Base", Styles.PanelTitle);

            var fieldRect = GetPanelFieldRect(rect);
            var baseValueProp = property.FindPropertyRelative("_baseValue");
            if (baseValueProp != null)
                EditorGUI.PropertyField(fieldRect, baseValueProp, GUIContent.none, true);
        }

        // ── правая панель: рилтайм-превью ─────────────────────────────────
        void DrawResultPanel(Rect rect, SerializedProperty property)
        {
            DrawPanelBackground(rect, ResultBg);

            var titleRect = GetPanelTitleRect(rect);
            EditorGUI.LabelField(titleRect, "Result", Styles.PanelTitle);

            var fieldRect = GetPanelFieldRect(rect);
            var valueStr = GetEditorValue(property);

            using (new EditorGUI.DisabledScope(true))
                EditorGUI.LabelField(fieldRect, valueStr, Styles.ResultValue);
        }

        // ── вспомогательные методы рендера ────────────────────────────────
        void DrawPanelBackground(Rect rect, Color? overrideBg = null)
        {
            DrawFilledRect(rect, overrideBg ?? PanelBg);
            DrawBorder(rect, PanelBorder, BorderW);
        }

        static Rect GetPanelTitleRect(Rect panel) =>
            new(panel.x + InnerPad, panel.y + InnerPad, panel.width - InnerPad * 2, PanelHeaderH);

        static Rect GetPanelFieldRect(Rect panel)
        {
            var top = panel.y + InnerPad + PanelHeaderH + InnerPad * 0.5f;
            return new Rect(panel.x + InnerPad, top, panel.width - InnerPad * 2, panel.yMax - top - InnerPad);
        }

        static void DrawFilledRect(Rect r, Color c) => EditorGUI.DrawRect(r, c);

        static void DrawBorder(Rect r, Color c, float w)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, w, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        static Rect Shrink(Rect r, float amount) =>
            new(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);

        // ── рефлексия: получить значение из Modifiable ───────────────────
        static string GetEditorValue(SerializedProperty property)
        {
            try
            {
                var obj = ResolvePropertyObject(property);
                if (obj == null) return "—";

                var method = obj.GetType().GetMethod(
                    "GetValueEditor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (method == null) return "—";

                var result = method.Invoke(obj, null);
                return result?.ToString() ?? "null";
            }
            catch (Exception e)
            {
                return $"err: {e.Message}";
            }
        }

        static object ResolvePropertyObject(SerializedProperty property)
        {
            object current = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data[", "[");

            foreach (var token in path.Split('.'))
            {
                if (token.Contains("["))
                {
                    var name = token[..token.IndexOf('[')];
                    var idx = int.Parse(token[(token.IndexOf('[') + 1)..token.IndexOf(']')]);
                    current = GetField(current, name);
                    if (current is IList list) current = list[idx];
                }
                else
                {
                    current = GetField(current, token);
                }
                if (current == null) return null;
            }
            return current;
        }

        static object GetField(object obj, string name)
        {
            if (obj == null) return null;
            for (var t = obj.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null) return f.GetValue(obj);
            }
            return null;
        }

        // ── статические стили ─────────────────────────────────────────────
        static class Styles
        {
            public static readonly GUIStyle PanelTitle = new(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.65f, 0.85f, 1f, 0.85f) }
            };

            public static readonly GUIStyle ResultValue = new(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.75f, 1f, 0.8f, 0.9f) }
            };
        }
    }
}
#endif