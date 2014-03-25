namespace MoaiUtils.MoaiParsing.CodeGraph.Types {

    public interface IType {
        string Name { get; }
        string Description { get; }
        string Signature { get; }
        bool Exists { get; }
    }

}