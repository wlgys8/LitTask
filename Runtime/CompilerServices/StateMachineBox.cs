using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace MS.Async.CompilerServices{

    public class TokenAllocator{
        private short _token = 0;

        public short Next(){
            do{
                _token ++;
            }while(_token == 0);
            return _token;
        }     
    }

    internal interface IStateMachineBox
    {
  
        Action MoveNext{
            get;
        }     

        void Return();
    }

    internal class StateMachineBox<TStateMachine>:IStateMachineBox, Diagnostics.ITracableObject where TStateMachine : IAsyncStateMachine{

        [DebuggerHidden]
        public static StateMachineBox<TStateMachine> Allocate(ref TStateMachine stateMachine){
            var box = TaskValueSourcePool<StateMachineBox<TStateMachine>>.Allocate();
            box._stateMachine = stateMachine;
            Diagnostics.Trace.TraceAllocation(box);
            return box;
        }
        protected TStateMachine _stateMachine;
        public StateMachineBox(){
            this.MoveNext = ()=>{
                _stateMachine.MoveNext();
            };          
        }

        [DebuggerHidden]
        public Action MoveNext{
            get;private set;
        }

        public string DebugNameId{
            get{
                return _stateMachine.GetType().Name;
            }
        }
        public void Return(){
            _stateMachine = default(TStateMachine);
            TaskValueSourcePool<StateMachineBox<TStateMachine>>.Return(this);
            Diagnostics.Trace.TraceReturn(this);
        }

    }    

}
