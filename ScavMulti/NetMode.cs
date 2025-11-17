using System;

namespace ScavMulti;

public static class NetMode
{
	[Flags]
	public enum ModeClass
	{
		Offline = (1 << 0),
		Online = (1 << 1),
		IAmTheServer = Online | (1 << 2),
		IAmTheClient = Online | (1 << 3),
	}

	public static ModeClass Mode { get; private set; } = NetMode.ModeClass.Offline;
	public static bool Offline => Mode == ModeClass.Offline;
	public static bool Online => (Mode & ModeClass.Online) != 0;
	public static bool OnlineAndPlaying => (Mode & ModeClass.Online) != 0 && GameFlowManager.IsPlaying;
	public static bool IAmTheServer => Mode == ModeClass.IAmTheServer;
	public static bool IAmTheServerAndPlaying => Mode == ModeClass.IAmTheServer && GameFlowManager.IsPlaying;
	public static bool IAmTheClient => Mode == ModeClass.IAmTheClient;
	public static bool IAmTheClientAndPlaying => Mode == ModeClass.IAmTheClient && GameFlowManager.IsPlaying;

	internal static void SetMode(ModeClass newMode)
	{
		if (!Enum.IsDefined(typeof(ModeClass), newMode))
			throw new InvalidOperationException($"Invalid newState of value {(int)newMode}");
		Mode = newMode;
	}
}
