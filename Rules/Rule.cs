using System;
using System.Linq;

public class Rule {
    public int indentLevel {get;}
    private string _rule;
    public string rule {
        get{return _rule;}
        set{
            _rule = value;
            updateChildren();
        }
    }

    public string? path {get;set;}
    private Rule? under {get;set;}
    private Rule? indent {get;set;}
    private Rule? next {get;set;}

    public Rule(int indentLevel){
        this.indentLevel = indentLevel;
        this._rule = "";
        Console.WriteLine(indentLevel);

    }

    private void updateChildren(){
        Console.WriteLine(this.rule.Length);
        if (this._rule.Length > 0) {
            under = new Rule(this.indentLevel);
            indent = new Rule(this.indentLevel+1);
            next = new Rule(this.indentLevel);
        }
   }

}