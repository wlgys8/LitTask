using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace MS.Async.CompilerServices{

    internal class TokenAllocator{
        private short _token = 0;

        public short Next(){
            do{
                _token ++;
            }while(_token == 0);
            return _token;
        }     
    }

    internal interface IBaseStateMachineBox
    {
        void SetException(Exception e,short token);

        short Token{
            get;
        }
        Action MoveNext{
            get;
        }     
    }

    internal interface IStateMachineBox:IBaseStateMachineBox,ILitTaskValueSource
    {
        void SetResult(short token);

    }

    internal interface IStateMachineBox<TResult>:IBaseStateMachineBox,ILitTaskValueSource<TResult>{
        void SetResult(TResult result,short token);
    }


    internal abstract class BaseStateMachineBox<TStateMachine> : IBaseStateMachineBox where TStateMachine : IAsyncStateMachine
    {
        protected TStateMachine _stateMachine;
        private Action _continuation;
        protected short _token;
        private ValueSourceStatus _status;
        private Exception _exception;
        private bool _shouldForget = false;

        public BaseStateMachineBox(){
            this.MoveNext = ()=>{
                _stateMachine.MoveNext();
            };
        }

        protected void Clear(){
            _token = 0;
            _continuation = null;
            _status = ValueSourceStatus.Pending;
            _stateMachine = default(TStateMachine);
            _exception = null;
        }

        protected void ThrowCancellationOrExceptionIfNeed(){
            if(_status == ValueSourceStatus.Faulted){
                throw new AggregateException(_exception);
            }else if(_status == ValueSourceStatus.Canceled){
                throw _exception;
            }           
        }

        protected void Succeed(){
            _status = ValueSourceStatus.Succeed;
            if(!_shouldForget){
                if(_continuation != null){
                    _continuation();
                }else{
                    //user does not await the task
                }
            }
        }
        

        public Action MoveNext{
            get;private set;
        }

        public short Token{
            get{
                return _token;
            }
        }

        protected void ValidateToken(short token){
            if(_token == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(_token != token){
                throw new InvalidOperationException(string.Format("{0},{1}",_token,token));
            }
        }


        [DebuggerHidden()]
        public virtual void SetException(Exception e,short token){
            ValidateToken(token);
            if(e is LitCancelException litCancelException){
                _status = ValueSourceStatus.Canceled;
            }else{
                _status = ValueSourceStatus.Faulted;
            }
            _exception = e;
            if(!_shouldForget){
                if(_continuation != null){
                    _continuation();
                }else{
                    //user does not await the task
                }
            }
        }

        protected bool shouldForget{
            get{
                return _shouldForget;
            }
        }

        /// <summary>
        /// Forget means GetStatus & OnComplete & GetResult won't be called anymore.
        /// </summary>
        public void Forget(short token){
            ValidateToken(token);
            _shouldForget = true;
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
    }

    internal class StateMachineBox<TStateMachine>:BaseStateMachineBox<TStateMachine>,IStateMachineBox where TStateMachine : IAsyncStateMachine{
        
        private static TokenAllocator _tokenAllocator = new TokenAllocator();

        private static Stack<StateMachineBox<TStateMachine>> _pool = new Stack<StateMachineBox<TStateMachine>>();
        public static StateMachineBox<TStateMachine> Allocate(ref TStateMachine stateMachine){
            StateMachineBox<TStateMachine> ret = null;
            if(_pool.Count == 0){
                ret = new StateMachineBox<TStateMachine>();
            }else{
                ret = _pool.Pop();
            }
            ret.AcquireToken();
            ret._stateMachine = stateMachine;
            return ret;
        }

        private void AcquireToken(){
            _token = _tokenAllocator.Next();
        }

        private void ReturnToPool(){
            this.Clear();
            _pool.Push(this);
            
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

        [DebuggerHidden()]
        public void SetResult(short token){
            ValidateToken(token);
            this.Succeed();
            if(this.shouldForget){
                ReturnToPool();
            }
        }

        [DebuggerHidden()]
        public override void SetException(Exception e,short token){
            base.SetException(e,token);
            if(this.shouldForget){
                ReturnToPool();
            }
        }
    }    

    internal class StateMachineBox<TStateMachine,TResult>:BaseStateMachineBox<TStateMachine>,IStateMachineBox<TResult> where TStateMachine : IAsyncStateMachine{
        private static TokenAllocator _tokenAllocator = new TokenAllocator();

        private static Stack<StateMachineBox<TStateMachine,TResult>> _pool = new Stack<StateMachineBox<TStateMachine,TResult>>();
        public static StateMachineBox<TStateMachine,TResult> Allocate(ref TStateMachine stateMachine){
            StateMachineBox<TStateMachine,TResult> ret = null;
            if(_pool.Count == 0){
                ret = new StateMachineBox<TStateMachine,TResult>();
            }else{
                ret = _pool.Pop();
            }
            ret.AcquireToken();
            ret._stateMachine = stateMachine;
            return ret;
        }

        private TResult _result;

        private void AcquireToken(){
            _token = _tokenAllocator.Next();
        }

        private void ReturnToPool(){
            this.Clear();
            _result = default(TResult);
            _pool.Push(this);
            
        }

        public TResult GetResult(short token)
        {
            ValidateToken(token);
            try{
                ThrowCancellationOrExceptionIfNeed();
                return _result;
            }finally{
                ReturnToPool();
            }
        }

        [DebuggerHidden()]
        public void SetResult(TResult result,short token){
            ValidateToken(token);
            _result = result;
            this.Succeed();
            if(this.shouldForget){
                ReturnToPool();
            }
        }

        [DebuggerHidden()]
        public override void SetException(Exception e,short token){
            base.SetException(e,token);
            if(this.shouldForget){
                ReturnToPool();
            }
        }
    }    
}
