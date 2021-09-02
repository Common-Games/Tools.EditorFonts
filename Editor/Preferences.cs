using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

using JetBrains.Annotations;

namespace CGTK.Tools.CustomEditorFonts.Editor
{
    using static PackageConstants;
    
    /// <summary>
    /// This class will act as a manager for the <see cref="Settings"/> singleton.
    /// </summary>
    internal static class SettingsManager
    {
        private static Settings _internalInstance;
        internal static Settings Instance => _internalInstance ??= new Settings(package: PACKAGE_NAME);
        public static void Save() => Instance.Save();

        [SettingsProvider]
        private static SettingsProvider Create() //TODO: TryGet?
        {
            UserSettingsProvider __provider = new UserSettingsProvider(
                path: PREFERENCES_PATH, 
                settings: Instance, 
                assemblies: new [] { typeof(SettingsManager).Assembly });
            
            return __provider;
        }
    }
    
    internal sealed class Setting<T> : UserSetting<T>
    {
        public Setting(T value, [CallerMemberName] String key = "", in SettingsScope scope = SettingsScope.User)
            : base(settings: SettingsManager.Instance, key: key, value: value, scope: scope)
        { }
    }
    
    [PublicAPI]
    internal class Preferences : EditorWindow
    {
        #region Fields
        
        [UserSetting] 
        private static readonly Setting<Font> EditorFont = new Setting<Font>(value: null);

        public static Font CustomEditorFont
        {
            get => EditorFont.value;
            
            private 
            set => EditorFont.value = value;
        }

        #endregion

        #region Methods
        
        [UserSettingBlock(category: "Custom Editor Font")]
        private static void OnSearchGUI(String searchContext)
        {
            EditorGUI.BeginChangeCheck();
            {
                Draw(searchContext);
            }
            if (EditorGUI.EndChangeCheck())
            {
                SettingsManager.Save();
            }
        }
            
        private static void Draw(in String searchContext)
        {
            GUIStyle __buttonStyle = new GUIStyle(other: GUI.skin.button)
            {
                fixedHeight = 18,
                padding     = new RectOffset(left: 0, right: 0, top: 0, bottom: 0)
            }; //Somehow get Errors when using a static version of this.... So unfortunately we have to do this. I could send them as a parameter, but don't want to.
        
            EditorGUILayout.BeginHorizontal();
            {
                CustomEditorFont = EditorGUILayout.ObjectField(
                    label: "", 
                    obj: CustomEditorFont, 
                    objType: typeof(Font), 
                    allowSceneObjects: false) as Font;
            
                if (GUILayout.Button(text: "Apply", style: __buttonStyle))
                {
                    SetEditorFont(font: CustomEditorFont);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        public static void SetEditorFont(in Font font)
        {
            const BindingFlags __FLAGS = BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty;
            
            IEnumerable<PropertyInfo> __editorStyleProperties = typeof(EditorStyles).GetProperties(bindingAttr: __FLAGS)
                .Where(predicate: property => property.PropertyType == typeof(GUIStyle));
            
            IEnumerable<PropertyInfo> __guiSkinProperties     = GUI.skin.GetType().GetProperties()
                .Where(predicate: property => property.PropertyType == typeof(GUIStyle));

            foreach (PropertyInfo __property in __editorStyleProperties)
            {
                GUIStyle __style = (GUIStyle)__property.GetValue(obj: null, index: null);
                __style.font = font;
            }

            foreach (PropertyInfo __property in __guiSkinProperties)
            {
                GUIStyle __style = (GUIStyle)__property.GetValue(obj: GUI.skin, index: null);
                __style.font = font;
            }

            foreach (GUIStyle __style in GUI.skin.customStyles)
            {
                __style.font = font;
            }

            foreach (EditorWindow __window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                __window.Repaint();   
            }
        }
        
        #endregion
    }
}
