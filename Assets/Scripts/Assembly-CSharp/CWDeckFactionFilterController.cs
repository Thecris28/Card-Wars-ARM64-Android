using System.Collections.Generic;
using UnityEngine;

public class CWDeckFactionFilterController : MonoBehaviour
{
	private const float FactionButtonScreenOffsetX = 250f;

	private CWDeckAddCards addCardsController;

	private GameObject filterButtonObject;

	private GameObject filterPanelObject;

	private UILabel buttonLabel;

	private UIButtonTween panelTween;

	private TweenPositionAnchored panelMotion;

	public static void EnsureInitialized(CWDeckAddCards targetAddCards)
	{
		if (targetAddCards == null)
		{
			return;
		}

		CWDeckFactionFilterController controller = targetAddCards.GetComponent<CWDeckFactionFilterController>();
		if (controller == null)
		{
			controller = targetAddCards.gameObject.AddComponent<CWDeckFactionFilterController>();
		}

		controller.Initialize(targetAddCards);
	}

	public void Initialize(CWDeckAddCards targetAddCards)
	{
		addCardsController = targetAddCards;
		if (FindAddPanelRoot() == null)
		{
			Debug.LogError("CWDeckFactionFilterController: The card grid is not inside B_1_AddPanel.");
			return;
		}

		if (filterButtonObject == null)
		{
			SetupUI();
		}

		ResetFilterState();
	}

	private void SetupUI()
	{
		Transform addPanelRoot = FindAddPanelRoot();
		if (addPanelRoot == null)
		{
			Debug.LogError("CWDeckFactionFilterController: The card grid is not inside B_1_AddPanel.");
			return;
		}

		Transform buttons = addPanelRoot.Find("Buttons");
		Transform sortButtonTransform = (buttons != null) ? buttons.Find("SortButton") : null;
		if (sortButtonTransform == null)
		{
			Debug.LogError("CWDeckFactionFilterController: Could not find B_1_AddPanel/Buttons/SortButton.");
			return;
		}

		GameObject sortButton = sortButtonTransform.gameObject;
		UIButtonTween sortPanelTween = FindPanelTween(sortButton);
		if (sortPanelTween == null || sortPanelTween.tweenTarget == null)
		{
			Debug.LogError("CWDeckFactionFilterController: Could not find the B_1_AddPanel Sort panel tween.");
			return;
		}

		GameObject sortPanel = sortPanelTween.tweenTarget;
		if (sortPanel.transform.parent != addPanelRoot.parent ||
			sortPanel.GetComponent<TweenPositionAnchored>() == null)
		{
			Debug.LogError("CWDeckFactionFilterController: The B_1_AddPanel Sort panel is invalid.");
			return;
		}

		filterPanelObject = UnityEngine.Object.Instantiate(sortPanel, sortPanel.transform.parent);
		filterPanelObject.name = "FactionFilterPanel";
		filterPanelObject.SetActive(false);
		panelMotion = filterPanelObject.GetComponent<TweenPositionAnchored>();
		RewirePanelTweens(sortPanel);

		filterButtonObject = UnityEngine.Object.Instantiate(sortButton, sortButton.transform.parent);
		filterButtonObject.name = "FactionFilterButton";
		CWDeckSortButton[] sortScripts = filterButtonObject.GetComponents<CWDeckSortButton>();
		foreach (CWDeckSortButton sortScript in sortScripts)
		{
			sortScript.enabled = false;
		}

		panelTween = FindPanelTween(filterButtonObject);
		if (panelTween == null || !PositionBesideSort(sortButton, filterButtonObject))
		{
			UnityEngine.Object.Destroy(filterButtonObject);
			filterButtonObject = null;
			return;
		}
		panelTween.tweenTarget = filterPanelObject;
		buttonLabel = filterButtonObject.GetComponentInChildren<UILabel>(true);

		ConfigurePanel();
		UpdateButtonLabel(null);
	}

