﻿using System.Diagnostics;
using System;

namespace MS.Async{
    using CompilerServices;
    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(AsyncLitTaskMethodBuilder))]
    public partial struct LitTask
    {
        
        private ILitTaskValueSource _valueSource;
        private short _token;

        public LitTask(ILitTaskValueSource valueSource,short token){
            _valueSource = valueSource;
            _token = token;
        }

        public void Forget(){
            if(_valueSource == null){
                return;
            }
            _valueSource.Forget(_token);
        }
        

        [DebuggerHidden]
        internal void GetResult(){
            if(_valueSource == null){
                return;
            }
            _valueSource.GetResult(_token);
        }

        internal bool IsCompleted{
            get{
                if(_valueSource == null){
                    return true;
                }
                var ret = _valueSource.GetStatus(_token) != ValueSourceStatus.Pending;
                return ret;
            }
        }

        internal void OnCompleted(Action continuation){
            if(_valueSource == null){
                return;
            }
            _valueSource.OnCompleted(continuation,_token);
        }

        public LitTaskAwaiter GetAwaiter() {
            return new LitTaskAwaiter(this);
        }
    }

    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(AsyncLitTaskMethodBuilder<>))]
    public struct LitTask<T>{
        private ILitTaskValueSource<T> _valueSource;

        private T _result;
        private short _token;

        public LitTask(ILitTaskValueSource<T> valueSource,short token){
            _valueSource = valueSource;
            _token = token;
            _result = default;
        }

        public LitTask(T result){
            _valueSource = null;
            _token = 0;
            _result = result;
        }

        public void Forget(){
            if(_valueSource == null){
                return;
            }
            _valueSource.Forget(_token);
        }
        

        [DebuggerHidden]
        internal T GetResult(){
            if(_valueSource == null){
                return _result;
            }
            return _valueSource.GetResult(_token);
        }

        internal bool IsCompleted{
            get{
                if(_valueSource == null){
                    return true;
                }
                var ret = _valueSource.GetStatus(_token) != ValueSourceStatus.Pending;
                return ret;
            }
        }

        internal void OnCompleted(Action continuation){
            if(_valueSource == null){
                return;
            }
            _valueSource.OnCompleted(continuation,_token);
        }

        /// <summary>
        /// This method is intended for compiler use. Don't call it in your code.
        /// </summary>
        public LitTaskAwaiter<T> GetAwaiter() {
            return new LitTaskAwaiter<T>(this);
        }        
    }

}
