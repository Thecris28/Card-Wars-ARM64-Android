using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JsonFx.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class CardWarsProfile
{
	public string id;
	public string displayName;
	public bool builtIn;
	public bool allContentUnlocked;
	public bool infiniteCoins;
	public bool infiniteGems;
	public bool infiniteStamina;
	public int minimumCoins;
	public int minimumGems;
	public long createdUtcTicks;
	public long lastPlayedUtcTicks;
}

[Serializable]
internal class CardWarsProfileCollection
{
	public List<CardWarsProfile> profiles = new List<CardWarsProfile>();
}

public class CardWarsProfileManager : MonoBehaviour
{
	private const string ProfilesKey = "CardWars.LocalProfiles.V1";
	private const string ActiveProfileKey = "CardWars.ActiveProfile.V1";
	private const string UnlimitedProfileId = "unlimited";
	private const string StoryProfileId = "story";
	private const string LegacyGameFile = "game.json";
	private static CardWarsProfileCollection data;
	private static CardWarsProfileManager instance;

	private bool menuVisible;
	private bool createMode;
	private int selectedIndex;
	private string newProfileName = "Nuevo aventurero";
	private string pendingImportJson;
	private string importStatusMessage;
	private bool isSavePickerOpen;
	private bool newUnlockAll;
	private bool newInfiniteCoins;
	private bool newInfiniteGems;
	private bool newInfiniteStamina = true;
	private int newMinimumCoins;
	private int newMinimumGems;
	private Vector2 profileScroll;
	private Vector2 detailScroll;
	private string resetConfirmationId;
	private GUIStyle titleStyle;
	private GUIStyle headingStyle;
	private GUIStyle labelStyle;
	private GUIStyle smallStyle;
	private GUIStyle buttonStyle;
	private GUIStyle selectedButtonStyle;
	private GUIStyle panelStyle;
	private GUIStyle toggleStyle;
	private Texture2D panelTexture;
	private Texture2D goldTexture;
	private Texture2D selectedTexture;
	private int styledWidth;
	private int styledHeight;

	public static CardWarsProfile ActiveProfile
	{
		get
		{
			EnsureProfiles();
			string activeId = PlayerPrefs.GetString(ActiveProfileKey, UnlimitedProfileId);
			CardWarsProfile profile = FindProfile(activeId);
			return profile ?? data.profiles[0];
		}
	}

