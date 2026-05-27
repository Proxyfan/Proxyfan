namespace Proxyfan.Domain.Scripting;

/// <summary>
///     Compiles user-supplied C# script source into an executable <see cref="IUserScript" />.
/// </summary>
public interface IUserScriptCompiler
{
    /// <summary>
    ///     Compiles the supplied request- and response-phase script bodies, returning either a
    ///     runnable <see cref="IUserScript" /> or a list of compilation diagnostics.
    /// </summary>
    /// <param name="displayName">The display name to assign to the compiled script.</param>
    /// <param name="requestScript">The C# code to run on the request phase. Empty disables the request hook.</param>
    /// <param name="responseScript">The C# code to run on the response phase. Empty disables the response hook.</param>
    /// <returns>A <see cref="ScriptCompilationResult" /> reporting success or failure.</returns>
    ScriptCompilationResult Compile(string displayName, string requestScript, string responseScript);
}
