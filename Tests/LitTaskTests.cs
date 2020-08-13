using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using System.Threading.Tasks;
using System;
using UnityEngine.TestTools;

namespace MS.Async.Tests{


    public class LitTaskTests
    {

        [UnityTest]
        public IEnumerator AwaitVoidAsync(){
            var startTime = System.DateTime.Now;
            var op = new AwaitLitTaskOperation(Delay(100));
            yield return op;
            Assert.IsNull(op.exception);
            Assert.GreaterOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(100));
        }


        [UnityTest]
        public IEnumerator AwaitVoidSync(){
            var op = new AwaitLitTaskOperation(Complete());
            yield return op;
            Assert.IsNull(op.exception);
            
        }       

       [UnityTest]
        public IEnumerator AwaitValueAsync(){
            var op = new AwaitLitTaskOperation<int>(GetValueAsync(100));
            yield return op;
            Assert.True(op.value == 100);
        }


       [UnityTest]
        public IEnumerator AwaitValueSync(){
            var op = new AwaitLitTaskOperation<int>(GetValueSync(100));
            yield return op;
            Assert.True(op.value == 100);
        }


        [UnityTest]
        public IEnumerator AwaitExceptionAsync(){
            var op = new AwaitLitTaskOperation(ThrowExceptionAsync());
            yield return op;
            Assert.True(op.exception is TestException);
        }

        [UnityTest]
        public IEnumerator AwaitExceptionSync(){
            var op = new AwaitLitTaskOperation(ThrowExceptionSync());
            yield return op;
            Assert.True(op.exception is TestException);
        }