	public static bool IsMenuVisible
	{
		get { return instance != null && instance.menuVisible; }
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		EnsureProfiles();
		if (instance != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("CardWarsProfileManager");
		gameObject.hideFlags = HideFlags.DontSave;
		DontDestroyOnLoad(gameObject);
		instance = gameObject.AddComponent<CardWarsProfileManager>();
	}

	private void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(base.gameObject);
			return;
		}
		instance = this;
		DontDestroyOnLoad(base.gameObject);
		EnsureProfiles();
		if (string.IsNullOrEmpty(newProfileName) || newProfileName == "Nuevo aventurero")
		{
			newProfileName = Localize("Nuevo aventurero", "New adventurer");
		}
		selectedIndex = Mathf.Max(0, FindProfileIndex(ActiveProfile.id));
		menuVisible = true;
	}

	private static bool IsSpanish()
	{
		return Application.systemLanguage == SystemLanguage.Spanish;
	}

	private static string Localize(string spanish, string english)
	{
		return IsSpanish() ? spanish : english;
	}

	private static string GetProfileDisplayName(CardWarsProfile profile)
	{
		if (profile.id == UnlimitedProfileId)
		{
			return Localize("Todo ilimitado", "Unlimited");
		}
		if (profile.id == StoryProfileId)
		{
			return Localize("Historia desde cero", "Story from scratch");
		}
		return profile.displayName;
	}

	public static string GetActiveGameFileName()
	{
		CardWarsProfile profile = ActiveProfile;
		if (profile.id == UnlimitedProfileId)
		{
			// Keep using the old save for the unlimited profile so existing progress is preserved.
			return LegacyGameFile;
		}
		return "game_profile_" + SanitizeId(profile.id) + ".json";
	}

	public static void ShowProfiles()
	{
		if (instance != null)
		{
			instance.selectedIndex = Mathf.Max(0, FindProfileIndex(ActiveProfile.id));
			instance.createMode = false;
			instance.menuVisible = true;
		}
	}

	public static void ApplyActiveProfile(PlayerInfoScript playerInfo)
	{
		if (playerInfo == null)
		{
			return;
		}
		CardWarsProfile profile = ActiveProfile;
		if (profile.minimumCoins > 0 && playerInfo.Coins < profile.minimumCoins)
		{
			playerInfo.Coins = profile.minimumCoins;
		}
		if (profile.minimumGems > 0 && playerInfo.Gems < profile.minimumGems)
		{
			playerInfo.Gems = profile.minimumGems;
		}
	}

	private static void EnsureProfiles()
	{
		if (data != null)
		{
			return;
		}
		string json = PlayerPrefs.GetString(ProfilesKey, string.Empty);
		if (!string.IsNullOrEmpty(json))
		{
			try
			{
				data = JsonUtility.FromJson<CardWarsProfileCollection>(json);
			}
			catch
			{
				data = null;
			}
		}
		if (data == null)
		{
			data = new CardWarsProfileCollection();
		}
		if (data.profiles == null)
		{
			data.profiles = new List<CardWarsProfile>();
		}
		if (FindProfile(UnlimitedProfileId) == null)
		{
			data.profiles.Insert(0, CreateProfile(UnlimitedProfileId, "Todo ilimitado", true, true, true, true, true, 1000000000, 1000000000));
		}
		if (FindProfile(StoryProfileId) == null)
		{
			data.profiles.Add(CreateProfile(StoryProfileId, "Historia desde cero", true, false, false, false, false, 0, 0));
		}
		if (!PlayerPrefs.HasKey(ActiveProfileKey) || FindProfile(PlayerPrefs.GetString(ActiveProfileKey)) == null)
		{
			PlayerPrefs.SetString(ActiveProfileKey, UnlimitedProfileId);
		}
		SaveProfiles();
	}

	private static CardWarsProfile CreateProfile(string id, string name, bool builtIn, bool unlockAll, bool coins, bool gems, bool stamina, int minimumCoins, int minimumGems)
	{
		CardWarsProfile profile = new CardWarsProfile();
		profile.id = id;
		profile.displayName = name;
		profile.builtIn = builtIn;
		profile.allContentUnlocked = unlockAll;
		profile.infiniteCoins = coins;
		profile.infiniteGems = gems;
		profile.infiniteStamina = stamina;
		profile.minimumCoins = minimumCoins;
		profile.minimumGems = minimumGems;
		profile.createdUtcTicks = DateTime.UtcNow.Ticks;
		profile.lastPlayedUtcTicks = 0L;
		return profile;
	}

	private static CardWarsProfile FindProfile(string id)
	{
		if (data == null || data.profiles == null)
		{
			return null;
		}
		for (int i = 0; i < data.profiles.Count; i++)
		{
			if (data.profiles[i] != null && data.profiles[i].id == id)
			{
				return data.profiles[i];
			}
		}
		return null;
	}

	private static int FindProfileIndex(string id)
	{
		EnsureProfiles();
		for (int i = 0; i < data.profiles.Count; i++)
		{
			if (data.profiles[i].id == id)
			{
				return i;
			}
		}
		return -1;
	}

	private static string SanitizeId(string id)
	{
		char[] chars = id.ToCharArray();
		for (int i = 0; i < chars.Length; i++)
		{
			if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_')
			{
				chars[i] = '_';
			}
		}
		return new string(chars);
	}

	private static void SaveProfiles()
	{
		PlayerPrefs.SetString(ProfilesKey, JsonUtility.ToJson(data));
		PlayerPrefs.Save();
	}

	private void ActivateProfile(CardWarsProfile profile)
	{
		if (profile == null)
		{
			return;
		}
		CardWarsProfile current = ActiveProfile;
		PlayerInfoScript playerInfo = PlayerInfoScript.GetInstance();
		if (current.id != profile.id && IsPlayerReady() && playerInfo != null)
		{
			playerInfo.Save();
		}
		profile.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
		PlayerPrefs.SetString(ActiveProfileKey, profile.id);
		SaveProfiles();
		if (current.id == profile.id)
		{
			if (IsPlayerReady())
			{
				CardWarsMod.Apply(playerInfo);
			}
			menuVisible = false;
			return;
		}
		menuVisible = false;
		if (LanRealtimeManager.Instance != null)
		{
			LanRealtimeManager.Instance.Disconnect();
		}
		if (playerInfo != null)
		{
			playerInfo.ReloadGame();
		}
		else
		{
			SceneManager.LoadScene("AppReloadScene");
		}
	}

	private void ResetProfile(CardWarsProfile profile)
	{
		if (profile == null)
		{
			return;
		}
		if (resetConfirmationId != profile.id)
		{
			resetConfirmationId = profile.id;
			return;
		}
		string activeFile = profile.id == UnlimitedProfileId ? LegacyGameFile : "game_profile_" + SanitizeId(profile.id) + ".json";
		try
		{
			PlayerInfoScript playerInfo = PlayerInfoScript.GetInstance();
			SessionManager sessionManager = SessionManager.GetInstance();
			if (sessionManager != null)
			{
				string path = sessionManager.GetPlayerDataPath(activeFile);
				if (!string.IsNullOrEmpty(path) && File.Exists(path))
				{
					File.Delete(path);
				}
			}
			resetConfirmationId = null;
			if (ActiveProfile.id == profile.id)
			{
				menuVisible = false;
				if (playerInfo != null)
				{
					playerInfo.ReloadGame();
				}
			}
		}
		catch (Exception exception)
		{
			Debug.LogWarning("No se pudo reiniciar el perfil: " + exception.Message);
		}
	}

	private void CreateCustomProfile()
	{
		string trimmedName = newProfileName.Trim();
		if (trimmedName.Length == 0)
		{
			trimmedName = "Nuevo aventurero";
		}
		string id = "custom_" + DateTime.UtcNow.Ticks;
		CardWarsProfile profile = CreateProfile(id, trimmedName, false, newUnlockAll, newInfiniteCoins, newInfiniteGems, newInfiniteStamina, newMinimumCoins, newMinimumGems);
		data.profiles.Add(profile);
		SaveProfiles();
		selectedIndex = data.profiles.Count - 1;
		createMode = false;
	}

	private void DeleteProfile(CardWarsProfile profile)
	{
		if (profile == null || profile.builtIn || profile.id == ActiveProfile.id)
		{
			return;
		}
		if (resetConfirmationId != "delete_" + profile.id)
		{
			resetConfirmationId = "delete_" + profile.id;
			return;
		}
		ResetProfileFile(profile);
		data.profiles.Remove(profile);
		selectedIndex = Mathf.Clamp(selectedIndex - 1, 0, data.profiles.Count - 1);
		resetConfirmationId = null;
		SaveProfiles();
	}

	private static void ResetProfileFile(CardWarsProfile profile)
	{
		try
		{
			SessionManager sessionManager = SessionManager.GetInstance();
			if (sessionManager == null)
			{
				return;
			}
			string file = profile.id == UnlimitedProfileId ? LegacyGameFile : "game_profile_" + SanitizeId(profile.id) + ".json";
			string path = sessionManager.GetPlayerDataPath(file);
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception exception)
		{
			Debug.LogWarning("No se pudo borrar el guardado del perfil: " + exception.Message);
		}
	}

	private void AddCurrency(int coins, int gems)
	{
		PlayerInfoScript playerInfo = PlayerInfoScript.GetInstance();
		if (playerInfo == null || !IsPlayerReady())
		{
			return;
		}
		if (coins > 0)
		{
			playerInfo.Coins += coins;
		}
		if (gems > 0)
		{
			playerInfo.Gems += gems;
		}
		playerInfo.Save();
	}

	private void OpenSavePicker()
	{
		if (!IsPlayerReady())
		{
			importStatusMessage = Localize("Espera a que el juego termine de cargar para importar.", "Wait for the game to finish loading before importing.");
			return;
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		try
		{
			AndroidJavaClass picker = new AndroidJavaClass("com.cardwars.saveimport.SavePicker");
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			isSavePickerOpen = true;
			picker.CallStatic("launch", activity);
			importStatusMessage = Localize("Selecciona el JSON descifrado en el explorador de Android.", "Choose the decoded JSON in the Android file picker.");
		}
		catch (Exception exception)
		{
			isSavePickerOpen = false;
			Debug.LogError("No se pudo abrir el selector de partidas: " + exception);
			importStatusMessage = Localize("No se pudo abrir el selector de archivos.", "Could not open the file picker.");
		}
#else
		importStatusMessage = Localize("El selector de archivos está disponible en la versión Android.", "The file picker is available in the Android version.");
#endif
	}

	public void OnSaveFilePicked(string encodedJson)
	{
		isSavePickerOpen = false;
		try
		{
			string json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedJson));
			ValidateImportSave(json);
			pendingImportJson = json;
			importStatusMessage = Localize(
				"JSON compatible seleccionado. Al confirmar reemplazará el perfil activo y se intentará sincronizar con el servidor.",
				"Compatible JSON selected. Confirming will replace the active profile and attempt to sync it with the server.");
		}
		catch (Exception exception)
		{
			pendingImportJson = null;
			Debug.LogError("No se pudo leer el JSON seleccionado: " + exception);
			importStatusMessage = Localize("No se pudo usar el archivo: ", "Could not use the file: ") + exception.Message;
		}
	}

	public void OnSaveFilePickerCancelled(string unused)
	{
		isSavePickerOpen = false;
		importStatusMessage = Localize("Selección cancelada.", "Selection cancelled.");
	}

	public void OnSaveFilePickerError(string encodedError)
	{
		isSavePickerOpen = false;
		string errorMessage;
		try
		{
			errorMessage = Encoding.UTF8.GetString(Convert.FromBase64String(encodedError));
		}
		catch (FormatException)
		{
			errorMessage = Localize("Error desconocido al leer el archivo.", "Unknown error while reading the file.");
		}
		Debug.LogError("El selector de partidas no pudo leer el archivo: " + errorMessage);
		importStatusMessage = Localize("No se pudo leer el archivo: ", "Could not read the file: ") + errorMessage;
	}

	private static void ValidateImportSave(string json)
	{
		Dictionary<string, object> saveData = JsonReader.Deserialize<Dictionary<string, object>>(json);
		object versionValue;
		int saveVersion;
		if (saveData == null || !saveData.TryGetValue("PlayerInfoVersion", out versionValue))
		{
			throw new InvalidDataException(Localize("El JSON no contiene PlayerInfoVersion.", "The JSON does not contain PlayerInfoVersion."));
		}
		if (!int.TryParse(Convert.ToString(versionValue, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out saveVersion) ||
			saveVersion < 1 ||
			saveVersion > 4 ||
			(!saveData.ContainsKey("Inventory") && !saveData.ContainsKey("Leaders")))
		{
			throw new InvalidDataException(Localize(
				"El JSON no parece un guardado compatible de Card Wars (versiones 1 a 4).",
				"This JSON does not appear to be a compatible Card Wars save (versions 1 to 4)."));
		}
	}

	private void ImportSave(CardWarsProfile profile, string json)
	{
		if (profile == null || profile.id != ActiveProfile.id || !IsPlayerReady())
		{
			importStatusMessage = Localize("Activa este perfil y espera a que el juego termine de cargar.", "Activate this profile and wait for the game to finish loading.");
			return;
		}

		string activeSavePath = Path.Combine(Application.persistentDataPath, GetActiveGameFileName());
		string backupPath = null;
		bool hadActiveSave = File.Exists(activeSavePath);
		PlayerInfoScript playerInfo = PlayerInfoScript.GetInstance();
		if (playerInfo == null)
		{
			importStatusMessage = Localize("No se pudo acceder a los datos del jugador.", "Could not access player data.");
			return;
		}

		string previousState = null;
		bool importCommitted = false;
		try
		{
			previousState = playerInfo.Serialize();
			ValidateImportSave(json);

			if (hadActiveSave)
			{
				backupPath = activeSavePath + ".before_import_" + DateTime.UtcNow.Ticks + ".bak";
				File.Copy(activeSavePath, backupPath);
			}

			playerInfo.Deserialize(json);
			CardWarsMod.Apply(playerInfo);
			string importedState = playerInfo.Serialize();
			SessionManager sessionManager = SessionManager.GetInstance();
			sessionManager.SetGameStateJson(importedState);
			importCommitted = true;
			playerInfo.Save();

			importStatusMessage = backupPath == null
				? Localize("Partida importada y guardada con la clave de esta instalación.", "Save imported and stored with this installation's key.")
				: Localize("Partida importada. Copia anterior: ", "Save imported. Previous save backup: ") + Path.GetFileName(backupPath);
			pendingImportJson = null;
		}
		catch (Exception exception)
		{
			if (!importCommitted && previousState != null)
			{
				try
				{
					playerInfo.Deserialize(previousState);
				}
				catch (Exception rollbackException)
				{
					Debug.LogError(Localize("No se pudo restaurar el estado anterior tras fallar la importación: ", "Could not restore the previous state after import failed: ") + rollbackException);
				}
			}
			if (!importCommitted)
			{
				try
				{
					if (backupPath != null && File.Exists(backupPath))
					{
						File.Copy(backupPath, activeSavePath, true);
					}
					else if (!hadActiveSave && File.Exists(activeSavePath))
					{
						File.Delete(activeSavePath);
					}
				}
				catch (Exception restoreException)
				{
					Debug.LogError(Localize("No se pudo restaurar el archivo de guardado anterior: ", "Could not restore the previous save file: ") + restoreException);
				}
			}
			Debug.LogError("No se pudo importar el guardado: " + exception);
			importStatusMessage = Localize("Error al importar: ", "Import failed: ") + exception.Message;
		}
	}

	private static bool IsPlayerReady()
	{
		try
		{
			SessionManager manager = SessionManager.GetInstance();
			return manager != null && manager.IsReady();
		}
		catch
		{
			return false;
		}
	}

	private static Texture2D MakeTexture(Color color)
	{
		Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		texture.hideFlags = HideFlags.DontSave;
		texture.SetPixel(0, 0, color);
		texture.Apply();
		return texture;
	}

	private void EnsureStyles()
	{
		if (panelTexture == null)
		{
			panelTexture = MakeTexture(new Color(0.14f, 0.09f, 0.05f, 0.97f));
			goldTexture = MakeTexture(new Color(0.76f, 0.45f, 0.08f, 1f));
			selectedTexture = MakeTexture(new Color(0.35f, 0.18f, 0.06f, 1f));
			panelStyle = new GUIStyle(GUI.skin.box);
			panelStyle.normal.background = panelTexture;
			titleStyle = new GUIStyle(GUI.skin.label);
			titleStyle.alignment = TextAnchor.MiddleCenter;
			titleStyle.fontStyle = FontStyle.Bold;
			titleStyle.normal.textColor = new Color(1f, 0.82f, 0.25f);
			headingStyle = new GUIStyle(titleStyle);
			headingStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.wordWrap = true;
			labelStyle.normal.textColor = Color.white;
			smallStyle = new GUIStyle(labelStyle);
			smallStyle.normal.textColor = new Color(0.85f, 0.75f, 0.58f);
			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.normal.textColor = Color.white;
			buttonStyle.hover.textColor = Color.white;
			buttonStyle.active.textColor = Color.white;
			selectedButtonStyle = new GUIStyle(buttonStyle);
			selectedButtonStyle.normal.background = selectedTexture;
			toggleStyle = new GUIStyle(GUI.skin.toggle);
			toggleStyle.normal.textColor = Color.white;
			toggleStyle.onNormal.textColor = new Color(1f, 0.82f, 0.25f);
		}
		if (styledWidth == Screen.width && styledHeight == Screen.height)
		{
			return;
		}
		styledWidth = Screen.width;
		styledHeight = Screen.height;
		int font = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.028f), 18, 34);
		titleStyle.fontSize = font + 10;
		headingStyle.fontSize = font + 4;
		labelStyle.fontSize = font;
		smallStyle.fontSize = Mathf.Max(16, font - 4);
		buttonStyle.fontSize = font;
		selectedButtonStyle.fontSize = font;
		toggleStyle.fontSize = font;
	}

	private static Rect GetSafeArea()
	{
		Rect area = Screen.safeArea;
		return new Rect(area.x, Screen.height - area.yMax, area.width, area.height);
	}

	private bool IsBattleRunning()
	{
		try
		{
			return GameState.Instance != null && GameState.Instance.IsSetUp;
		}
		catch
		{
			return false;
		}
	}

	private void OnGUI()
	{
		GUI.depth = -11000;
		GUI.matrix = Matrix4x4.identity;
		EnsureStyles();
		Rect safe = GetSafeArea();
		float quickHeight = Mathf.Clamp(safe.height * 0.065f, 48f, 72f);
		if (!menuVisible)
		{
			return;
		}
		GUI.Box(safe, string.Empty, panelStyle);
		float margin = Mathf.Clamp(safe.width * 0.025f, 18f, 42f);
		GUILayout.BeginArea(new Rect(safe.x + margin, safe.y + margin * 0.5f, safe.width - margin * 2f, safe.height - margin));
		GUILayout.Label(Localize("CARD WARS · PERFILES", "CARD WARS · PROFILES"), titleStyle, GUILayout.Height(quickHeight));
		GUILayout.Label(Localize(
			"Cada perfil conserva por separado su historia, mazos, cartas y recursos.",
			"Each profile keeps its own story progress, decks, cards, and resources."), smallStyle);
		GUILayout.Space(8f);
		GUILayout.BeginHorizontal();
		DrawProfileList(Mathf.Clamp(safe.width * 0.30f, 260f, 430f));
		GUILayout.Space(18f);
		if (createMode)
		{
			DrawCreatePanel();
		}
		else
		{
			DrawProfileDetails();
		}
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}

	private void DrawProfileList(float width)
	{
		GUILayout.BeginVertical(panelStyle, GUILayout.Width(width), GUILayout.ExpandHeight(true));
		GUILayout.Label(Localize("AVENTUREROS", "ADVENTURERS"), headingStyle);
		profileScroll = GUILayout.BeginScrollView(profileScroll);
		for (int i = 0; i < data.profiles.Count; i++)
		{
			CardWarsProfile profile = data.profiles[i];
			string prefix = profile.id == ActiveProfile.id ? "★ " : string.Empty;
			GUIStyle style = i == selectedIndex && !createMode ? selectedButtonStyle : buttonStyle;
			if (GUILayout.Button(prefix + GetProfileDisplayName(profile), style, GUILayout.MinHeight(58f)))
			{
				selectedIndex = i;
				createMode = false;
				resetConfirmationId = null;
				if (profile.id != ActiveProfile.id)
				{
					pendingImportJson = null;
				}
			}
		}
		GUILayout.EndScrollView();
		if (GUILayout.Button(Localize("+ CREAR PERFIL", "+ CREATE PROFILE"), buttonStyle, GUILayout.MinHeight(58f)))
		{
			createMode = true;
			resetConfirmationId = null;
			pendingImportJson = null;
		}
		GUILayout.Space(8f);
		GUILayout.Label(Localize("IMPORTAR PARTIDA", "IMPORT SAVE"), headingStyle);
		GUILayout.Label(Localize(
			"Selecciona el JSON descifrado desde Descargas o cualquier carpeta accesible.",
			"Choose the decoded JSON from Downloads or any accessible folder."), smallStyle);
		CardWarsProfile selectedProfile = selectedIndex >= 0 && selectedIndex < data.profiles.Count
			? data.profiles[selectedIndex]
			: null;
		bool canImportSelectedProfile = selectedProfile != null &&
			selectedProfile.id == ActiveProfile.id &&
			IsPlayerReady() &&
			!isSavePickerOpen;
		GUI.enabled = canImportSelectedProfile;
		string importButtonText = pendingImportJson != null
			? Localize("CONFIRMAR IMPORTACIÓN", "CONFIRM IMPORT")
			: Localize("ELEGIR JSON", "CHOOSE JSON");
		if (GUILayout.Button(importButtonText, buttonStyle, GUILayout.MinHeight(58f)))
		{
			if (pendingImportJson != null)
			{
				ImportSave(selectedProfile, pendingImportJson);
			}
			else
			{
				OpenSavePicker();
			}
		}
		GUI.enabled = true;
		if (selectedProfile == null || selectedProfile.id != ActiveProfile.id)
		{
			GUILayout.Label(Localize(
				"Selecciona primero el perfil activo para importar en él.",
				"Select the active profile first to import into it."), smallStyle);
		}
		else if (!IsPlayerReady())
		{
			GUILayout.Label(Localize("Espera a que el juego termine de cargar para importar.", "Wait for the game to finish loading before importing."), smallStyle);
		}
		if (!string.IsNullOrEmpty(importStatusMessage))
		{
			GUILayout.Label(importStatusMessage, smallStyle);
		}
		GUILayout.EndVertical();
	}

	private void DrawCreatePanel()
	{
		GUILayout.BeginVertical(panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		detailScroll = GUILayout.BeginScrollView(detailScroll);
		GUILayout.Label(Localize("NUEVO PERFIL", "NEW PROFILE"), headingStyle);
		GUILayout.Label(Localize("Nombre", "Name"), labelStyle);
		newProfileName = GUILayout.TextField(newProfileName, GUILayout.MinHeight(52f));
		GUILayout.Space(10f);
		GUILayout.Label(Localize("Elige las reglas iniciales. Podrás cambiarlas después.", "Choose the starting rules. You can change them later."), smallStyle);
		newUnlockAll = DrawCheckbox(Localize("Desbloquear todo el contenido", "Unlock all content"), newUnlockAll);
		newInfiniteCoins = DrawCheckbox(Localize("Monedas infinitas", "Infinite coins"), newInfiniteCoins);
		newInfiniteGems = DrawCheckbox(Localize("Gemas infinitas", "Infinite gems"), newInfiniteGems);
		newInfiniteStamina = DrawCheckbox(Localize("Corazones/energía ilimitados", "Unlimited hearts/energy"), newInfiniteStamina);
		GUILayout.Space(12f);
		newMinimumCoins = DrawAmount(Localize("Monedas mínimas", "Minimum coins"), newMinimumCoins, 1000);
		newMinimumGems = DrawAmount(Localize("Gemas mínimas", "Minimum gems"), newMinimumGems, 100);
		GUILayout.EndScrollView();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(Localize("CANCELAR", "CANCEL"), buttonStyle, GUILayout.MinHeight(62f)))
		{
			createMode = false;
		}
		if (GUILayout.Button(Localize("CREAR", "CREATE"), buttonStyle, GUILayout.MinHeight(62f)))
		{
			CreateCustomProfile();
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	private int DrawAmount(string label, int value, int step)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label + ": " + value, labelStyle, GUILayout.ExpandWidth(true));
		if (GUILayout.Button("−", buttonStyle, GUILayout.Width(70f), GUILayout.Height(48f)))
		{
			value = Mathf.Max(0, value - step);
		}
		if (GUILayout.Button("+", buttonStyle, GUILayout.Width(70f), GUILayout.Height(48f)))
		{
			value = Mathf.Min(1000000000, value + step);
		}
		GUILayout.EndHorizontal();
		return value;
	}

	private bool DrawCheckbox(string label, bool value)
	{
		GUIStyle style = value ? selectedButtonStyle : buttonStyle;
		if (GUILayout.Button((value ? "☑  " : "☐  ") + label, style, GUILayout.MinHeight(48f)))
		{
			value = !value;
		}
		return value;
	}

	private void DrawProfileDetails()
	{
		selectedIndex = Mathf.Clamp(selectedIndex, 0, data.profiles.Count - 1);
		CardWarsProfile profile = data.profiles[selectedIndex];
		bool isActive = profile.id == ActiveProfile.id;
		GUILayout.BeginVertical(panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		detailScroll = GUILayout.BeginScrollView(detailScroll);
		GUILayout.Label(GetProfileDisplayName(profile).ToUpperInvariant(), headingStyle);
		GUILayout.Label(profile.id == StoryProfileId
			? Localize("Campaña limpia: empieza con el tutorial y desbloquea todo poco a poco.", "A fresh campaign: start with the tutorial and unlock content as you progress.")
			: Localize("Configura libremente las ayudas de este aventurero.", "Customize this adventurer's settings freely."), smallStyle);
		GUILayout.Space(10f);
		bool oldUnlockAll = profile.allContentUnlocked;
		bool oldInfiniteCoins = profile.infiniteCoins;
		bool oldInfiniteGems = profile.infiniteGems;
		bool oldInfiniteStamina = profile.infiniteStamina;
		int oldMinimumCoins = profile.minimumCoins;
		int oldMinimumGems = profile.minimumGems;
		profile.allContentUnlocked = DrawCheckbox(Localize("Desbloquear todo el contenido", "Unlock all content"), profile.allContentUnlocked);
		profile.infiniteCoins = DrawCheckbox(Localize("Monedas infinitas", "Infinite coins"), profile.infiniteCoins);
		profile.infiniteGems = DrawCheckbox(Localize("Gemas infinitas", "Infinite gems"), profile.infiniteGems);
		profile.infiniteStamina = DrawCheckbox(Localize("Corazones/energía ilimitados", "Unlimited hearts/energy"), profile.infiniteStamina);
		profile.minimumCoins = DrawAmount(Localize("Monedas mínimas", "Minimum coins"), profile.minimumCoins, 1000);
		profile.minimumGems = DrawAmount(Localize("Gemas mínimas", "Minimum gems"), profile.minimumGems, 100);
		if (oldUnlockAll != profile.allContentUnlocked || oldInfiniteCoins != profile.infiniteCoins || oldInfiniteGems != profile.infiniteGems || oldInfiniteStamina != profile.infiniteStamina || oldMinimumCoins != profile.minimumCoins || oldMinimumGems != profile.minimumGems)
		{
			SaveProfiles();
		}
		GUILayout.Space(8f);
		GUILayout.Label(Localize("BÓVEDA LOCAL · no realiza compras reales", "LOCAL VAULT · no real purchases"), headingStyle);
		GUILayout.BeginHorizontal();
		GUI.enabled = isActive;
		if (GUILayout.Button(Localize("+10 000 MONEDAS", "+10,000 COINS"), buttonStyle, GUILayout.MinHeight(54f)))
		{
			AddCurrency(10000, 0);
		}
		if (GUILayout.Button(Localize("+500 GEMAS", "+500 GEMS"), buttonStyle, GUILayout.MinHeight(54f)))
		{
			AddCurrency(0, 500);
		}
		GUI.enabled = true;
		GUILayout.EndHorizontal();
		if (!isActive)
		{
			GUILayout.Label(Localize("Activa el perfil para modificar su saldo.", "Activate this profile to change its balance."), smallStyle);
		}
		GUILayout.EndScrollView();
		if (GUILayout.Button(isActive
			? Localize("CONTINUAR CON ESTE PERFIL", "CONTINUE WITH THIS PROFILE")
			: Localize("ACTIVAR PERFIL", "ACTIVATE PROFILE"), buttonStyle, GUILayout.MinHeight(64f)))
		{
			ActivateProfile(profile);
		}
		GUILayout.BeginHorizontal();
		string resetText = resetConfirmationId == profile.id
			? Localize("CONFIRMAR REINICIO", "CONFIRM RESET")
			: Localize("REINICIAR DESDE CERO", "RESET TO DEFAULTS");
		if (GUILayout.Button(resetText, buttonStyle, GUILayout.MinHeight(56f)))
		{
			ResetProfile(profile);
		}
		if (!profile.builtIn && !isActive)
		{
			string deleteText = resetConfirmationId == "delete_" + profile.id
				? Localize("CONFIRMAR BORRADO", "CONFIRM DELETE")
				: Localize("BORRAR PERFIL", "DELETE PROFILE");
			if (GUILayout.Button(deleteText, buttonStyle, GUILayout.MinHeight(56f)))
			{
				DeleteProfile(profile);
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}
}
