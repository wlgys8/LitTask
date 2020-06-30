
using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics;

namespace MS.Async.Diagnostics{


    public interface ITracableObject
    {
        string DebugNameId{
            get;
        }    
    }

    public static class Trace
    {
        private static Dictionary<int,TraceItem> _taskSourceDict = new Dictionary<int, TraceItem>();

        public static bool Enabled{
            get{
                #if LIT_TASK_TRACE
                return true;
                #else
                return false;
                #endif
            }
        }

        public static bool EnableStack{
            get;set;
        }

        private static string GetStackTrace(){
            StackTrace stack = new StackTrace(1,true);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach(var frame in stack.GetFrames()){
                var method = frame.GetMethod();
                var skip = method.GetCustomAttributes(typeof(System.Diagnostics.DebuggerHiddenAttribute),true).Length > 0;
                if(skip){
                    continue;
                }
                var line = string.Format($"at {method.DeclaringType.FullName}.{method.Name}({frame.GetFileName()}:{frame.GetFileLineNumber()})");
                builder.AppendLine(line);
            }
            return builder.ToString();
        }

        private const string DEFAULT_EMPTY_STACK_STACE = "stack trace disabled";


        [Conditional("LIT_TASK_TRACE")]
        [DebuggerHidden]
        public static void TraceAllocation(ITracableObject source){
            var hashCode = source.GetHashCode();
            TraceItem item = null;
            if(_taskSourceDict.TryGetValue(hashCode,out item)){
                item.ResetTarget(source);
            }else{
                item = new TraceItem(source);
                _taskSourceDict.Add(hashCode,item);
            }
            if(EnableStack){
                item.StackTrace = GetStackTrace();
            }else{
                item.StackTrace = DEFAULT_EMPTY_STACK_STACE;
            }
        }

        [Conditional("LIT_TASK_TRACE")]
        public static void TraceReturn(ITracableObject source){
            _taskSourceDict.Remove(source.GetHashCode());
        }

        public static int[] ListTaskSourceIds(){
            return _taskSourceDict.Where((kv)=>{
                return kv.Value.IsAlive;
            }).Select((kv)=>{
                return kv.Key;
            }).ToArray();
        }

        public static TraceItem GetTraceItem(int id){
            return _taskSourceDict[id];
        }


        public class TraceItem{

            private WeakReference _valueSourceReference;
            private string _stackTrace;

            private string _debugNameId;

            public TraceItem(ITracableObject source){
                ResetTarget(source);
            }

            public string StackTrace{
                get;internal set;
            }
            internal void ResetTarget(ITracableObject source){
                _debugNameId = source.DebugNameId;
                _valueSourceReference = new WeakReference(source);                
            }

            public string Name{
                get{
                    return _debugNameId;
                }
            }

            public bool IsAlive{
                get{
                    return _valueSourceReference.IsAlive;
                }
            }
        }

    }
}
