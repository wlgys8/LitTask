using System.Collections.Generic;
using System;
using System.Linq;

namespace MS.Async{
    public partial struct LitTask 
    {
        
        public static async LitTask<WhenAnyResult> WhenAny(IEnumerable<LitTask> tasks){
            if(tasks.Count() == 0){
                return new WhenAnyResult(0,ValueSourceStatus.Succeed,null);
            }
            var source = LitTaskWhenAnySource.Get(tasks);
            return await new LitTask<WhenAnyResult>(source,source.Token);
        }

        public static LitTask<WhenAnyResult> WhenAny(params LitTask[] tasks){
            if(tasks.Length == 0){
                return new LitTask<WhenAnyResult>(new WhenAnyResult(0,ValueSourceStatus.Succeed,null));
            }
            return WhenAny(new List<LitTask>(tasks));
        }

        public struct WhenAnyResult{
            private int _firstCompletedTaskIndex;
            private ValueSourceStatus _status;
            private Exception _exception;

            public WhenAnyResult(int firstCompletedTaskIndex,ValueSourceStatus status,Exception exception){
                _firstCompletedTaskIndex = firstCompletedTaskIndex;
                _status = status;
                _exception = exception;
            }

            public int FirstCompletedTaskIndex{
                get{
                    return _firstCompletedTaskIndex;
                }
            }

            public ValueSourceStatus Status{
                get{
                    return _status;
                }
            }

            public Exception Exception{
                get{
                    return _exception;
                }
            }

        }
    }

    internal class LitTaskWhenAnySource : ILitTaskValueSource<LitTask.WhenAnyResult>,Diagnostics.ITracableObject
    {

        private static CompilerServices.TokenAllocator _tokenAllocator = new CompilerServices.TokenAllocator();
        private static Stack<LitTaskWhenAnySource> _pool = new Stack<LitTaskWhenAnySource>();
        public static LitTaskWhenAnySource Get(IEnumerable<LitTask> tasks){
            LitTaskWhenAnySource source = null;
            if(_pool.Count == 0){
                source = new LitTaskWhenAnySource();
            }else{
                source = _pool.Pop();
            }
            source.Initialize(tasks,_tokenAllocator.Next());
            return source;
        }

        private short _token;
        private IEnumerable<LitTask> _tasks;
        private Action _continuation;
        private int _firstCompletedTaskIndex = 0;
        private ValueSourceStatus _status = ValueSourceStatus.Pending;
        private Exception _exception;
        private Action<LitTaskResult<LitTask.WhenAnyResult>> _continueWith;
        private bool _runWithContinue = false;

        private void Initialize(IEnumerable<LitTask> tasks,short token){
            if(_token != 0){
                throw new InvalidOperationException();
            }
            _tasks = tasks;
            _token = token;
        }

        private void ReturnToPool(){
            _token = 0;
            _tasks = null;
            _continuation = null;
            _firstCompletedTaskIndex = 0;
            _status = ValueSourceStatus.Pending;
            _exception = null;
            _continueWith = null;
            _runWithContinue = false;
            _pool.Push(this);
        }

        private void ValidateToken(short token){
            if(_token == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(_token != token){
                throw new DuplicateWaitObjectException();
            }
        }       
        public short Token{
            get{
                return _token;
            }
        }

        public string DebugNameId{
            get{
                return this.GetType().Name;
            }
        }

        public void Continue(short token,Action<LitTaskResult<LitTask.WhenAnyResult>> action)
        {
            ValidateToken(token);
            _continueWith = action;
            _runWithContinue = true;
            StartTasks();
            if(_status != ValueSourceStatus.Pending){
                TryInvokeContinueWithAction();
            }
        }

        public LitTask.WhenAnyResult GetResult(short token)
        {
            ValidateToken(token);
            try{
                if(_status == ValueSourceStatus.Pending){
                    throw new InvalidOperationException();
                }else{
                    return new LitTask.WhenAnyResult(_firstCompletedTaskIndex,_status,_exception);
                }
            }finally{
                ReturnToPool();
            }
        }

        public ValueSourceStatus GetStatus(short token)
        {
            ValidateToken(token);
            return _status;
        }

        public void OnCompleted(Action continuation, short token)
        {
            ValidateToken(token);
            _continuation = continuation;
            StartTasks();
        }

        private void StartTasks(){
            var index = 0;
            foreach(var task in _tasks){
                RunTask(task,index,this._token).Forget();
                index ++;
            }
        }

        private async LitTask RunTask(LitTask task,int taskIndex,short token){
            Exception exception = null;
            try{
                await task;
            }catch(Exception e){
                exception = e;
            }finally{
                if(_token == token && _status == ValueSourceStatus.Pending){
                    if(exception == null){
                        _status = ValueSourceStatus.Succeed;
                    }else if(exception is LitCancelException){
                        _status = ValueSourceStatus.Canceled;
                    }else{
                        _status = ValueSourceStatus.Faulted;
                        _exception = exception;
                    }
                    _firstCompletedTaskIndex = taskIndex;
                    if(_continuation != null){
                        _continuation();
                    }else{
                        if(_runWithContinue){
                            try{
                                TryInvokeContinueWithAction();
                            }finally{
                                this.ReturnToPool();
                            }
                        }else{
                            //run in sync mode. use will call continute to get the result

                        }
                    }
                }
            }
        }

        private void TryInvokeContinueWithAction(){
            if(_continueWith != null){
                _continueWith(new LitTaskResult<LitTask.WhenAnyResult>(new LitTask.WhenAnyResult(_firstCompletedTaskIndex,_status,_exception)));
            }           
        }

    }
}
