﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Ast;
using Irony.Parsing;
using System.Diagnostics;
using System.Reflection;
using Pash.Implementation;
using System.Management.Automation;
using Extensions.String;
using System.Collections;
using System.Management.Automation.Runspaces;

namespace Pash.ParserIntrinsics.AstNodes
{
    public class assignment_expression_astnode : _astnode
    {
        public readonly variable_astnode Variable;
        public readonly string Operator;
        public readonly statement_astnode Statement;

        public assignment_expression_astnode(AstContext astContext, ParseTreeNode parseTreeNode)
            : base(astContext, parseTreeNode)
        {
            ////        assignment_expression:
            ////            expression   assignment_operator   statement
            Debug.Assert(this.ChildAstNodes.Count == 3, this.ToString());
            this.Variable = this.ChildAstNodes[0].As<variable_astnode>();
            this.Operator = this.parseTreeNode.ChildNodes[1].FindTokenAndGetText();
            this.Statement = this.ChildAstNodes[2].As<statement_astnode>();
        }

        internal void Execute(ExecutionContext context, ICommandRuntime commandRuntime)
        {
            var statementValue = this.Statement.Execute(context, commandRuntime);

            ////        assignment_operator:  one of
            ////            =		dash   =			+=		*=		/=		%=
            switch (this.Operator)
            {
                case "=":
                    SetVariable(context, commandRuntime, this.Variable.Name, statementValue);
                    // TODO: return variable
                    break;

                default:
                    throw new NotImplementedException(this.Operator);
            }

            throw new NotImplementedException();
        }

        private void SetVariable(ExecutionContext context, ICommandRuntime commandRuntime, string variableName, object statementValue)
        {
            ExecutionContext nestedContext = context.CreateNestedContext();

            if (!(context.CurrentRunspace is LocalRunspace))
                throw new InvalidOperationException("Invalid context");

            // MUST: fix this with the commandRuntime
            Pipeline pipeline = context.CurrentRunspace.CreateNestedPipeline();
            context.PushPipeline(pipeline);

            try
            {
                Command cmd = new Command("Set-Variable");
                cmd.Parameters.Add("Name", new[] { variableName });
                cmd.Parameters.Add("Value", statementValue);
                // TODO: implement command invoke
                pipeline.Commands.Add(cmd);
                pipeline.Invoke();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                context.PopPipeline();
            }
        }

        static IEnumerable<object> Execute(ExecutionContext context, ICommandRuntime commandRuntime, _astnode firstItemAstNode, _astnode remainingItemsAstNode)
        {
            ////  7.11 Assignment operators
            ////      Syntax:
            ////    
            ////        assignment-expression:
            ////        expression   assignment-operator   statement
            ////    
            ////        assignment-operator:  one of
            ////        =     dash   =         +=    *=    /=    %=
            ////    
            ////      Description:
            ////    
            ////         An assignment operator stores a value in the writable location designated by expression. For a
            ////        discussion of assignment-operator = see §7.11.1. For a discussion of all other assignment-operators
            ////        see §7.11.2.
            ////    
            ////         An assignment expression has the value designated by expression after the assignment has taken
            ////        place; however, that assignment expression does not itself designate a writable location. If
            ////        expression is type-constrained (§5.3), the type used in that constraint is the type of the result;
            ////        otherwise, the type of the result is the type after the usual arithmetic conversions (§6.15) have
            ////        been applied.
            ////    
            ////         This operator is right associative.
            ////
            ////  7.11.1 Simple assignment
            ////     Description:
            ////    
            ////         In simple assignment (=), the value designated by statement replaces the value stored in the
            ////        writable location designated by expression. However, if expression designates a non-existent key in
            ////        a Hashtable, that key is added to the Hashtable with an associated value of the value designated by
            ////        statement.
            ////        
            ////         As shown by the grammar, expression may designate a comma-separated list of writable locations.
            ////        This is known as multiple assignment. statement designates a list of one or more comma-separated
            ////        values. The commas in either operand list are part of the multiple-assignment syntax and do not
            ////        represent the binary comma operator.  Values are taken from the list designated by statement, in
            ////        lexical order, and stored in the corresponding writable location designated by expression. If the
            ////        list designated by statement has fewer values than there are expression writable locations, the
            ////        excess locations take on the value $null. If the list designated by statement has more values than
            ////        there are expression writable locations, all but the right-most expression location take on the
            ////        corresponding statement value and the right-most expression location becomes an unconstrained
            ////        1-dimensional array with all the remaining statement values as elements.
            ////        
            ////         For statements that have values (§8.1.2), statement can be a statement.
            throw new NotImplementedException();
        }
    }
}