	private Transform FindAddPanelRoot()
	{
		Transform current = addCardsController.transform;
		while (current != null)
		{
			if (current.name == "B_1_AddPanel")
			{
				return current;
			}

			current = current.parent;
		}

		return null;
	}

	private UIButtonTween FindPanelTween(GameObject button)
	{
		UIButtonTween[] tweens = button.GetComponents<UIButtonTween>();
		foreach (UIButtonTween tween in tweens)
		{
			if (tween.tweenTarget != null &&
				tween.tweenTarget.GetComponent<TweenPositionAnchored>() != null)
			{
				return tween;
			}
		}

		return null;
	}

	private bool PositionBesideSort(GameObject sortButton, GameObject factionButton)
	{
		SLOTUIAnchor sortAnchor = sortButton.GetComponent<SLOTUIAnchor>();
		SLOTUIAnchor factionAnchor = factionButton.GetComponent<SLOTUIAnchor>();
		if (sortAnchor != null && factionAnchor != null)
		{
			factionAnchor.screenOffset = sortAnchor.screenOffset;
			factionAnchor.screenOffset.x = FactionButtonScreenOffsetX;
			return true;
		}

		Debug.LogError("CWDeckFactionFilterController: The Sort button has no SLOTUIAnchor.");
		return false;
	}

	private void RewirePanelTweens(GameObject sourcePanel)
	{
		UIButtonTween[] tweens = filterPanelObject.GetComponentsInChildren<UIButtonTween>(true);
		foreach (UIButtonTween tween in tweens)
		{
			if (tween.tweenTarget == sourcePanel)
			{
				tween.tweenTarget = filterPanelObject;
			}
		}
	}

	private void ConfigurePanel()
	{
		Transform titleTransform = filterPanelObject.transform.Find("Buttons/Title");
		if (titleTransform != null)
		{
			UILabel titleLabel = titleTransform.GetComponentInChildren<UILabel>(true);
			if (titleLabel != null)
			{
				titleLabel.text = GetLocalizedText("!!FACTION", "Faction");
			}
		}

		ConfigureOptionButtons();
	}

	private void ConfigureOptionButtons()
	{
		Transform buttonsContainer = filterPanelObject.transform.Find("Buttons");
		if (buttonsContainer == null)
		{
			Debug.LogError("CWDeckFactionFilterController: The Sort panel has no Buttons container.");
			return;
		}

		FactionOptionData[] options = new FactionOptionData[]
		{
			new FactionOptionData(Faction.Corn, "!!FACTION_CORN", "Corn Fields"),
			new FactionOptionData(Faction.Plains, "!!FACTION_PLAINS", "Blue Plains"),
			new FactionOptionData(Faction.Swamp, "!!FACTION_SWAMP", "Useless Swamp"),
			new FactionOptionData(Faction.Cotton, "!!FACTION_COTTON", "Nice Lands"),
			new FactionOptionData(Faction.Sand, "!!FACTION_SAND", "Sandy Lands"),
			new FactionOptionData(Faction.Universal, "!!FACTION_UNIVERSAL", "Rainbow"),
			new FactionOptionData(null, "!!FILTER_ALL", "All")
		};

		List<GameObject> optionButtons = new List<GameObject>();
		for (int i = 0; i < buttonsContainer.childCount; i++)
		{
			Transform child = buttonsContainer.GetChild(i);
			if (child.name.StartsWith("SortButton_"))
			{
				optionButtons.Add(child.gameObject);
			}
			else if (child.name.StartsWith("FilterButton_"))
			{
				child.gameObject.SetActive(false);
			}
		}

		if (optionButtons.Count != options.Length)
		{
			Debug.LogError("CWDeckFactionFilterController: Expected seven Sort options but found " + optionButtons.Count + ".");
			return;
		}

		for (int i = 0; i < options.Length; i++)
		{
			SetupOptionButton(optionButtons[i], options[i]);
		}
	}

