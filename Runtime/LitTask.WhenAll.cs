using System;
using System.Collections.Generic;
using System.Linq;

namespace MS.Async{
    public partial struct LitTask
    {

        public static async LitTask WhenAll(IEnumerable<LitTask> tasks){
            if(tasks.Count() == 0){
                return;
            }
            var source = WhenAllSource.Get(tasks);
            await new LitTask(source,source.Token);
        }

        public static LitTask WhenAll(params LitTask[] tasks){
            if(tasks.Length == 0){
                return default;
            }
            return WhenAll(new List<LitTask>(tasks));
        }
    }


    internal class WhenAllSource : ILitTaskValueSource,Diagnostics.ITracableObject
    {
        private static CompilerServices.TokenAllocator _tokenAllocator = new CompilerServices.TokenAllocator();
        private static Stack<WhenAllSource> _pool = new Stack<WhenAllSource>();
        public static WhenAllSource Get(IEnumerable<LitTask> tasks){
            WhenAllSource source = null;
            if(_pool.Count == 0){
                source = new WhenAllSource();
            }else{
                source = _pool.Pop();
            }
            source.Initialize(tasks,_tokenAllocator.Next());
            return source;
        }

        private short _token;
        private IEnumerable<LitTask> _tasks;
        private Action _continuation;

        private int _completedCount = 0;

        private ValueSourceStatus _status = ValueSourceStatus.Pending;
        private Exception _exception;

        private WhenAllSource(){
        }

        private void CompleteSubTask(){
            this._completedCount ++;
            if(this._completedCount == this._tasks.Count()){
                var continuation = this._continuation;
                continuation();
            }
        }

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
            _completedCount = 0;
            _status = ValueSourceStatus.Pending;
            _exception = null;
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

        public string DebugNameId {
            get{
                return this.GetType().Name;
            }
        }

        public void Forget(short token)
        {
            ValidateToken(token);
            StartTasksAndForget();
            ReturnToPool();
        }

        public void GetResult(short token)
        {
            ValidateToken(token);
            try{
                if(_status == ValueSourceStatus.Canceled){
                    throw _exception;
                }else if(_status == ValueSourceStatus.Faulted){
                    throw new AggregateException(_exception);
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
            foreach(var task in _tasks){
                RunTask(task).Forget();
            }
        }

        private async LitTask RunTask(LitTask task){
            
            try{
                await task;
            }catch(LitCancelException cancelException){
                if(_status != ValueSourceStatus.Faulted){
                    _status = ValueSourceStatus.Canceled;
                }
                _exception = cancelException;
            }catch(Exception exception){
                _status = ValueSourceStatus.Faulted;
                _exception =exception;
            }finally{
                this.CompleteSubTask();
            }
        }

        private void StartTasksAndForget(){
            foreach(var task in _tasks){
                RunTaskVoid(task).Forget();
            }
        }

        private async LitTask RunTaskVoid(LitTask task){
            await task;
        }
    }
}