using System;

namespace MS.Async{
    public interface ILitTaskValueSource 
    {

        ValueSourceStatus GetStatus(short token);

        void GetResult(short token);

        void OnCompleted(Action continuation,short token);

        void Forget(short token);

    }



    public enum ValueSourceStatus{
        Pending,
        Succeed,
        Faulted,
        Canceled,
    }
}
