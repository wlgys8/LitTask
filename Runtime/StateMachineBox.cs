using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace MS.Async.CompilerServices{
    internal interface IStateMachineBox:ILitTaskValueSource
    {
        void SetResult(short token);

        void SetException(Exception e,short token);

        short Token{
            get;
        }
        Action MoveNext{
            get;
        }
    }

    internal class StateMachineBox<TStateMachine>:IStateMachineBox where TStateMachine : IAsyncStateMachine{
        private static short _globalToken = 0;

        private static short AllocateToken(){
            do{
                _globalToken ++;
            }while(_globalToken == 0);
            return _globalToken;
        }

        private static Stack<StateMachineBox<TStateMachine>> _pool = new Stack<StateMachineBox<TStateMachine>>();

        public static StateMachineBox<TStateMachine> Allocate(ref TStateMachine stateMachine){
            StateMachineBox<TStateMachine> ret = null;
            if(_pool.Count == 0){
                ret = new StateMachineBox<TStateMachine>();
            }else{
                ret = _pool.Pop();
            }
            ret.AcquireToken();
            ret.stateMachine = stateMachine;
            return ret;
        }

        public TStateMachine stateMachine;

        private Action _continuation;

        private short _token;
        private ValueSourceStatus _status;
        private Exception _exception;

        private bool _shouldForget = false;

        // private CancellationToken _cancellationToken;


        private StateMachineBox(){
            this.MoveNext = ()=>{
                stateMachine.MoveNext();
            };
        }

        private void AcquireToken(){
            _token = AllocateToken();
        }

        private void ReturnToPool(){
            _token = 0;
            _continuation = null;
            _status = ValueSourceStatus.Pending;
            stateMachine = default(TStateMachine);
            _pool.Push(this);
            _exception = null;
        }

        public Action MoveNext{
            get;private set;
        }

        public short Token{
            get{
                return _token;
            }
        }

        private void ValidateToken(short token){
            if(_token == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(_token != token){
                throw new InvalidOperationException(string.Format("{0},{1}",_token,token));
            }
        }

        /// <summary>
        /// Forget means GetStatus & OnComplete & GetResult won't be called anymore.
        /// </summary>
        public void Forget(short token){
            ValidateToken(token);
            _shouldForget = true;
        }

        public void GetResult(short token)
        {
            ValidateToken(token);
            try{
                if(_status == ValueSourceStatus.Faulted){
                    throw new AggregateException(_exception);
                }else if(_status == ValueSourceStatus.Canceled){
                    throw _exception;
                }
            }finally{
                ReturnToPool();
            }
        }

        [DebuggerHidden()]
        public void SetResult(short token){
            ValidateToken(token);
            _status = ValueSourceStatus.Succeed;
            if(!_shouldForget){
                if(_continuation != null){
                    _continuation();
                }else{
                    //user does not await the task
                }
            }else{
                ReturnToPool();
            }
        }

        [DebuggerHidden()]
        public void SetException(Exception e,short token){
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
            }else{
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
    }    
}
