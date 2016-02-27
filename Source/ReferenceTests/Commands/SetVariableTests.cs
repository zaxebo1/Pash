﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Linq;
using System.Management.Automation;
using NUnit.Framework;

namespace ReferenceTests.Commands
{
    [TestFixture]
    public class SetVariableTests : ReferenceTestBase
    {
        [Test]
        public void NameAndValue()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable foo 'bar'",
                "$foo"
            });

            Assert.AreEqual("bar" + Environment.NewLine, result);
        }

        [Test]
        public void NameAndValueUsingNamedParameters()
        {
            string result = ReferenceHost.Execute(new string[] {
                "set -name foo -value 'bar'",
                "$foo"
            });

            Assert.AreEqual("bar" + Environment.NewLine, result);
        }

        [Test]
        public void ValueFromPipeline()
        {
            string result = ReferenceHost.Execute(new string[] {
                "'bar' | sv foo",
                "$foo"
            });

            Assert.AreEqual("bar" + Environment.NewLine, result);
        }

        [Test]
        public void NameAndValueForVariableThatAlreadyExists()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$foo = 'abc'",
                "Set-Variable foo 'bar'",
                "$foo"
            });

            Assert.AreEqual("bar" + Environment.NewLine, result);
        }

        [Test]
        public void MultipleNamesSingleValue()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable a,b 'bar'",
                "$a + \", \" + $b"
            });

            Assert.AreEqual("bar, bar" + Environment.NewLine, result);
        }

        [Test]
        public void NoValue()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable foo",
                "$foo -eq $null"
            });

            Assert.AreEqual("True" + Environment.NewLine, result);
        }

        [Test]
        public void SingleNameMultipleValues()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable a 'foo','bar'",
                "$a[0] + \", \" + $a[1]"
            });

            Assert.AreEqual("foo, bar" + Environment.NewLine, result);
        }

        [Test]
        public void SingleNameMultipleValuesOnPipeline()
        {
            string result = ReferenceHost.Execute(new string[] {
                "'foo','bar' | Set-Variable a",
                "$a[0] + \", \" + $a[1] + \" - Type=\" + $a.GetType().Name"
            });

            Assert.AreEqual("foo, bar - Type=Object[]" + Environment.NewLine, result);
        }

        [Test]
        public void Passthru()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$output = Set-Variable foo 'bar' -passthru",
                "'Name=' + $output.Name + ', Value=' + $output.Value + ', Type=' + $output.GetType().Name"
            });

            Assert.AreEqual("Name=foo, Value=bar, Type=PSVariable" + Environment.NewLine, result);
        }

        [Test]
        public void PassthruTwoVariablesCreated()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$v= Set-Variable foo,bar 'abc' -passthru",
                "'Names=' + $v[0].Name + ',' + $v[1].Name + ' Values=' + $v[0].Value + ',' + $v[1].Value + ' Type=' + $v.GetType().Name + ' ' + $v[0].GetType().Name"
            });

            Assert.AreEqual("Names=foo,bar Values=abc,abc Type=Object[] PSVariable" + Environment.NewLine, result);
        }

        [Test]
        public void VariableDescription()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable foo 'bar' -description 'Variable Description'",
                "(Get-Variable foo).Description"
            });

            Assert.AreEqual("Variable Description" + Environment.NewLine, result);
        }

        [Test]
        public void DefaultVariableDescriptionIsEmptyString()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable foo 'bar'",
                "(Get-Variable foo).Description"
            });

            Assert.AreEqual(String.Empty + Environment.NewLine, result);
        }

        [Test]
        public void VariableDescriptionForExistingVariable()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable foo 'bar' -description 'desc 1'",
                "Set-Variable foo 'bar' -description 'Variable Description'",
                "(Get-Variable foo).Description"
            });

            Assert.AreEqual("Variable Description" + Environment.NewLine, result);
        }

        [Test]
        public void VariableOptionsIsNoneByDefault()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable foo 'bar'",
                "(Get-Variable foo).Options.ToString()"
            });

            Assert.AreEqual("None" + Environment.NewLine, result);
        }

        [Test]
        public void ReadOnlyVariable()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable foo 'bar' -option readonly",
                "(Get-Variable foo).Options.ToString()"
            });

            Assert.AreEqual("ReadOnly" + Environment.NewLine, result);
        }

        [Test]
        public void CreateReadOnlyVariableThenTryToCreateWritableVariableWithSameName()
        {
            var ex = Assert.Throws<ExecutionWithErrorsException>(delegate
            {
                ReferenceHost.Execute(new string[] {
                    "Set-Variable foo 'bar' -option readonly",
                    "Set-Variable foo 'abc'"
                });
            });

            ErrorRecord error = ReferenceHost.GetLastRawErrorRecords().Single();
            var sessionStateException = error.Exception as SessionStateUnauthorizedAccessException;

            Assert.AreEqual("foo", error.TargetObject);
            Assert.AreEqual("Cannot overwrite variable foo because it is read-only or constant.", error.Exception.Message);
            Assert.IsInstanceOf<SessionStateUnauthorizedAccessException>(error.Exception);
            Assert.AreEqual("foo", sessionStateException.ItemName);
            Assert.AreEqual(SessionStateCategory.Variable, sessionStateException.SessionStateCategory);
            Assert.AreEqual("System.Management.Automation", sessionStateException.Source);
            Assert.AreEqual("Set-Variable", error.CategoryInfo.Activity);
            Assert.AreEqual(ErrorCategory.WriteError, error.CategoryInfo.Category);
            Assert.AreEqual("SessionStateUnauthorizedAccessException", error.CategoryInfo.Reason);
            Assert.AreEqual("foo", error.CategoryInfo.TargetName);
            Assert.AreEqual("String", error.CategoryInfo.TargetType);
            Assert.AreEqual("VariableNotWritable,Microsoft.PowerShell.Commands.SetVariableCommand", error.FullyQualifiedErrorId);
            Assert.AreEqual("foo", error.TargetObject);
        }

        [Test]
        public void CreateVariableThenTryToCreateConstantVariableWithSameName()
        {
            var ex = Assert.Throws<ExecutionWithErrorsException>(delegate
            {
                ReferenceHost.Execute(new string[] {
                    "Set-Variable foo 'bar'",
                    "Set-Variable foo 'abc' -option constant"
                });
            });

            ErrorRecord error = ReferenceHost.GetLastRawErrorRecords().Single();
            var sessionStateException = error.Exception as SessionStateUnauthorizedAccessException;

            Assert.AreEqual("foo", error.TargetObject);
            Assert.AreEqual("Existing variable foo cannot be made constant. Variables can be made constant only at creation time.", error.Exception.Message);
            Assert.IsInstanceOf<SessionStateUnauthorizedAccessException>(error.Exception);
            Assert.AreEqual("foo", sessionStateException.ItemName);
            Assert.AreEqual(SessionStateCategory.Variable, sessionStateException.SessionStateCategory);
            Assert.AreEqual("System.Management.Automation", sessionStateException.Source);
            Assert.AreEqual("Set-Variable", error.CategoryInfo.Activity);
            Assert.AreEqual(ErrorCategory.WriteError, error.CategoryInfo.Category);
            Assert.AreEqual("SessionStateUnauthorizedAccessException", error.CategoryInfo.Reason);
            Assert.AreEqual("foo", error.CategoryInfo.TargetName);
            Assert.AreEqual("String", error.CategoryInfo.TargetType);
            Assert.AreEqual("VariableCannotBeMadeConstant,Microsoft.PowerShell.Commands.SetVariableCommand", error.FullyQualifiedErrorId);
            Assert.AreEqual("foo", error.TargetObject);
        }

        [Test]
        public void DefaultVisibilityIsPublic()
        {
            string result = ReferenceHost.Execute(new string[] {
                "Set-Variable -name foo",
                "(Get-Variable foo).Visibility.ToString()"
            });

            Assert.AreEqual("Public" + Environment.NewLine, result);
        }

        [Test]
        public void VisibilityIsPrivatePassThru()
        {
            string result = ReferenceHost.Execute(new string[] {
                "$a = Set-Variable -name foo -passthru -visibility private",
                "$a.Visibility.ToString()"
            });

            Assert.AreEqual("Private" + Environment.NewLine, result);
        }

        [Test]
        public void CreateVisibilityPrivateVariableThenTryToChangeDescription()
        {
            var ex = Assert.Throws<ExecutionWithErrorsException>(delegate
            {
                ReferenceHost.Execute(new string[] {
                    "Set-Variable -name foo -visibility private",
                    "Set-Variable -name foo -description 'private'"
                });
            });

            ErrorRecord error = ReferenceHost.GetLastRawErrorRecords().Single();
            var sessionStateException = error.Exception as SessionStateException;

            Assert.AreEqual("foo", error.TargetObject);
            Assert.AreEqual("Cannot access the variable '$foo' because it is a private variable", error.Exception.Message);
            Assert.AreEqual("VariableIsPrivate,Microsoft.PowerShell.Commands.SetVariableCommand", error.FullyQualifiedErrorId);
            Assert.AreEqual(ErrorCategory.PermissionDenied, error.CategoryInfo.Category);
            Assert.AreEqual("foo", sessionStateException.ItemName);
            Assert.AreEqual(SessionStateCategory.Variable, sessionStateException.SessionStateCategory);
        }
    }
}
