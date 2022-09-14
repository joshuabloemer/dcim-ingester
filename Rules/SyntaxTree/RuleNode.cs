using System;
using System.Linq;

public class RuleNode : SyntaxNode {
    public SyntaxNode Condition {get;} 
    public SyntaxNode Path {get;}
    public SyntaxNode Under {get;}
    public SyntaxNode Indent {get;}

    public RuleNode(SyntaxNode condition,SyntaxNode path,SyntaxNode under, SyntaxNode indent)
    {
        this.Condition = condition;
        this.Path = path;
        this.Under = under;
        this.Indent = indent;
    }

    public SyntaxNode GetIndent(){
        if (this.Indent is not EmptyNode){
            return (BlockNode)this.Indent;   
        }
        if (this.Under is not EmptyNode){ 
            RuleNode under = (RuleNode)this.Under;
            return under.GetIndent();
        }
        return null;
    }
}