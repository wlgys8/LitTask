using System.Collections.Generic;


namespace MS.Async{
    using Diagnostics;


    public class TaskValueSourcePool<T> where T:ILitTaskValueSourceBase
    {
        private static Stack<T> _pool = new Stack<T>();
        public static T Allocate(){
            T ret = default(T);
            if(_pool.Count == 0){
                ret = (T)System.Activator.CreateInstance(typeof(T));
            }else{
                ret = _pool.Pop();
            }
            return ret;
        }       

        public static void Return(T source){
            _pool.Push(source);
        }
    }


}
