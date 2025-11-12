using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ScavMulti.PreloadPatcher;

// here we're writing stubs that we're gonna hook in the main code
// this is useful for Unity specific functions like OnDestroy
// that don't exist in the assembly
// unfortunately we can't add instance fields because they crash
// the game immediately
public static class PreloadPatcher
{
    public static IEnumerable<string> TargetDLLs { get; } = new[] {"Assembly-CSharp.dll"};

	static readonly (string typeName, string methodName)[] StubDefinitions = {
	};

	public static void Patch(AssemblyDefinition assembly)
    {
		var voidType = assembly.MainModule.TypeSystem.Void;

		foreach (var tuple in StubDefinitions)
		{
			var methodDef = new MethodDefinition(
				tuple.methodName,
				MethodAttributes.Public,
				voidType
			);
			methodDef.Body.InitLocals = true;
			var processor = methodDef.Body.GetILProcessor();
			processor.Emit(OpCodes.Ret);
			assembly.MainModule.GetType(tuple.typeName).Methods.Add(methodDef);
		}

	}
}
