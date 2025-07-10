using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace deltafm
{
    public static class SqlModifier
    {
        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("MYAPP_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Environment variable 'MYAPP_CONNECTION_STRING' is not set.");
        // ^ this can go in a config file, this won't stay in the new class, can be pulled from application settings 
        public static string AppendWhereCondition(string originalSql, DateTime lastRun)
        {
            TSql150Parser parser = new TSql150Parser(false);
            IList<ParseError> errors;
            TSqlFragment fragment;
            // how the SQL is parsed, the string is read through this string reader
            // string reader then breaks it into keywords (tokenizes) 
            // abstract syntax tree (ast) is created from the tokens

            using (var reader = new StringReader(originalSql))
            {
                // sets the fragment, which is the root node of the AST to the parsed SQL
                // the parser then reads the SQL from the string reader and helps the fragment construct the branches and leaves of the AST
                // ^ tokenization
                // these components are the different parts of the SQL statement, like SELECT, FROM, WHERE, etc.

                fragment = parser.Parse(reader, out errors);
            }

            if (errors != null && errors.Count > 0)
                return originalSql;

            var script = fragment as TSqlScript;
            if (script == null || script.Batches.Count == 0)
                return originalSql;

            var batch = script.Batches[0];
            if (batch.Statements.Count == 0)
                return originalSql;

            var select = batch.Statements[0] as SelectStatement;
            if (select == null)
                return originalSql;

            var querySpec = select.QueryExpression as QuerySpecification;

            if (querySpec == null)
                return originalSql;

            // builds the played at condition
            var condition = new BooleanComparisonExpression
            {
                ComparisonType = BooleanComparisonType.GreaterThan,
                FirstExpression = new ColumnReferenceExpression
                {
                    MultiPartIdentifier = new MultiPartIdentifier
                    {
                        Identifiers = { new Identifier { Value = "PlayedAt" } }
                    }
                },

                SecondExpression = new StringLiteral
                {

                    Value = lastRun.ToString("yyyy-MM-dd HH:mm:ss")

                }

            };
            // end of played at condition

            // adds the where clause to query
            if (querySpec.WhereClause == null)
            {
                querySpec.WhereClause = new WhereClause { SearchCondition = condition };
            }
            else
            {
                querySpec.WhereClause.SearchCondition =
                    new BooleanBinaryExpression
                    {
                        BinaryExpressionType = BooleanBinaryExpressionType.And,
                        FirstExpression = querySpec.WhereClause.SearchCondition,
                        SecondExpression = condition
                    };
            }
            // where clause addition end

            string newSql;
            // generates SQL from the modified AST
            var generator = new Sql150ScriptGenerator();
            generator.GenerateScript(fragment, out newSql);

            return newSql;

        }
    }
    

}
