using System;

namespace MS.Async{

    public interface ILitTaskValueSource
    {

        ValueSourceStatus GetStatus(short token);

        void OnCompleted(Action continuation,short token);

        void Forget(short token);

    }
    public interface ILitTaskValueSourceVoid:ILitTaskValueSource
    {

        void GetResult(short token);

    }

    public interface ILitTaskValueSource<T>:ILitTaskValueSource
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
