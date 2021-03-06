﻿namespace MS.Async{


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
                return _tokenId != _source.TokenId;
            }
        }

        public void ThrowIfCancellationRequested(){
            if(IsCancellationRequested){
                LitCancelException.Throw();
            }
        }
    }


    public class LitCancellationTokenSource{

        private short _tokenId;

        public LitCancellationTokenSource(){
            _tokenId = 1;
        }

        internal short TokenId{
            get{
                return _tokenId;
            }
        }

        public LitCancellationToken Token{
            get{
                return new LitCancellationToken(this);
            }
        }

        /// <summary>
        /// Cancel all the token that generated by this source previously.
        /// Tokens generated later still work.
        /// </summary>
        public void Cancel(){
            _tokenId ++;
        }
  
    }

}
