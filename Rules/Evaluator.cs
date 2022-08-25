using System;
using System.Collections.Generic;

namespace DcimIngester.Rules {
    class Evaluator {
        public object Evaluate(SyntaxNode node) {
            switch(node) {
                case PathNode n: return n.Value;
                case BlockNode b: return BlockNode(b);

            }
            throw(new Exception($"Unknown node type {node.GetType()}"));
        }

        private object BlockNode(BlockNode b) {
            object result = null;
            foreach(var statement in b.Statements) {
                result = Evaluate(statement);
            }
            return result;
        }
    }
}