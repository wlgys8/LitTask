# LitTask

C# Task的轻量无GC Alloc版本实现。
LitTask是ValueType实现，同时内部对非值类型也采用了对象池管理。

## 优势:

* 无GC Alloc (在Release编译模式下).
* 性能好.

## 缺点:

* 线程不安全.
* 一个LitTask只能被await一次.
* 最好不要对LitTask进行引用，而应该在返回的时候马上await或者Forget掉



# Usage

```csharp

async LitTask RunAsync(){
    await new SomeAwaitableObject();
}


```


# TODO:

LitTask\<T> 实现