﻿using Common.Logging;
using Roslyn.Scripting;
using System;
using System.Reflection;
using ScriptCs.Exceptions;

namespace ScriptCs.Engine.Roslyn
{
    using System.IO;

    public class RoslynScriptDllGeneratorEngine : RoslynScriptCompilerEngine
    {
        private IFileSystem _fileSystem;

        public RoslynScriptDllGeneratorEngine(IScriptHostFactory scriptHostFactory, ILog logger, IFileSystem fileSystem)
            : base(scriptHostFactory, logger)
        {
            _fileSystem = fileSystem;
        }

        protected override void ExecuteScriptInSession(Session session, byte[] exeBytes, byte[] pdbBytes, ScriptResult scriptResult)
        {
            _logger.DebugFormat("Writing assembly to {0}.", FileName);
            var dllName = FileName.Replace(Path.GetExtension(FileName), ".dll");
            _fileSystem.WriteAllBytes(dllName, exeBytes);
            var assembly = Assembly.LoadFrom(dllName);
            _logger.Debug("Retrieving compiled script class (reflection).");
            var type = assembly.GetType(CompiledScriptClass);
            _logger.Debug("Retrieving compiled script method (reflection).");
            var method = type.GetMethod(CompiledScriptMethod, BindingFlags.Static | BindingFlags.Public);

            try
            {
                this._logger.Debug("Invoking method.");
                scriptResult.ReturnValue = method.Invoke(null, new[] { session });
            }
            catch (Exception executeException)
            {
                scriptResult.ExecuteException = executeException;
                this._logger.Error("An error occurred when executing the scripts.");
                var message = string.Format(
                    "Exception Message: {0} {1}Stack Trace:{2}",
                    executeException.InnerException.Message,
                    Environment.NewLine,
                    executeException.InnerException.StackTrace);
                throw new ScriptExecutionException(message);
            }
        }
    }
}