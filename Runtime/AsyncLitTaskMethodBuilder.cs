using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;


namespace MS.Async.CompilerServices{

    public class AsyncLitTaskMethodBuilder
    {
        private static Stack<AsyncLitTaskMethodBuilder> _pool = new Stack<AsyncLitTaskMethodBuilder>();
        public static AsyncLitTaskMethodBuilder Create(){
            if(_pool.Count == 0){
                return new AsyncLitTaskMethodBuilder();
            }else{
                return _pool.Pop();
            }
        }

        private IStateMachineBox _stateMachineBox;
        private short _token;

        private void ReturnToPool(){
            _token = 0;
            _stateMachineBox = null;
            _pool.Push(this);
        }

        private void ValidateToken(){
            if(_token == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        [DebuggerHidden]
        public void SetResult(){
            try{
                if(_stateMachineBox != null){
                    _stateMachineBox.SetResult(_token);
                }
            }finally{
                ReturnToPool();
            }
        }

        [DebuggerHidden]
        public void SetException(Exception exception){
            try{
                if(_stateMachineBox != null){
                    _stateMachineBox.SetException(exception,_token);
                }else{
                    throw exception;
                }
            }finally{
                ReturnToPool();
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
                _token = stateMachineBox.Token;
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
            //dont use boxed stateMachine
        }

        public LitTask Task{
            get{
                return new LitTask(_stateMachineBox,_token);
            }
        }
    }


    public class AsyncLitTaskMethodBuilder<T>{
        private static Stack<AsyncLitTaskMethodBuilder<T>> _pool = new Stack<AsyncLitTaskMethodBuilder<T>>();
        public static AsyncLitTaskMethodBuilder<T> Create(){
            if(_pool.Count == 0){
                return new AsyncLitTaskMethodBuilder<T>();
            }else{
                return _pool.Pop();
            }
        }

        private IStateMachineBox<T> _stateMachineBox;
        private short _token;

        private void ReturnToPool(){
            _token = 0;
            _stateMachineBox = null;
            _pool.Push(this);
        }

        private void ValidateToken(){
            if(_token == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        [DebuggerHidden]
        public void SetResult(T result){
            try{
                if(_stateMachineBox != null){
                    _stateMachineBox.SetResult(result,_token);
                }
            }finally{
                ReturnToPool();
            }
        }

        [DebuggerHidden]
        public void SetException(Exception exception){
            try{
                if(_stateMachineBox != null){
                    _stateMachineBox.SetException(exception,_token);
                }else{
                    throw exception;
                }
            }finally{
                ReturnToPool();
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
                var stateMachineBox = StateMachineBox<TStateMachine,T>.Allocate(ref stateMachine);
                _stateMachineBox = stateMachineBox;
                _token = stateMachineBox.Token;
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
            //dont use boxed stateMachine
        }

        public LitTask<T> Task{
            get{
                return new LitTask<T>(_stateMachineBox,_token);
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
