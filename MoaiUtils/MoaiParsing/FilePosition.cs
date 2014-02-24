using System;
using System.IO;

namespace MoaiUtils.MoaiParsing {
    public class FilePosition : IComparable {
        public FilePosition(FileInfo fileInfo) {
            FileInfo = fileInfo;
        }

        public FileInfo FileInfo { get; private set; }

        public override string ToString() {
            return ToString(pathAsUri: false);
        }

        public virtual string ToString(bool pathAsUri) {
            return GetFileDescription(pathAsUri);
        }

        protected string GetFileDescription(bool pathAsUri) {
            return pathAsUri
                ? new Uri(FileInfo.FullName).AbsoluteUri
                : FileInfo.FullName;
        }

        #region Equality members

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return ToString() == obj.ToString();
        }

        public override int GetHashCode() {
            return ToString().GetHashCode();
        }

        public int CompareTo(object obj) {
            return ToString().CompareTo(obj.ToString());
        }

        #endregion
    }

    public class TypePosition : FilePosition {
        public TypePosition(FilePosition filePosition, string typeName)
            : base(filePosition.FileInfo) {
            TypeName = typeName;
        }

        public string TypeName { get; private set; }

        public override string ToString(bool pathAsUri) {
            return string.Format("{0}, type {1}", GetFileDescription(pathAsUri), TypeName);
        }
    }

    public class MethodPosition : TypePosition {
        public MethodPosition(FilePosition filePosition, string typeName, string nativeMethodName)
            : base(filePosition, typeName) {
            NativeMethodName = nativeMethodName;
        }

        public MethodPosition(TypePosition typePosition, string memberName)
            : this(typePosition, typePosition.TypeName, memberName) {}

        public string NativeMethodName { get; private set; }

        public override string ToString(bool pathAsUri) {
            return string.Format("{0}, {1}::{2}()", GetFileDescription(pathAsUri), TypeName, NativeMethodName);
        }
    }

}