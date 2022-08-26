using System.Collections.Generic;

public class PathNode : SyntaxNode{
    public List<SyntaxNode> Parts {get;}
    public PathNode(SyntaxNode node){
        this.Parts = new List<SyntaxNode> {node};
    }
    
    public PathNode Concat(PathNode tail) {
        this.Parts.AddRange(tail.Parts);
        return(this);
    }
}