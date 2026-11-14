using UnityEngine;

public class CWDeckFactionFilterOption : MonoBehaviour
{
	private CWDeckFactionFilterController filterController;

	private Faction? factionValue;

	public void Configure(CWDeckFactionFilterController controller, Faction? faction)
	{
		filterController = controller;
		factionValue = faction;
	}

	public void OnClick()
	{
		if (filterController != null)
		{
			filterController.OnFactionSelected(factionValue);
		}
	}
}
