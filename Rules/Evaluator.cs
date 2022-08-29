using System;
using System.Collections.Generic;
using MetadataExtractor;

namespace DcimIngester.Rules {
    class Evaluator {
        public string FilePath {get;}
        
        public Dictionary<String,Dictionary<String,String>> Metadata {get;} = new Dictionary<String,Dictionary<String,String>>();
        
        public Evaluator(string filePath){
            this.FilePath = filePath;
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(filePath);
            foreach (var directory in directories){
                Metadata[directory.Name] = new Dictionary<String, String>();
                foreach (var tag in directory.Tags){
                    Metadata[directory.Name][tag.Name] = tag.Description;
                }
            }
        }

        public object Evaluate(SyntaxNode node) {
            switch(node) {
                case ProgramNode p: return program(p);
                case StringNode n: return n.Value;
                case BlockNode b: return block(b);
                case PathNode p: return path(p);
                case RuleNode r: return rule(r);
                case AnyNode: return true;
                case EqualsNode e: return equals(e);
                case NotNode n: return not(n);
                case LessThanNode l: return less(l);
                case GreaterThanNode g: return greater(g);
                case LessOrEqualNode l: return lesseOrEqual(l);
                case GreaterOrEqualNode g: return greaterOrEqual(g);

            }
            throw(new Exception($"Unknown node type {node.GetType()}"));
        }

        private object program(ProgramNode p)
        {
            return Evaluate(p.Block);
        }

        private object greaterOrEqual(GreaterOrEqualNode g)
        {
            return (decimal)Evaluate(g.l) >= (decimal)Evaluate(g.r);
        }

        private object lesseOrEqual(LessOrEqualNode l)
        {
            return (decimal)Evaluate(l.l) <= (decimal)Evaluate(l.r);
        }

        private object greater(GreaterThanNode g)
        {
            return (decimal)Evaluate(g.l) > (decimal)Evaluate(g.r);
        }

        private object less(LessThanNode l)
        {
            return (decimal)Evaluate(l.l) < (decimal)Evaluate(l.r);
        }

        private object not(NotNode n)
        {
            return Evaluate(n.l) != Evaluate(n.r);
        }

        private object equals(EqualsNode e)
        {
            return Evaluate(e.l) == Evaluate(e.r);
        }

        private string rule(RuleNode r)
        {
            string result = null;
            if ((bool)Evaluate(r.Condition)){
                result = (string)Evaluate(r.Path);
                SyntaxNode indent = r.GetIndent();
                if (indent is not null){
                    result += Evaluate(indent);
                }
                else if (r.Under is not EmptyNode){
                    result += Evaluate(r.Under);
                }
            }
            return result;
        }

        private string path(PathNode p)
        {
            string result = "/";
            foreach(SyntaxNode part in p.Parts){
                result += Evaluate(part);
            }
            return result;
        }

        private string? block(BlockNode b) {
            string? result = null;
            foreach(var statement in b.Statements) {
                result = (string)Evaluate(statement);
                if (result is not null) return result;
            }
            return null;
        }
    }
}