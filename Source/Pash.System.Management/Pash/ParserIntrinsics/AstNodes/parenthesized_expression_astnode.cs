﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Ast;
using Irony.Parsing;
using Pash.Implementation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Pash.ParserIntrinsics.AstNodes
{
    public class parenthesized_expression_astnode : _astnode
    {
        public readonly pipeline_astnode Pipeline;

        public parenthesized_expression_astnode(AstContext astContext, ParseTreeNode parseTreeNode)
            : base(astContext, parseTreeNode)
        {
            ////        parenthesized_expression:
            ////            (   new_lines_opt   pipeline   new_lines_opt   )

            this.Pipeline = this.ChildAstNodes[1].As<pipeline_astnode>();
        }

        internal object Execute(ExecutionContext context, ICommandRuntime commandRuntime)
        {
            Pipeline pipeline = context.CurrentRunspace.CreateNestedPipeline();
            context.PushPipeline(pipeline);
            Collection<PSObject> results = this.Pipeline.Execute(context, commandRuntime) as Collection<PSObject>;
            context.PopPipeline();

            if (results.Count == 0)
                return null;

            if (results.Count == 1)
                return results.Single();

            // TODO: make sure that the array.ToString calls the ToString on each PSObject
            return results.ToArray();
        }
    }
}
