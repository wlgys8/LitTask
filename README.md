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


# Install

For unity package manager, add:

```json
"com.ms.litask":"https://github.com/wlgys8/LitTask.git"
```

to `Package/manifest.json`

# Usage

```csharp

async LitTask RunAsync(){
    await new SomeAwaitableObject();
}


```

# FAQ

* 在Unity Editor中使用Profiler分析发现有GC Alloc?
    
  这是因为在Debug编译模式下，async/await编译出来的状态机是class类型的. Release编译模式下会切换成struct类型，就无alloc了。
  Unity2020已经支持了在编辑器中切换release/debug编译模式。可以测试看看。



# TODO:

LitTask\<T> 实现