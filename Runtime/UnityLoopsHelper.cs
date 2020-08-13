using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MS.Async.Utilities{
    internal class UnityLoopsHelper:MonoBehaviour
    {

        private static List<Action> _updates = new List<Action>();

        public static void OnceUpdate(Action update){
            #if UNITY_EDITOR
            if(Application.isPlaying){
                _updates.Add(update);
            }else{
                 UnityEditor.EditorApplication.delayCall += ()=>{
                     try{
                        update();
                     }catch(System.Exception e){
                         Debug.LogException(e);
                     }
                 };
            }
            #else
                _updates.Add(update);
            #endif
        }

        public static void ThrowAsync(Exception exception){
            UnityEngine.Debug.LogException(exception);
        }

        [RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AppLaunch(){
            var instance = new GameObject("LitTaskLoops").AddComponent<UnityLoopsHelper>();
            GameObject.DontDestroyOnLoad(instance.gameObject);
        }



        void LateUpdate(){
            while(_updates.Count > 0){
                try{
                    var action = _updates[0];
                    action();
                }catch(System.Exception e){
                    Debug.LogException(e);
                }finally{
                    _updates.RemoveAt(0);
                }
            }
        }


    }
}
