﻿using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

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

        private Action _continuation = null;
        private Action<LitTaskResult> _continueWithAction = null;
        private bool _runWithContinue = false;



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
            _continuation = null;
            _continueWithAction = null;
            _runWithContinue = false;
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
                
                if(_runWithContinue){
                    //run in async mode, but use continue instead of await
                    try{
                        if(_continueWithAction != null){
                            try{
                                _continueWithAction(new LitTaskResult(null));
                            }catch(System.Exception e){
                                Utilities.UnityLoopsHelper.ThrowAsync(e);
                            }
                        }else{
                            //missing continueWithAction
                        }
                    }finally{
                        ReturnToPool();
                    }
                }else{
                    //run in sync mode. user will call continue to finish to task
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
                if(_runWithContinue){
                    try{
                        if(_continueWithAction != null){
                            try{
                                _continueWithAction(new LitTaskResult(_exception));
                            }catch(Exception e){
                                Utilities.UnityLoopsHelper.ThrowAsync(e);
                            }
                        }else{
                            //continue without handle exception. so throw exception async
                            if(_status == ValueSourceStatus.Faulted){
                                Utilities.UnityLoopsHelper.ThrowAsync(exception);
                            }
                        }
                    }finally{
                        ReturnToPool();
                    }
                }else{
                    //run in sync mode. user will call continue to finish to task
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
                ExceptionDispatchInfo.Capture(_exception).Throw();
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

        public void Continue(short token,Action<LitTaskResult> action){
            ValidateToken(token);
            _continueWithAction = action;
            _runWithContinue = true;
            if(_status != ValueSourceStatus.Pending){
                if(_continueWithAction != null){
                    try{
                        _continueWithAction(new LitTaskResult(_exception));
                    }catch(System.Exception e){
                        Utilities.UnityLoopsHelper.ThrowAsync(e);
                    }finally{
                        ReturnToPool();
                    }
                }else{
                    try{
                        if(_status == ValueSourceStatus.Faulted){
                            Utilities.UnityLoopsHelper.ThrowAsync(_exception);
                        }
                    }finally{
                        ReturnToPool();
                    }
                }
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


    public class AsyncLitTaskMethodBuilder<T>:ILitTaskValueSource<T>,Diagnostics.ITracableObject{
        private static Stack<AsyncLitTaskMethodBuilder<T>> _pool = new Stack<AsyncLitTaskMethodBuilder<T>>();
        private static TokenAllocator _tokenAllocator = new TokenAllocator();

        public static AsyncLitTaskMethodBuilder<T> Create(){
            AsyncLitTaskMethodBuilder<T> builder = null;
            if(_pool.Count == 0){
                builder = new AsyncLitTaskMethodBuilder<T>();
            }else{
                builder = _pool.Pop();
            }
            builder._token = _tokenAllocator.Next();
            Diagnostics.Trace.TraceAllocation(builder);
            return builder;
        }

        private IStateMachineBox _stateMachineBox;
        private T _result;
        private Exception _exception;
        private short _token;
        private ValueSourceStatus _status;
        private Action _continuation;
        private Action<LitTaskResult<T>> _continueWithAction;
        private bool _runWithContinue = false;

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
            _continueWithAction = null;
            _runWithContinue = false;
            _status = ValueSourceStatus.Pending;
            _pool.Push(this);
            Diagnostics.Trace.TraceReturn(this);
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
                if(_runWithContinue){
                    try{
                        if(_continueWithAction != null){
                            try{
                                _continueWithAction(new LitTaskResult<T>(result));
                            }catch(System.Exception e){
                                Utilities.UnityLoopsHelper.ThrowAsync(e);
                            }
                        }else{
                            //missing continueWithAction
                        }
                    }finally{
                        ReturnToPool();
                    }
                }else{
                    //run in sync mode. user will call continue to finish to task
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
                if(_runWithContinue){
                    try{
                        if(_continueWithAction != null){
                            try{
                                _continueWithAction(new LitTaskResult<T>(_exception));
                            }catch(System.Exception e){
                                Utilities.UnityLoopsHelper.ThrowAsync(e);
                            }
                        }else{
                            if(_status == ValueSourceStatus.Faulted){
                                Utilities.UnityLoopsHelper.ThrowAsync(_exception);
                            }
                        }
                    }finally{
                        ReturnToPool();
                    }
                }else{
                    //run in sync mode. user will call continue to finish to task
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
                ExceptionDispatchInfo.Capture(_exception).Throw();
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

        public void Continue(short token,Action<LitTaskResult<T>> action)
        {
            ValidateToken(token);
            _continueWithAction = action;
            _runWithContinue = true;
             if(_status != ValueSourceStatus.Pending){
                if(_continueWithAction != null){
                    try{
                        if(_status == ValueSourceStatus.Succeed){
                            _continueWithAction(new LitTaskResult<T>(_result));
                        }else{
                            _continueWithAction(new LitTaskResult<T>(_exception));
                        }
                    }catch(System.Exception e){
                        Utilities.UnityLoopsHelper.ThrowAsync(e);
                    }finally{
                        ReturnToPool();
                    }
                }else{
                    try{
                        if(_status == ValueSourceStatus.Faulted){
                            Utilities.UnityLoopsHelper.ThrowAsync(_exception);
                        }
                    }finally{
                        ReturnToPool();
                    }
                }
            }           
        }
        public LitTask<T> Task{
            get{
                return new LitTask<T>(this,_token);
            }
        }

        public string DebugNameId{
            get{
                return this.GetType().Name;
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
