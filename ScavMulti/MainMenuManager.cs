using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HarmonyLib;

namespace ScavMulti;

[HarmonyPatch]
public static class MainMenuManager
{
	public static PreRunScript MenuInstance { get; private set; }
	static TextMeshProUGUI _errorLabel;

	/// <summary>
	/// Sets up the UI required to connect to an instance in the game's main menu
	/// </summary>
	[HarmonyPostfix]
	[HarmonyPatch(typeof(global::PreRunScript), nameof(global::PreRunScript.Start))]
	static void PreRunScript_Start_Postfix(PreRunScript __instance)
	{
		MenuInstance = __instance;

		UiUtils.DefaultBackgroundSprite = __instance.loadButton.GetComponent<Image>().sprite;
		UiUtils.ReferenceText = __instance.loadButton.GetComponentInChildren<TextMeshProUGUI>(true);
		var canvas = __instance.GetComponent<Canvas>();

		var netCanvasObject = UiUtils.CreateAutoGrowingUI(canvas.transform, "ScavMultiUI", ContentAlignment.TopLeft, new Vector2(100, -100), 5, new RectOffset(15, 22, 5, 15));

		UiUtils.CreateTMPLabel(netCanvasObject.transform, "Label", "ScavMulti");

		var inputField = UiUtils.CreateInputField(netCanvasObject.transform, customWidth: 220, prompt: "Enter an IP address");
		inputField.text = "127.0.0.1:5000";

		inputField.characterValidation = TMP_InputField.CharacterValidation.CustomValidator;
		inputField.inputValidator = ScriptableObject.CreateInstance<Ipv4Validator>();
		
		var connectButton = UiUtils.CreateButton(netCanvasObject.transform, "btn", "Connect", null);

		_errorLabel = UiUtils.CreateTMPLabel(netCanvasObject.transform, "errorLabel", "", keepDefault: true, overrideColor: Color.red);

		connectButton.onClick.AddListener(() =>
		{
			OnConnectClicked?.Invoke(inputField.text);
		});
		UiUtils.ForceRealodObject(netCanvasObject);

		/*
		// this is something i made for testing with two clients on the same computer,
		// automatically repositions windows and starts a game on the first client
		var res = Screen.currentResolution;
		Screen.SetResolution(res.width / 2, res.height / 2, FullScreenMode.Windowed, res.refreshRateRatio);
		Application.runInBackground = true;
		var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
		var otherProcess = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName)
			.Where(x => x != currentProcess).FirstOrDefault();
		if (otherProcess != null && currentProcess.StartTime > otherProcess.StartTime)
			Screen.MoveMainWindowTo(Screen.mainWindowDisplayInfo, new Vector2Int(Screen.width, 0));
		else
		{
			Screen.MoveMainWindowTo(Screen.mainWindowDisplayInfo, new Vector2Int(0, 0));
			__instance.StartRun();
			return;
		}
		*/
	}

	public delegate void OnConnectClickedDelegate(string ipAddress);
	public static event OnConnectClickedDelegate OnConnectClicked;

	public static void SetConnectErrorText(string errorText)
	{
		if (_errorLabel)
			_errorLabel.text = errorText;
	}

	private class Ipv4Validator : TMP_InputValidator
	{
		public override char Validate(ref string text, ref int pos, char ch)
		{
			if ((!char.IsDigit(ch) && ch != '.' && ch != ':') || text.Length > 22)
				return '\0';
			text = text.Insert(pos, new string(ch, 1));
			pos++;
			return ch;
		}
	}
}
