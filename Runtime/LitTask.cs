using System.Diagnostics;
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

        /// <summary>
        /// forget will eat all exceptions.
        /// </summary>
        public void Forget(){
            if(_valueSource == null){
                return;
            }
            _valueSource.Continue(_token,null);
        }
     
        /// <summary>
        /// different from Forget.
        /// 
        /// Action will be invoked after the task completed
        /// </summary>
        public void ContinueWith(Action<LitTaskResult> action){
            if(_valueSource == null){
                return;
            }
            _valueSource.Continue(_token,action);            
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
            _valueSource.Continue(_token,null);
        }
        
        public void ContinueWith(Action<LitTaskResult<T>> action){
           if(_valueSource == null){
                return;
            }
            _valueSource.Continue(_token,action);            
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


    public struct LitTaskResult{

        private Exception _exception;
        private bool _isCancelled;

        internal LitTaskResult(Exception exception){
            _exception = null;
            if(exception == null){
                _isCancelled = false;
            }else{
                _isCancelled = exception is LitCancelException;
                if(!_isCancelled){
                    _exception = exception;
                }
            }
        }

        public bool IsCancelled{
            get{
                return _isCancelled;
            }
        }

        public bool IsSuccess{
            get{
                return _exception == null && !_isCancelled;
            }
        }

        public bool IsFaulted{
            get{
                return _exception != null;
            }
        }


        public Exception Exception{
            get{
                return _exception;
            }
        }
    }

    public struct LitTaskResult<T>{

        private T _value;
        private Exception _exception;

        private bool _isCancelled;

        public LitTaskResult(T value){
            _value = value;
            _exception = null;
            _isCancelled = false;
        }

        public LitTaskResult(Exception exception){
            _value = default(T);
            _isCancelled = exception is LitCancelException;
            _exception = _isCancelled?null:exception;
        }

        public T Value{
            get{
                return _value;
            }
        }

        public bool IsCancelled{
            get{
                return _isCancelled;
            }
        }

        public bool IsSuccess{
            get{
                return _exception == null && !_isCancelled;
            }
        }

        public bool IsFaulted{
            get{
                return _exception != null;
            }
        }

        public Exception Exception{
            get{
                return _exception;
            }
        }
    }

}
