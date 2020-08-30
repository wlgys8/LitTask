using System.Collections;
using System.Collections.Generic;
using System;
namespace MS.Async{


    public struct LitCancellationToken
    {
        private static LitCancelException _defaultCancelException = new LitCancelException();

        private LitCancellationTokenSource _source;
        private short _tokenId;

        internal LitCancellationToken(LitCancellationTokenSource source){
            _source = source;
            _tokenId = source.TokenId;
        }

        public bool IsCancellationRequested{
            get{
                if(_source == null){
                    return false;
                }
                return _tokenId != _source.TokenId || _source.IsCancellationRequested;
            }
        }

        public void ThrowIfCancellationRequested(){
            if(IsCancellationRequested){
                LitCancelException.Throw();
            }
        }

    }


    public class LitCancellationTokenSource:System.IDisposable{

        private bool _isCancellationRequested = false;
        private short _tokenId;

        private LitCancellationTokenSource(){}

        internal short TokenId{
            get{
                return _tokenId;
            }
        }

        private void AssertNotDisposed(){
            if(_tokenId == 0){
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        public LitCancellationToken Token{
            get{
                AssertNotDisposed();
                return new LitCancellationToken(this);
            }
        }

        public bool IsCancellationRequested{
            get{
                AssertNotDisposed();
                return _isCancellationRequested;
            }
        }

        /// <summary>
        /// send cancel request and will be disposed later.
        /// </summary>
        public void Cancel(){
            AssertNotDisposed();
            _isCancellationRequested = true;
        }

        /// <summary>
        /// Dispose will also cancel all task
        /// </summary>
        public void Dispose(){
            AssertNotDisposed();
            _isCancellationRequested = false;
            _tokenId = 0;
            _pool.Push(this);
        }

        private static CompilerServices.TokenAllocator _tokenAllocator = new CompilerServices.TokenAllocator();
        private static Stack<LitCancellationTokenSource> _pool = new Stack<LitCancellationTokenSource>();

        public static LitCancellationTokenSource Get(){
            LitCancellationTokenSource source = null;
            if(_pool.Count == 0){
                source = new LitCancellationTokenSource();
            }else{
                source = _pool.Pop();
            }
            source._tokenId = _tokenAllocator.Next();
            return source;
        }
    }

}
