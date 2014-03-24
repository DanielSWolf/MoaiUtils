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

    public class ClassPosition : FilePosition {
        public ClassPosition(FilePosition filePosition, string className)
            : base(filePosition.FileInfo) {
            ClassName = className;
        }

        public string ClassName { get; private set; }

        public override string ToString(bool pathAsUri) {
            return string.Format("{0}, type {1}", GetFileDescription(pathAsUri), ClassName);
        }
    }

    public class MethodPosition : ClassPosition {
        public MethodPosition(FilePosition filePosition, string className, string nativeMethodName)
            : base(filePosition, className) {
            NativeMethodName = nativeMethodName;
        }

        public MethodPosition(ClassPosition classPosition, string memberName)
            : this(classPosition, classPosition.ClassName, memberName) {}

        public string NativeMethodName { get; private set; }

        public override string ToString(bool pathAsUri) {
            return string.Format("{0}, {1}::{2}()", GetFileDescription(pathAsUri), ClassName, NativeMethodName);
        }
    }

}