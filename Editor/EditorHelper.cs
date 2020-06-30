using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MS.Async.Editor{
    internal class EditorHelper
    {

        public class SplitterStateProxy{
            private static Type _SplitterStateType;
            internal static Type SplitterStateType{
                get{
                    if(_SplitterStateType == null){
                        _SplitterStateType = typeof(EditorWindow).Assembly.GetTypes().First(x => x.FullName == "UnityEditor.SplitterState");
                    }
                    return _SplitterStateType;
                }
            }

            private static ConstructorInfo _SplitterStateTypeCtor;
            internal static ConstructorInfo SplitterStateTypeCtor{
                get{
                    if(_SplitterStateTypeCtor == null){
                        _SplitterStateTypeCtor = SplitterStateType.GetConstructor(flags, null, new Type[] { typeof(float[]), typeof(int[]), typeof(int[]) }, null);
                    }
                    return _SplitterStateTypeCtor;
                }
            }

            private object _raw;

            public SplitterStateProxy(float[] relativeSizes, int[] minSizes, int[] maxSizes){
                _raw =  SplitterStateTypeCtor.Invoke(new object[] { relativeSizes, minSizes, maxSizes });
            }

            internal object raw{
                get{
                    return _raw;
                }
            }

            public int currentActiveSplitter{
                get{
                    return (int)SplitterStateType.GetField("currentActiveSplitter",flags).GetValue(_raw);
                }
            }
            public int[] realSizes{
                get{
                    return (int[])SplitterStateType.GetField("realSizes",flags).GetValue(_raw);
                }
            }
            public int splitSize{
                get{
                    return (int)SplitterStateType.GetField("splitSize",flags).GetValue(_raw);
                }set{
                    SplitterStateType.GetField("splitSize",flags).SetValue(_raw,value);
                }
            }

        }

        static BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;


        private static Type _SplitterGUILayoutType;
        private static Type SplitterGUILayoutType{
            get{
                if(_SplitterGUILayoutType == null){
                    _SplitterGUILayoutType = typeof(EditorWindow).Assembly.GetTypes().First(x => x.FullName == "UnityEditor.SplitterGUILayout");
                }
                return _SplitterGUILayoutType;
            }
        }

        private static MethodInfo _BeginVerticalSplitMethod = null;

        private static MethodInfo BeginVerticalSplitMethod{
            get{
                if(_BeginVerticalSplitMethod == null){
                    _BeginVerticalSplitMethod = SplitterGUILayoutType.GetMethod("BeginVerticalSplit", flags, null, new Type[] { SplitterStateProxy.SplitterStateType, typeof(GUILayoutOption[]) }, null);
                }
                return _BeginVerticalSplitMethod;
            }
        }

        private static MethodInfo _EndVerticalSplitMethod = null;

        private static MethodInfo EndVerticalSplitMethod{
            get{
                if(_EndVerticalSplitMethod == null){
                    _EndVerticalSplitMethod = SplitterGUILayoutType.GetMethod("EndVerticalSplit", flags, null, Type.EmptyTypes, null);
                }
                return _EndVerticalSplitMethod;
            }
        }

        private static MethodInfo _BeginHorizontalSplitMethod = null;

        private static MethodInfo BeginHorizontalSplitMethod{
            get{
                if(_BeginHorizontalSplitMethod == null){
                    _BeginHorizontalSplitMethod = SplitterGUILayoutType.GetMethod("BeginHorizontalSplit", flags, null, new Type[] { SplitterStateProxy.SplitterStateType, typeof(GUILayoutOption[]) }, null);
                }
                return _BeginHorizontalSplitMethod;
            }
        }

        private static MethodInfo _EndHorizontalSplitMethod = null;

        private static MethodInfo EndHorizontalSplitMethod{
            get{
                if(_EndHorizontalSplitMethod == null){
                    _EndHorizontalSplitMethod = SplitterGUILayoutType.GetMethod("EndHorizontalSplit", flags, null, Type.EmptyTypes, null);
                }
                return _EndHorizontalSplitMethod;
            }
        }



        public static void BeginVerticalSplit(SplitterStateProxy splitterState, params GUILayoutOption[] options)
        {
            BeginVerticalSplitMethod.Invoke(null, new object[] { splitterState.raw, options });
        }

        public static void EndVerticalSplit()
        {
            EndVerticalSplitMethod.Invoke(null, Type.EmptyTypes);
        }
        public static void BeginHorizontalSplit(SplitterStateProxy splitterState, params GUILayoutOption[] options)
        {
            BeginHorizontalSplitMethod.Invoke(null, new object[] { splitterState.raw, options });
        }

        public static void EndHorizontalSplit()
        {
            EndHorizontalSplitMethod.Invoke(null, Type.EmptyTypes);
        }


        public static void EnableTrace(){
            var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,defineSymbols + ";" + "LIT_TASK_TRACE");
        }

        public static void DisableTrace(){
            var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var array = defineSymbols.Split(';').Where((value)=>{
                return value != "LIT_TASK_TRACE";
            });
            defineSymbols = string.Join(";",array);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,defineSymbols);
        }

        private const string PREF_ENABLE_STACK = "MS.LitTask.Prefs.EnableStack";
        public static bool EnableStack{
            get{
                return Diagnostics.Trace.EnableStack;
            }set{
                Diagnostics.Trace.EnableStack = value;
                EditorPrefs.SetBool(PREF_ENABLE_STACK,value);
            }
        }


        [InitializeOnLoadMethod]
        private static void InitializeOnLoad(){
            Diagnostics.Trace.EnableStack = EditorPrefs.GetBool(PREF_ENABLE_STACK,false);
        }


   


    }
}
