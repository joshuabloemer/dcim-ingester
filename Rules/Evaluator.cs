using System;
using System.Collections.Generic;
using MetadataExtractor;
using System.IO;
using static DcimIngester.Utilities;

namespace DcimIngester.Rules {
    class Evaluator {
        public string FilePath {get;}
        
        public Dictionary<String,Dictionary<String,String>> Metadata {get;} = new Dictionary<String,Dictionary<String,String>>();
        
        public DateTime DateTaken {get;}

        public Boolean RuleMatched {get;private set;} = false;
        
        public Evaluator(string filePath){
            this.FilePath = filePath;
            this.DateTaken = GetDateTaken(filePath);

            try {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(filePath);
                foreach (var directory in directories){
                    Metadata[directory.Name] = new Dictionary<String, String>();
                    foreach (var tag in directory.Tags){
                        Metadata[directory.Name][tag.Name] = tag.Description;
                    }
                }
            }
            catch (ImageProcessingException) {}
        }

        public object Evaluate(SyntaxNode node) {
            switch(node) {
                case ProgramNode p: return Evaluate(p.Block);
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
                case ContainsNode c: return containsNode(c);
                case MetadataNode m: return metadataNode(m);
                case ExtensionNode: return Path.GetExtension(this.FilePath).Remove(0,1);
                case YearNode: return this.DateTaken.Year;
                case MonthNode: return this.DateTaken.Month;
                case DayNode: return this.DateTaken.Day;
                case HourNode: return this.DateTaken.Hour;
                case MinuteNode: return this.DateTaken.Minute;
                case SecondNode: return this.DateTaken.Second;
                case PathPartNode p: return pathPartNode(p);
                case FileNameNode: return Path.GetFileName(this.FilePath);
                case PathNameNode: return this.FilePath;
                case EmptyNode: return null;
            }
            throw(new Exception($"Unknown node type {node.GetType()}"));
        }

        private object containsNode(ContainsNode c)
        {
            string lhs = (string)Evaluate(c.l);
            string rhs = (string)Evaluate(c.r);
            return lhs.Contains(rhs);
        }

        private object pathPartNode(PathPartNode p)
        {
            string[] pathParts = this.FilePath.Split("\\");
            if (p.Part >= 0)
                return pathParts[p.Part];
            else  
                return pathParts[^Math.Abs(p.Part)];
        }

        private object metadataNode(MetadataNode m)
        {
            
            Dictionary<String,String> directory;
            String tag;
            if (this.Metadata.TryGetValue(m.Directory, out directory)){
                if (directory.TryGetValue(m.Tag, out tag)){
                    return tag;
                }
            }
            return "null";
        }

        private object greaterOrEqual(GreaterOrEqualNode g)
        {
            return Convert.ToDecimal(Evaluate(g.l)) >= Convert.ToDecimal(Evaluate(g.r));
        }

        private object lesseOrEqual(LessOrEqualNode l)
        {
            return Convert.ToDecimal(Evaluate(l.l)) <= Convert.ToDecimal(Evaluate(l.r));
        }

        private object greater(GreaterThanNode g)
        {
            return Convert.ToDecimal(Evaluate(g.l)) > Convert.ToDecimal(Evaluate(g.r));
        }

        private object less(LessThanNode l)
        {
            return Convert.ToDecimal(Evaluate(l.l)) < Convert.ToDecimal(Evaluate(l.r));
        }

        private object not(NotNode n)
        {
            return Convert.ToString(Evaluate(n.l)) != Convert.ToString(Evaluate(n.r));
        }

        private object equals(EqualsNode e)
        {
            return Convert.ToString(Evaluate(e.l)) == Convert.ToString(Evaluate(e.r));
        }

        private string rule(RuleNode r)
        {
            string result = null;
            if ((bool)Evaluate(r.Condition)){
                result = (string)Evaluate(r.Path);
                this.RuleMatched = true;
                SyntaxNode indent = r.GetIndent();
                if (indent is not null){
                    result += Evaluate(indent);
                }
            }
            else if (r.Under is not EmptyNode){
                result += Evaluate(r.Under);
            }
            return result;
        }

        private string path(PathNode p)
        {
            string result = "";
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