	private void SetupOptionButton(GameObject buttonObject, FactionOptionData optionData)
	{
		buttonObject.SetActive(true);

		CWDeckSortButton[] sortScripts = buttonObject.GetComponentsInChildren<CWDeckSortButton>(true);
		foreach (CWDeckSortButton sortScript in sortScripts)
		{
			sortScript.enabled = false;
		}

		UILabel label = buttonObject.GetComponentInChildren<UILabel>(true);
		if (label != null)
		{
			label.text = GetLocalizedText(optionData.LocalizationKey, optionData.DefaultName);
		}

		UIButtonTween[] optionTweens = buttonObject.GetComponentsInChildren<UIButtonTween>(true);
		foreach (UIButtonTween optionTween in optionTweens)
		{
			optionTween.enabled = false;
		}

		CWDeckFactionFilterOption optionHandler = buttonObject.GetComponent<CWDeckFactionFilterOption>();
		if (optionHandler == null)
		{
			optionHandler = buttonObject.AddComponent<CWDeckFactionFilterOption>();
		}
		optionHandler.Configure(this, optionData.FactionValue);
	}

	public void OnFactionSelected(Faction? faction)
	{
		if (addCardsController != null)
		{
			addCardsController.FactionFilter = faction;
		}

		UpdateButtonLabel(faction);
		ClosePanel();
	}

	public void ResetFilterState()
	{
		UpdateButtonLabel(null);
		ClosePanel();
	}

	private void UpdateButtonLabel(Faction? faction)
	{
		if (buttonLabel == null)
		{
			return;
		}

		string defaultFactionLabel = GetLocalizedText("!!FACTION", "Faction");
		if (faction.HasValue)
		{
			buttonLabel.text = GetFactionDisplayName(faction.Value);
		}
		else
		{
			buttonLabel.text = defaultFactionLabel;
		}
	}

	private string GetFactionDisplayName(Faction faction)
	{
		switch (faction)
		{
			case Faction.Corn:
				return GetLocalizedText("!!FACTION_CORN", "Corn");
			case Faction.Plains:
				return GetLocalizedText("!!FACTION_PLAINS", "Plains");
			case Faction.Swamp:
				return GetLocalizedText("!!FACTION_SWAMP", "Swamp");
			case Faction.Cotton:
				return GetLocalizedText("!!FACTION_COTTON", "Cotton");
			case Faction.Sand:
				return GetLocalizedText("!!FACTION_SAND", "Sand");
			case Faction.Universal:
				return GetLocalizedText("!!FACTION_UNIVERSAL", "Universal");
			default:
				return faction.ToString();
		}
	}

	private string GetLocalizedText(string key, string fallbackText)
	{
		string localized = KFFLocalization.Get(key);
		if (string.IsNullOrEmpty(localized) || localized.StartsWith("MISSING LANG"))
		{
			return fallbackText;
		}
		return localized;
	}

	public void ClosePanel()
	{
		if (filterPanelObject != null && filterPanelObject.activeInHierarchy)
		{
			if (panelMotion != null && panelTween != null)
			{
				if (panelMotion.tweenFactor > 0f)
				{
					panelTween.onFinished = OnFilterPanelClosed;
					panelTween.Play(false);
				}
				else
				{
					filterPanelObject.SetActive(false);
				}
			}
			else
			{
				filterPanelObject.SetActive(false);
			}
		}
	}

	private void OnFilterPanelClosed(UITweener tween)
	{
		if (tween == panelMotion && tween.tweenFactor <= 0f && filterPanelObject != null)
		{
			panelTween.onFinished = null;
			filterPanelObject.SetActive(false);
		}
	}

	private struct FactionOptionData
	{
		public readonly Faction? FactionValue;
		public readonly string LocalizationKey;
		public readonly string DefaultName;

		public FactionOptionData(Faction? factionValue, string localizationKey, string defaultName)
		{
			FactionValue = factionValue;
			LocalizationKey = localizationKey;
			DefaultName = defaultName;
		}
	}
}
