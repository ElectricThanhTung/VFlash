using System;

namespace VFlash {
    internal class FlashException : Exception {
        public FlashException() : base() {

        }

        public FlashException(string msg) : base(msg) {

        }
    }
}
