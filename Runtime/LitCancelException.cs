using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MS.Async{
    public class LitCancelException : System.Exception
    {
        private const string _stackTrance = "";

        public LitCancelException(){
        }

        /// <summary>
        /// Empty stackTrace to avoid gc alloc
        /// </summary>
        public override string StackTrace{
            get{
                return _stackTrance;
            }
        }


        private static LitCancelException _default = new LitCancelException();

        public static LitCancelException Throw(){
            throw _default;
        }
    }
}
