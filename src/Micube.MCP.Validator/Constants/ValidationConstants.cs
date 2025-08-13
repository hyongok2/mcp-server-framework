namespace Micube.MCP.Validator.Constants;

public static class ValidationConstants
{
    public static class ErrorCodes
    {
        public static class Orchestrator
        {
            public const string ValidatorFailed = "ORCH001";
            public const string SingleValidationCrashed = "ORCH002";
            public const string DirectoryProcessingError = "ORCH003";
            public const string DirectoryValidationCrashed = "ORCH004";
            public const string DllProcessingFailed = "ORCH005";
            public const string DirectoryNotFound = "ORCH010";
            public const string NoDllFilesFound = "ORCH011";
            public const string NoMcpToolGroupAttribute = "ORCH012";
            public const string ManifestFileNotFound = "ORCH013";
            public const string ProcessingCrashed = "ORCH999";
        }

        public static class Manifest
        {
            public const string PathNotSpecified = "MAN001";
            public const string FileNotFound = "MAN002";
            public const string EmptyFile = "MAN003";
            public const string InvalidJsonFormat = "MAN004";
            public const string DeserializationFailed = "MAN005";
            public const string MissingGroupName = "MAN010";
            public const string GroupNameTooLong = "MAN011";
            public const string MissingDescription = "MAN012";
            public const string MissingToolsArray = "MAN013";
            public const string EmptyToolsArray = "MAN014";
            public const string MissingToolName = "MAN020";
            public const string DuplicateToolName = "MAN021";
            public const string InvalidToolNameFormat = "MAN022";
            public const string MissingToolDescription = "MAN023";
            public const string StructuredOutputWithoutSchema = "MAN024";
            public const string EmptyParameterName = "MAN031";
            public const string DuplicateParameterName = "MAN032";
            public const string InvalidParameterType = "MAN033";
            public const string RequiredAfterOptional = "MAN034";
            public const string MissingParameterDescription = "MAN035";
            public const string InvalidVersion = "MAN041";
            public const string UnexpectedError = "MAN999";
        }

        public static class Dll
        {
            public const string PathNotSpecified = "DLL001";
            public const string FileNotFound = "DLL002";
            public const string InvalidAssemblyFormat = "DLL003";
            public const string LoadFailed = "DLL004";
            public const string TypeLoadWarning = "DLL005";
            public const string AnalysisWarning = "DLL006";
            public const string NoToolGroupImplementation = "DLL010";
            public const string MissingMcpToolGroupAttribute = "DLL020";
            public const string EmptyGroupName = "DLL021";
            public const string EmptyManifestPath = "DLL022";
            public const string MissingRequiredConstructor = "DLL023";
            public const string NotExtendingBaseToolGroup = "DLL024";
            public const string NoMcpToolMethods = "DLL030";
            public const string DuplicateToolName = "DLL031";
            public const string InvalidReturnType = "DLL032";
            public const string TooManyParameters = "DLL033";
            public const string MissingSdkReference = "DLL040";
            public const string DependencyAnalysisFailed = "DLL041";
            public const string SdkVersionMismatch = "DLL050";
            public const string SdkCompatibilityCheck = "DLL051";
            public const string UnexpectedError = "DLL999";
        }

        public static class Info
        {
            public const string OrchestratorInfo = "ORCH100";
            public const string SkippedDependencyDll = "ORCH102";
            public const string ValidationComplete = "ORCH103";
            public const string ManifestValidated = "MAN100";
            public const string AssemblyLoaded = "DLL100";
            public const string ToolGroupFound = "DLL101";
            public const string ExtendsBaseToolGroup = "DLL102";
            public const string ToolMethodFound = "DLL103";
            public const string ExternalDependencies = "DLL104";
            public const string SdkVersionInfo = "DLL105";
        }
    }

    public static class FilePatterns
    {
        public const string DllExtension = "*.dll";
        public const string JsonExtension = "*.json";
        public const string ObjDirectory = "\\obj\\";
        public const string ObjDirectoryUnix = "/obj/";
    }

    public static class AssemblyNames
    {
        public const string MicubeMcpSdk = "Micube.MCP.SDK";
        public const string MicubeMcpSdkDll = "Micube.MCP.SDK.dll";
        public const string SystemPrefix = "System";
        public const string MicrosoftPrefix = "Microsoft";
    }

    public static class ValidationLimits
    {
        public const int MaxGroupNameLength = 100;
        public const int MaxToolMethodParameters = 1;
    }

    public static class RegexPatterns
    {
        public const string ValidToolName = @"^[a-zA-Z][a-zA-Z0-9_]*$";
        public const string SemanticVersion = @"^\d+\.\d+(\.\d+)?(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?$";
    }

    public static class Messages
    {
        public const string PressAnyKeyToExit = "Press any key to exit...";
        public const string ValidatingMcpTools = "Validating MCP tools...";
        public const string ValidationFailed = "VALIDATION FAILED";
        public const string CriticalError = "CRITICAL ERROR";
        public const string FatalError = "FATAL ERROR";
        public const string HelpDisplayed = "Help displayed";
        public const string VersionDisplayed = "Version displayed";
        public const string ApplicationStarted = "Application started";
        public const string ValidationCompleted = "Validation completed";
        public const string ApplicationEnding = "Application ending";
    }
}