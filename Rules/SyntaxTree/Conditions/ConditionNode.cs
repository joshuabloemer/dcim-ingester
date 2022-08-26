public abstract class ConditionNode : SyntaxNode{
    public SyntaxNode l {get;}
    public SyntaxNode r {get;}
    
    public ConditionNode(SyntaxNode l, SyntaxNode r){
        this.l = l;
        this.r= r;
    }
    public override string ToString()
    {
        return base.ToString() + " " + this.l.ToString() + this.r.ToString() ;
    }
}