        [UnityTest]
        public IEnumerator ForgetExceptionSync(){
            ThrowExceptionSync().Forget();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Exception,"TestException: TestException");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForgetExceptionAsync(){
            ThrowExceptionAsync().Forget();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ContinueWithExceptionAsync(){
            var op = new ContinueLitTaskOperation(ThrowExceptionAsync());
            yield return op;
            Assert.True(op.exception is TestException);
        }      


        [UnityTest]
        public IEnumerator ContinueWithExceptionSync(){
            var op = new ContinueLitTaskOperation(ThrowExceptionSync());
            yield return op;
            Assert.True(op.exception is TestException);
        }      

        [UnityTest]
        public IEnumerator AwaitWhenAll(){
            var startTime = System.DateTime.Now;
            var op = new AwaitLitTaskOperation(LitTask.WhenAll(new LitTask[]{
                Delay(100),
                Delay(1000),
            }));
            yield return op;
            Assert.Greater(DateTime.Now,startTime + TimeSpan.FromMilliseconds(1000));
        }

        [UnityTest]
        public IEnumerator ContinueWhenAll(){
            var startTime = System.DateTime.Now;
            var op = new ContinueLitTaskOperation(LitTask.WhenAll(new LitTask[]{
                Delay(100),
                Delay(1000),
            }));
            yield return op;
            Assert.Greater(DateTime.Now,startTime + TimeSpan.FromMilliseconds(1000));
        }

        [UnityTest]
        public IEnumerator AwaitWhenAllWithException(){
            System.DateTime start = System.DateTime.Now;
            var op = new AwaitLitTaskOperation(LitTask.WhenAll(new LitTask[]{
                Delay(100),
                ThrowExceptionAfterAsync(10),
            }));
            yield return op;
            Assert.NotNull(op.exception);
            Assert.True(op.exception is AggregateException);
            Assert.True((op.exception as AggregateException).InnerException is TestException);
            Assert.GreaterOrEqual(DateTime.Now,start + TimeSpan.FromMilliseconds(100));
        }

        [UnityTest]
        public IEnumerator ContinueWhenAllWithException(){
            System.DateTime start = System.DateTime.Now;
            var op = new ContinueLitTaskOperation(LitTask.WhenAll(new LitTask[]{
                Delay(100),
                ThrowExceptionAfterAsync(10),
            }));
            yield return op;
            Assert.NotNull(op.exception);
            Assert.True(op.exception is AggregateException);
            Assert.True((op.exception as AggregateException).InnerException is TestException);
            Assert.GreaterOrEqual(DateTime.Now,start + TimeSpan.FromMilliseconds(100));
        }        

        [UnityTest]
        public IEnumerator AwaitWhenAny(){
            var startTime = System.DateTime.Now;
            var op = new AwaitLitTaskOperation<LitTask.WhenAnyResult>(LitTask.WhenAny(new LitTask[]{
                Delay(1000),
                Delay(100),
            }));
            yield return op;
            Assert.GreaterOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(100));
            Assert.LessOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(1000));
            Assert.AreEqual(op.value.FirstCompletedTaskIndex,1);
        }

         [UnityTest]
        public IEnumerator ContinueWhenAny(){
            var startTime = System.DateTime.Now;
            var op = new ContinueLitTaskOperation<LitTask.WhenAnyResult>(LitTask.WhenAny(new LitTask[]{
                Delay(1000),
                Delay(100),
            }));
            yield return op;
            Assert.GreaterOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(100));
            Assert.LessOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(1000));
            Assert.AreEqual(op.value.FirstCompletedTaskIndex,1);
        }       

        [UnityTest]
        public IEnumerator AwaitWhenAnyWithExceptionFirst(){
            var startTime = System.DateTime.Now;
            var op = new AwaitLitTaskOperation<LitTask.WhenAnyResult>(LitTask.WhenAny(new LitTask[]{
                Delay(1000),
                ThrowExceptionAfterAsync(100),
            }));
            yield return op;
            Assert.GreaterOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(100));
            Assert.LessOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(1000));
            Assert.AreEqual(op.value.FirstCompletedTaskIndex,1);
            Assert.NotNull(op.value.Exception);
        }     
        [UnityTest]
        public IEnumerator ContinueWhenAnyWithExceptionFirst(){
            var startTime = System.DateTime.Now;
            var op = new ContinueLitTaskOperation<LitTask.WhenAnyResult>(LitTask.WhenAny(new LitTask[]{
                Delay(1000),
                ThrowExceptionAfterAsync(100),
            }));
            yield return op;
            Assert.GreaterOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(100));
            Assert.LessOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(1000));
            Assert.AreEqual(op.value.FirstCompletedTaskIndex,1);
            Assert.NotNull(op.value.Exception);
        }    
        [UnityTest]
        public IEnumerator AwaitWhenAnyWithExceptionLater(){
            var startTime = System.DateTime.Now;
            var op = new AwaitLitTaskOperation<LitTask.WhenAnyResult>(LitTask.WhenAny(new LitTask[]{
                Delay(100),
                ThrowExceptionAfterAsync(1000),
            }));
            yield return op;
            Assert.GreaterOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(100));
            Assert.LessOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(1000));
            Assert.AreEqual(op.value.FirstCompletedTaskIndex,0);
            Assert.IsNull(op.value.Exception);
        }  
        [UnityTest]
        public IEnumerator ContinueWhenAnyWithExceptionLater(){
            var startTime = System.DateTime.Now;
            var op = new ContinueLitTaskOperation<LitTask.WhenAnyResult>(LitTask.WhenAny(new LitTask[]{
                Delay(100),
                ThrowExceptionAfterAsync(1000),
            }));
            yield return op;
            Assert.GreaterOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(100));
            Assert.LessOrEqual(System.DateTime.Now,startTime + System.TimeSpan.FromMilliseconds(1000));
            Assert.AreEqual(op.value.FirstCompletedTaskIndex,0);
            Assert.IsNull(op.value.Exception);
        }  

        public async LitTask Complete(){
            await Task.CompletedTask;   
        }

        public async LitTask Delay(int millSeconds){
            await Task.Delay(millSeconds);
        } 

        public async LitTask<int> GetValueAsync(int value){
            await Task.Delay(100);
            return value;
        }

        public async LitTask<int> GetValueSync(int value){
            await Task.CompletedTask; 
            return value;
        }


        private async LitTask ThrowExceptionAfterAsync(int millSeconds){
            await Task.Delay(millSeconds);
            throw new TestException();    
        }

        private async LitTask ThrowExceptionAsync(){
            await Task.Delay(100);
            throw new TestException();           
        }


        private async LitTask ThrowExceptionSync(){
            await Task.CompletedTask; 
            throw new TestException();      
        }

    }


    public class TestException:System.Exception{

        public TestException():base("TestException"){

        }
    }


    class AwaitLitTaskOperation : CustomYieldInstruction
    {
        private Exception _exception;
        private bool _waiting = true;

        public AwaitLitTaskOperation(LitTask task){
            AwaitTask(task);
        }

        private async void AwaitTask(LitTask task){
            try{
                await task;
            }catch(System.Exception e){
                _exception = e;
            }finally{
                _waiting = false;
            }
        }
        public override bool keepWaiting {
            get{
                return _waiting;
            }
        }

        public Exception exception{
            get{
                return _exception;
            }
        }
    }

    class AwaitLitTaskOperation<T> : CustomYieldInstruction
    {
        private Exception _exception;
        private bool _waiting = true;
        private T _value;


        public AwaitLitTaskOperation(LitTask<T> task){
            AwaitTask(task);
        }

        private async void AwaitTask(LitTask<T> task){
            try{
                _value = await task;
            }catch(System.Exception e){
                _exception = e;
            }finally{
                _waiting = false;
            }
        }
        public override bool keepWaiting {
            get{
                return _waiting;
            }
        }

        public Exception exception{
            get{
                return _exception;
            }
        }

        public T value{
            get{
                return _value;
            }
        }
    }

    class ContinueLitTaskOperation : CustomYieldInstruction
    {
        private Exception _exception;
        private bool _waiting = true;

        private LitTaskResult _result;

        public ContinueLitTaskOperation(LitTask task){
            task.ContinueWith(OnContinue);
        }

        private void OnContinue(LitTaskResult result){
            _waiting = false;
            _result = result;
        }

        public override bool keepWaiting {
            get{
                return _waiting;
            }
        }

        public Exception exception{
            get{
                return _result.Exception;
            }
        }

        public bool isCancelled{
            get{
                return _result.IsCancelled;
            }
        }
    }

     class ContinueLitTaskOperation<T> : CustomYieldInstruction
    {
        private Exception _exception;
        private bool _waiting = true;

        private LitTaskResult<T> _result;

        public ContinueLitTaskOperation(LitTask<T> task){
            task.ContinueWith(OnContinue);
        }

        private void OnContinue(LitTaskResult<T> result){
            _waiting = false;
            _result = result;
        }

        public override bool keepWaiting {
            get{
                return _waiting;
            }
        }

        public Exception exception{
            get{
                return _result.Exception;
            }
        }

        public bool isCancelled{
            get{
                return _result.IsCancelled;
            }
        }

        public T value{
            get{
                return _result.Value;
            }
        }
    }   
}
