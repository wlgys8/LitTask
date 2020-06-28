using System;

namespace MS.Async{

    public interface ILitTaskValueSourceBase
    {

        ValueSourceStatus GetStatus(short token);

        void OnCompleted(Action continuation,short token);

        void Forget(short token);

    }
    public interface ILitTaskValueSource:ILitTaskValueSourceBase
    {

        void GetResult(short token);

    }

    public interface ILitTaskValueSource<T>:ILitTaskValueSourceBase
    {
        T GetResult(short token);
    }

    public enum ValueSourceStatus{
        Pending,
        Succeed,
        Faulted,
        Canceled,
    }
}
