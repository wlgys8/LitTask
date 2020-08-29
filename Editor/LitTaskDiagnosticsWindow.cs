using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MS.Async.Editor{
    using Diagnostics;
    public class LitTaskDiagnosticsWindow : EditorWindow
    {


        private static object _splitState;

        private static GUIStyle _stacktraceSnapStyle;



        [MenuItem("Window/LitTask/Diagnostics")]
        public static void Open(){
            var win = EditorWindow.GetWindow<LitTaskDiagnosticsWindow>();
            win.titleContent = new GUIContent("LitTaskDiagnoticsWindow");
            win.Show();
        }

        private static GUIStyle stacktraceSnapStyle{
            get{
                if(_stacktraceSnapStyle == null){
                    _stacktraceSnapStyle = new GUIStyle(EditorStyles.label);
                    _stacktraceSnapStyle.stretchHeight = false;
                }
                return _stacktraceSnapStyle;
            }
        }

        private Vector2[] _contentScrolls;

        private Trace.TraceItem _selectedItem = null;

        private EditorHelper.SplitterStateProxy _headerSplitState;

        private EditorHelper.SplitterStateProxy _contentSplitState;

        private EditorHelper.SplitterStateProxy headerSplitState{
            get{
                if(_headerSplitState == null){
                    _headerSplitState = new EditorHelper.SplitterStateProxy(new float[]{30,30,30},new int[]{100,100,100},null);
                }
                return _headerSplitState;
            }
        }

        private EditorHelper.SplitterStateProxy contentSplitState{
            get{
                if(_contentSplitState == null){
                    _contentSplitState = new EditorHelper.SplitterStateProxy(new float[]{70,30},new int[]{100,100},null);
                }
                return _contentSplitState;
            }
        }

        void OnEnable(){
            _headerSplitState = null;
            _contentSplitState = null;
            headerSplitState.splitSize = 6;
            Trace.onTraceUpdate += OnTraceUpdate;
        }

        void OnDisable(){
            Trace.onTraceUpdate -= OnTraceUpdate;
        }

        private void OnTraceUpdate(){
            this.Repaint();
        }

        private void OnGUI() {
            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);
            DrawToolButtons();
            DrawHeaders();
            if(_contentScrolls == null || _contentScrolls.Length != 2){
                _contentScrolls = new Vector2[]{Vector2.zero,Vector2.zero};
            }
            EditorHelper.BeginVerticalSplit(contentSplitState);
            _contentScrolls[0] = EditorGUILayout.BeginScrollView(_contentScrolls[0]);
            var ids = Trace.ListTaskSourceIds();
            foreach(var id in ids){
                var item = Trace.GetTraceItem(id);
                DrawTraceItem(item);
                if(_selectedItem  == null){
                    _selectedItem = item;
                }
            }
            EditorGUILayout.EndScrollView();

            _contentScrolls[1] = EditorGUILayout.BeginScrollView(_contentScrolls[1],EditorStyles.helpBox);
            DrawStackTrace();
            EditorGUILayout.EndScrollView();
            EditorHelper.EndVerticalSplit();

            EditorGUI.EndDisabledGroup();
        }

        private void DrawToolButtons(){
            GUILayout.BeginHorizontal(EditorStyles.toolbar,GUILayout.ExpandWidth(true));
            EditorGUI.BeginChangeCheck();
            var enableTrace = GUILayout.Toggle(Trace.Enabled,"Enable Trace",EditorStyles.toolbarButton,GUILayout.ExpandWidth(false));
            if(EditorGUI.EndChangeCheck()){
                if(enableTrace){
                    EditorHelper.EnableTrace();
                }else{
                    EditorHelper.DisableTrace();
                }
            }
            EditorGUI.BeginChangeCheck();
            var enableStack = GUILayout.Toggle(EditorHelper.EnableStack,"Enable Stack",EditorStyles.toolbarButton,GUILayout.ExpandWidth(false));
            if(EditorGUI.EndChangeCheck()){
                EditorHelper.EnableStack = enableStack;
            }
            GUILayout.EndHorizontal();

        }
        private void SplitLine(){
            var rect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(rect.xMax,rect.y,1,rect.height), new Color ( 0.3f,0.3f,0.3f, 1 ) );
        }
        private void DrawHeaders(){
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorHelper.BeginHorizontalSplit(headerSplitState);
            GUILayout.Label("NameId");
            SplitLine();

            GUILayout.Label("Status");
            SplitLine();
            GUILayout.Label("StackTrace");
            EditorHelper.EndHorizontalSplit();
            GUILayout.EndHorizontal();
        }

        private void DrawTraceItem(Trace.TraceItem item){
            var realSizes = headerSplitState.realSizes;
            var original = GUI.backgroundColor;

            if(_selectedItem == item){
                GUI.backgroundColor = Color.grey;
            }
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(item.Name,GUILayout.Width(realSizes[0]));
            GUILayout.Label(item.IsAlive?"Alive":"Dead",GUILayout.Width(realSizes[1]));
            EditorGUILayout.LabelField(item.StackTrace,GUILayout.Width(realSizes[2]));
            GUILayout.EndHorizontal();
            var rect = GUILayoutUtility.GetLastRect();
            if(GUI.Button(rect,GUIContent.none,GUIStyle.none)){
                _selectedItem = item;
            }
            GUI.backgroundColor = original;
        }

        private Vector2 _progress;
        private void DrawStackTrace(){
            if(_selectedItem != null){
                // GUI.BeginScrollView(this.position,_progress,)
                var position = this.position;
                // GUILayout.BeginArea(new Rect(0,0,position.width,200));
                // _progress = GUILayout.BeginScrollView(_progress,true,true,GUILayout.Height(200));
                GUILayout.BeginVertical();
                var labelStyle = EditorStyles.label;
                var original = labelStyle.wordWrap;
                labelStyle.wordWrap = true;
                var content = new GUIContent(_selectedItem.StackTrace);
                var height = labelStyle.CalcHeight(content,this.position.width);
                // GUILayout.LabelField(_selectedItem.StackTrace,labelStyle,GUILayout.Width(this.position.width),GUILayout.ExpandHeight(true));
                GUILayout.Label(content,labelStyle,GUILayout.Width(this.position.width),GUILayout.Height(height));

                labelStyle.wordWrap = original;
                GUILayout.EndVertical();
                // GUILayout.EndScrollView();
                // GUILayout.EndArea();
            }
        }
    }
}
