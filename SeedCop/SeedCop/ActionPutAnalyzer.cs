﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SeedCop
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ActionPutAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "ActionPut";

		// You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
		// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
		private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ActionPutAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ActionPutAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ActionPutAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
		private const string Category = "Usage";

		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
		}

		private void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;

			if (classDeclaration.BaseList == null)
				return;

			if (classDeclaration.Identifier.Text.Contains("Query"))
				return;

			var isController = classDeclaration.BaseList.DescendantTokens().Any(p => p.Text == "ControllerBase");

			if (!isController)
				return;

			var commandActions = classDeclaration.Members
				.Where(p => p.IsKind(SyntaxKind.MethodDeclaration))
				.Select(p => (MethodDeclarationSyntax)p)
				.Where(p => !p.AttributeLists.Any(t => t.DescendantTokens().Any(r => r.Text == "NotCommand")));

			foreach (var methodDeclaration in commandActions)
			{
				bool hasPutAsync = methodDeclaration.Body.DescendantTokens().Any(p => p.Text == "PutAsync");

				if (!hasPutAsync)
					context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation()));
			}
		}
	}
}
