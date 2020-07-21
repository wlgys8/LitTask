using System;

namespace MS.Async{

    public interface ILitTaskValueSourceBase
    {

        ValueSourceStatus GetStatus(short token);

        void OnCompleted(Action continuation,short token);

        

    }
    public interface ILitTaskValueSource:ILitTaskValueSourceBase
    {

        void GetResult(short token);

        void Continue(short token,bool exceptionSlience,Action<LitTaskResult> action);

    }

    public interface ILitTaskValueSource<T>:ILitTaskValueSourceBase
    {
        T GetResult(short token);

        void Continue(short token,bool exceptionSlience,Action<LitTaskResult<T>> action);
    }

    public enum ValueSourceStatus{
        Pending,
        Succeed,
        Faulted,
        Canceled,
    }
}
