using System;
using System.IO;
using MoaiUtils.Tools;

namespace MoaiUtils.CreateApiDescription {
    public class FilePosition : IComparable {
        public FilePosition(FileInfo fileInfo, DirectoryInfo rootDirectory, bool printFullPath) {
            FileInfo = fileInfo;
            RootDirectory = rootDirectory;
            PrintFullPath = printFullPath;
        }

        public FileInfo FileInfo { get; private set; }
        public DirectoryInfo RootDirectory { get; private set; }
        public bool PrintFullPath { get; private set; }

        public string FilePath {
            get { return PrintFullPath ? FileInfo.FullName : FileInfo.RelativeTo(RootDirectory); }
        }

        public override string ToString() {
            return FileDescription;
        }

        protected string FileDescription {
            get { return string.Format("file '{0}'", FilePath); }
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
            : base(filePosition.FileInfo, filePosition.RootDirectory, filePosition.PrintFullPath) {
            TypeName = typeName;
        }

        public string TypeName { get; private set; }

        public override string ToString() {
            return string.Format("type {0} in {1}", TypeName, FileDescription);
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

        public override string ToString() {
            return string.Format("{0}.{1}() in {2}", TypeName, NativeMethodName, FileDescription);
        }
    }

}