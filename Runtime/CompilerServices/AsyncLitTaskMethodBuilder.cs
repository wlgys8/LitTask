using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;


namespace MS.Async.CompilerServices{

    public class AsyncLitTaskMethodBuilder:ILitTaskValueSource, Diagnostics.ITracableObject
    {
        private static Stack<AsyncLitTaskMethodBuilder> _pool = new Stack<AsyncLitTaskMethodBuilder>();
        private static TokenAllocator _tokenAllocator = new TokenAllocator();
        [DebuggerHidden]
        public static AsyncLitTaskMethodBuilder Create(){
            AsyncLitTaskMethodBuilder builder = null;
            if(_pool.Count == 0){
                builder = new AsyncLitTaskMethodBuilder();
            }else{
                builder = _pool.Pop();
            }
            builder._token = _tokenAllocator.Next();
            Diagnostics.Trace.TraceAllocation(builder);
            return builder;

        }

        private IStateMachineBox _stateMachineBox;
        private short _token;

        private ValueSourceStatus _status;

        private Exception _exception;

        private bool _shouldForget = false;
        private Action _continuation = null;


        private void ValidateToken(short token){
            if(_token == 0 || _token != token){
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
        private void ReturnToPool(){
            if(_token == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            _token = 0;
            if(_stateMachineBox != null){
                _stateMachineBox.Return();
                _stateMachineBox = null;
            }
            _status = ValueSourceStatus.Pending;
            _shouldForget = false;
            _continuation = null;
            _pool.Push(this);
            Diagnostics.Trace.TraceReturn(this);
        }

        private void ValidateToken(){
            if(_token == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        [DebuggerHidden]
        public void SetResult(){
            _status = ValueSourceStatus.Succeed;
            if(_continuation != null){
                // get result will be called in continuation
                _continuation();
            }else{
                if(_shouldForget){
                    ReturnToPool();
                }else{
                    //maybe leak
                }
            }
        }

        [DebuggerHidden]
        public void SetException(Exception exception){
            if(exception is LitCancelException litCancelException){
                _status = ValueSourceStatus.Canceled;
            }else{
                _status = ValueSourceStatus.Faulted;
            }
            _exception = exception;
            if(_continuation != null){
                _continuation();
            }else{
                if(_shouldForget){
                    try{
                        if(_status == ValueSourceStatus.Faulted){
                            throw _exception;
                            // throw new AggregateException(_exception);
                        }
                    }finally{
                        ReturnToPool();
                    }
                }else{
                    //maybe leak
                }
            }
        }

        [DebuggerHidden]        
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        private void CreateStateMachineBoxIfNot<TStateMachine>(ref TStateMachine stateMachine) 
        where TStateMachine : IAsyncStateMachine{
            if(_stateMachineBox == null){
                var stateMachineBox = StateMachineBox<TStateMachine>.Allocate(ref stateMachine);
                _stateMachineBox = stateMachineBox;
            }
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter,TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine{
            CreateStateMachineBoxIfNot(ref stateMachine);
            awaiter.OnCompleted(_stateMachineBox.MoveNext);
        }

        [DebuggerHidden]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine{
            CreateStateMachineBoxIfNot(ref stateMachine);
            awaiter.UnsafeOnCompleted(_stateMachineBox.MoveNext);
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
        protected void ThrowCancellationOrExceptionIfNeed(){
            if(_status == ValueSourceStatus.Faulted){
                throw _exception;
                // throw new AggregateException(_exception);
            }else if(_status == ValueSourceStatus.Canceled){
                throw _exception;
            }           
        }
        public void GetResult(short token)
        {
            ValidateToken(token);
            try{
                ThrowCancellationOrExceptionIfNeed();
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
        }

        public void Forget(short token)
        {
            ValidateToken(token);
            _shouldForget = true;
            if(_status != ValueSourceStatus.Pending){
                ReturnToPool();
            }
        }

        public LitTask Task{
            get{
                return new LitTask(this,_token);
            }
        }

        public string DebugNameId{
            get{
                return this.GetType().Name;
            }
        }
    }


    public class AsyncLitTaskMethodBuilder<T>:ILitTaskValueSource<T>{
        private static Stack<AsyncLitTaskMethodBuilder<T>> _pool = new Stack<AsyncLitTaskMethodBuilder<T>>();
        private static TokenAllocator _tokenAllocator = new TokenAllocator();

        public static AsyncLitTaskMethodBuilder<T> Create(){
            AsyncLitTaskMethodBuilder<T> res = null;
            if(_pool.Count == 0){
                res = new AsyncLitTaskMethodBuilder<T>();
            }else{
                res = _pool.Pop();
            }
            res._token = _tokenAllocator.Next();
            return res;
        }

        private IStateMachineBox _stateMachineBox;
        private T _result;
        private Exception _exception;
        private short _token;
        private ValueSourceStatus _status;
        private Action _continuation;
        private bool _shouldForget;

        private void ReturnToPool(){
            if(_token == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            _token = 0;
            if(_stateMachineBox != null){
                _stateMachineBox.Return();
                _stateMachineBox = null;
            }
            
            _result = default;
            _exception = null;
            _continuation = null;
            _shouldForget = false;
            _status = ValueSourceStatus.Pending;
            _pool.Push(this);
        }

        private void ValidateToken(short token){
            if(_token == 0 || _token != token){
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
        
  
        [DebuggerHidden]
        public void SetResult(T result){
            _result = result;
            _status = ValueSourceStatus.Succeed;
            if(_continuation != null){
                _continuation();
            }else{
                if(_shouldForget){
                    ReturnToPool();
                }else{
                    //maybe leak
                }
            }
        }

        [DebuggerHidden]
        public void SetException(Exception exception){
            if(exception is LitCancelException litCancelException){
                _status = ValueSourceStatus.Canceled;
            }else{
                _status = ValueSourceStatus.Faulted;
            }
            _exception = exception;
            if(_continuation != null){
                _continuation();
            }else{
                if(_shouldForget){
                    try{
                        if(_status == ValueSourceStatus.Faulted){
                            throw _exception;
                            // throw new AggregateException(_exception);
                        }
                    }finally{
                        ReturnToPool();
                    }
                }else{
                    //maybe leak
                }
            }
        }

        [DebuggerHidden]        
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        private void CreateStateMachineBoxIfNot<TStateMachine>(ref TStateMachine stateMachine) 
        where TStateMachine : IAsyncStateMachine{
            if(_stateMachineBox == null){
                var stateMachineBox = StateMachineBox<TStateMachine>.Allocate(ref stateMachine);
                _stateMachineBox = stateMachineBox;
            }
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter,TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine{
            CreateStateMachineBoxIfNot(ref stateMachine);
            awaiter.OnCompleted(_stateMachineBox.MoveNext);
        }

        [DebuggerHidden]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine{
            CreateStateMachineBoxIfNot(ref stateMachine);
            awaiter.UnsafeOnCompleted(_stateMachineBox.MoveNext);
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        protected void ThrowCancellationOrExceptionIfNeed(){
            if(_status == ValueSourceStatus.Faulted){
                throw _exception;
                // throw new AggregateException(_exception);
            }else if(_status == ValueSourceStatus.Canceled){
                throw _exception;
            }           
        }
        public T GetResult(short token)
        {
            ValidateToken(token);
            try{
                ThrowCancellationOrExceptionIfNeed();
                return _result;
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
        }

        public void Forget(short token)
        {
            ValidateToken(token);
            _shouldForget = true;
            if(_status != ValueSourceStatus.Pending){
                try{
                    // ThrowCancellationOrExceptionIfNeed();
                }finally{
                    //if completed. return to poll
                    ReturnToPool();
                }
            }
        }

        public LitTask<T> Task{
            get{
                return new LitTask<T>(this,_token);
            }
        }
    }


    public struct LitTaskAwaiter: ICriticalNotifyCompletion{

        private LitTask _task;
        public LitTaskAwaiter(in LitTask task){
            _task = task;
        }

        [DebuggerHidden]
        public void GetResult() {
            _task.GetResult();
        }
    
        public bool IsCompleted{
            get{
                return _task.IsCompleted;
            }
        }
    
        [DebuggerHidden]
        public void OnCompleted(Action continuation) { 
            _task.OnCompleted(continuation);
        }

        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation){
            _task.OnCompleted(continuation);
        }
    }

    public struct LitTaskAwaiter<T>: ICriticalNotifyCompletion{

        private LitTask<T> _task;
        public LitTaskAwaiter(in LitTask<T> task){
            _task = task;
        }

        [DebuggerHidden]
        public T GetResult() {
            return _task.GetResult();
        }
    
        public bool IsCompleted{
            get{
                return _task.IsCompleted;
            }
        }
    
        [DebuggerHidden]
        public void OnCompleted(Action continuation) { 
            _task.OnCompleted(continuation);
        }

        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation){
            _task.OnCompleted(continuation);
        }
    }

}
