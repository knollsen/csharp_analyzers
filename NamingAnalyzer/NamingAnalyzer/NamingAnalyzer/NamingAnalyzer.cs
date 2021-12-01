using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NamingAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamingAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Naming";

        public const string DisallowedTermsDiagnosticId = "NamingAnalyzerDisallowedTerms";

        private static readonly LocalizableString DisallowedTermsTitle = new LocalizableResourceString(nameof(Resources.DisallowedTermsAnalyzerTitle), 
            Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString DisallowedTermsMessageFormat =
            new LocalizableResourceString(nameof(Resources.DisallowedTermsAnalyzerMessageFormat), Resources.ResourceManager,
                typeof(Resources));

        private static readonly LocalizableString DisallowedTermsDescription =
            new LocalizableResourceString(nameof(Resources.DisallowedTermsAnalyzerDescription), Resources.ResourceManager,
                typeof(Resources));

        private static readonly DiagnosticDescriptor DisallowedTermsRule = new DiagnosticDescriptor(DisallowedTermsDiagnosticId, DisallowedTermsTitle, DisallowedTermsMessageFormat,
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DisallowedTermsDescription);


        public const string DisallowedSuffixDiagnosticId = "NamingAnalyzerDisallowedSuffix";

        private static readonly LocalizableString DisallowedSuffixTitle = 
            new LocalizableResourceString(nameof(Resources.DisallowedSuffixAnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString DisallowedSuffixMessageFormat =
            new LocalizableResourceString(nameof(Resources.DisallowedSuffixAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString DisallowedSuffixDescription =
            new LocalizableResourceString(nameof(Resources.DisallowedSuffixAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor DisallowedSuffixRule = new DiagnosticDescriptor(DisallowedSuffixDiagnosticId, DisallowedSuffixTitle, DisallowedSuffixMessageFormat,
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DisallowedSuffixDescription);

        private static readonly DiagnosticDescriptor MissingFileRule =
            new DiagnosticDescriptor("NamingAnalyzerConfigMissing", "namingConfig.json missing",
                "The file namingConfig.json could not be found or has the wrong structure", "Naming", DiagnosticSeverity.Warning, true);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DisallowedTermsRule, DisallowedSuffixRule, MissingFileRule);

        private NamingConfig Config = null;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            // read config
            if (this.Config == null)
            {
                // can either come from the context's additional files (preferable)
                string configContent;
                if (context.Options.AdditionalFiles.Any(file => Path.GetFileName(file.Path).Equals("namingConfig.json")))
                {
                    configContent = context.Options.AdditionalFiles.First(file =>
                        Path.GetFileName(file.Path).Equals("namingConfig.json"))
                        .GetText().ToString();
                }
                // or directly from the file system
                else
                {
                    var currentFiles = Directory.GetFiles(".");

                    var configFile = currentFiles.SingleOrDefault(x => x.EndsWith("namingConfig.json"));

                    // report a warning and return if we can't find the file
                    if (configFile == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(MissingFileRule, Location.None));
                        return;
                    }

                    configContent = File.ReadAllText(configFile);
                }

                var config = JsonConvert.DeserializeObject<NamingConfig>(configContent);

                // if the config file does not contain the correct structure, return as well
                if (config == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MissingFileRule, Location.None));
                    return;
                }

                this.Config = config;
            }
            
            var namedType = context.Symbol;

            // check for disallowed terms in class names
            foreach (var disallowedTerm in this.Config.DisallowedTermsInClassNames)
            {
                if (namedType.Name.Contains(disallowedTerm))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DisallowedTermsRule, namedType.Locations.First(), namedType.Name, disallowedTerm));

                    break;
                }
            }

            var syntaxNode = namedType.DeclaringSyntaxReferences.First().GetSyntax();

            // for class declarations, check for members with disallowed suffixes
            if (this.Config.DisallowedSuffixesInMembersType != null && syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                var name = classDeclaration.Identifier.Text;

                // check if there is a configuration for this class
                if (this.Config.DisallowedSuffixesInMembersType.Any(x => name.EndsWith(x.ClassSuffix)))
                {
                    var config = this.Config.DisallowedSuffixesInMembersType.First(x => name.EndsWith(x.ClassSuffix));

                    var members = classDeclaration.Members;

                    // check if a member is of a type with a disallowed suffix
                    foreach (var member in members)
                    {
                        if (member is PropertyDeclarationSyntax propertyDeclaration)
                        {
                            var disallowedSuffix =
                                config.DisallowedMemberSuffixes.FirstOrDefault(x =>
                                    propertyDeclaration.Type.ToString().EndsWith(x));
                            if (disallowedSuffix != null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(DisallowedSuffixRule, member.GetLocation(),
                                    namedType.Name, disallowedSuffix));
                            }
                        }

                        if (member is FieldDeclarationSyntax fieldDeclaration)
                        {
                            var disallowedSuffix = config.DisallowedMemberSuffixes.FirstOrDefault(x =>
                                fieldDeclaration.Declaration.Type.ToString().EndsWith(x));
                            if (disallowedSuffix != null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(DisallowedSuffixRule, member.GetLocation(),
                                    namedType.Name, disallowedSuffix));
                            }
                        }
                    }
                }
            }
        }
    }

    public class NamingConfig
    {
        public string[] DisallowedTermsInClassNames { get; set; }

        public MemberSuffixConfig[] DisallowedSuffixesInMembersType { get; set; }
    }

    public class MemberSuffixConfig
    {
        public string ClassSuffix { get; set; }

        public string[] DisallowedMemberSuffixes { get; set; }
    }
}
