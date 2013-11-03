﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Engine;

namespace Core.Parser
{
	public class Warrior
    {
        public List<Statement> Statements { get; private set; }
        public Dictionary<String, int> Labels { get; private set; }
		public Dictionary<String, Expression> Constants { get; private set; }
		public HashSet<String> EvaluatingConstants { get; private set; }

		public Expression StartAddressExpression = null;
		public int StartAddress { get; private set; }

		public Warrior(List<Statement> statements)
        {
            Statements = statements;
            Labels = new Dictionary<string, int>();
			Constants = new Dictionary<string, Expression>();
			EvaluatingConstants = new HashSet<string>();

        }

        public Warrior() : this(new List<Statement>())
        {
        }

        public void AddStatement(Statement statement, ParserState state)
        {
            if (statement.HasLabel && Labels.ContainsKey(statement.Label))
                throw new CompilationException("Statement with same label already exists", state);
			if (statement.HasLabel && Constants.ContainsKey(statement.Label))
				throw new CompilationException("Constant with same label already exists", state);

            Statements.Add(statement);

            if (statement.HasLabel)
                Labels[statement.Label] = Statements.Count - 1;
        }

		public void EvaluateAllExpressions()
		{
			var address = 0;

			foreach (var constant in Constants.Keys.ToArray())
			{
				EvaluatingConstants.Clear();
				EvaluatingConstants.Add(constant);
				Constants[constant] = new NumberExpression(Constants[constant].Calculate(this));
				EvaluatingConstants.Remove(constant);
			}

			foreach (var statement in Statements)
			{
				statement.FieldA = new NumberExpression(statement.FieldA.Calculate(this, address));
				statement.FieldB = new NumberExpression(statement.FieldB.Calculate(this, address));
				address++;
			}

			if (StartAddressExpression == null)
				StartAddress = 0;
			else
			{
				if (StartAddressExpression.GetType() == typeof(VariableExpression))
					StartAddress = ModularArith.Mod(Statements.Count + StartAddressExpression.Calculate(this, Statements.Count));
				else if (StartAddressExpression.GetType() == typeof(NumberExpression))
					StartAddress = StartAddressExpression.Calculate(this, Statements.Count);
				else
					throw new CompilationException("END argument must be label or number", StartAddressExpression.ToString(), 0);
			}
		}
    }
}
