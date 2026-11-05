using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeRedemptionScript : MonoBehaviour
{
	private const string PigCode = "P1GGY1";

	private const string CerebralCode = "C3R3BL";

	private const string BardCode = "MINSTR3L";

	private const string PajamaFinnCode = "UND3RPANT5";

	private const string BriefPowerCode = "BRIEF";

	private const string DocFinnCode = "DRKUNGFU";

	private const string JakeGhostCode = "GHOST DOG";

	private const string XmasBmoCode = "WINTER BMO";

	private const string LSPSweaterCode = "LUMPS";

	private const string SnuggleTreeCode = "CUTIE PIE";

	private const string MusicMallardCode = "123224";

	private const string SpikedMaceStumpCode = "NICE ONE";

	private const string FreezyJCode = "COOLDUDE";

	private const string AwesomatudeCode = "AWESOMATUDE";

	private const string MeMowCode = "ME-MOW";

	private const string GhostNinjaCode = "GHOSTNINJA";

	public UILabel Label;

	public AudioClip fanfare;

	public AudioClip success;

	public AudioClip failure;

	public AudioClip pig;

	public AudioClip cerebral;

	public UIButtonTween ShowSuccess;

	public UIButtonTween Showfail;

	public UIButtonTween ShowDupe;

	public UILabel CreatureName;

	private Dictionary<string, Func<bool>> redemptions;

	private Dictionary<string, Func<bool>> Redemptions
	{
		get
		{
			if (redemptions == null)
			{
				redemptions = BuildRedemptions();
			}
			return redemptions;
		}
	}

	private Dictionary<string, Func<bool>> BuildRedemptions()
	{
		Dictionary<string, Func<bool>> map = new Dictionary<string, Func<bool>>();

		// cards (creatures / spells)
		map.Add(PigCode, CardRedemption(PlayerInfoScript.Tag.GotCreaturePig, "Creature_Pig"));
		map.Add(SnuggleTreeCode, CardRedemption(PlayerInfoScript.Tag.GotCreatureSnuggleTree, "Creature_SnuggleTree"));
		map.Add(BardCode, CardRedemption(PlayerInfoScript.Tag.GotCreatureEmbarassingBard, "Creature_EmbarrassingBard"));
		map.Add(CerebralCode, CardRedemption(PlayerInfoScript.Tag.GotSpellCerebral, "Spell_CerebralBloodstorm"));
		map.Add(BriefPowerCode, CardRedemption(PlayerInfoScript.Tag.GotSpellBriefPower, "Spell_BriefPower"));
		map.Add(MusicMallardCode, CardRedemption(PlayerInfoScript.Tag.GotCreatureMusicMallard, "Creature_MusicMallard"));
		map.Add(SpikedMaceStumpCode, CardRedemption(PlayerInfoScript.Tag.GotSpikedMaceStump, "Creature_SpikedMaceStump"));
		map.Add(FreezyJCode, CardRedemption(PlayerInfoScript.Tag.GotFreezyJ, "Creature_FreezyJ"));
		// creatures that are already in the game, but have a code for a free copy
		map.Add(AwesomatudeCode, CardRedemption(PlayerInfoScript.Tag.GotAwesomatude, "Creature_SpikedMaceStump"));
		map.Add(MeMowCode, CardRedemption(PlayerInfoScript.Tag.GotMeMow, "Creature_FreezyJ"));
		map.Add(GhostNinjaCode, CardRedemption(PlayerInfoScript.Tag.GotCreatureGhostNinja, "Creature_GL_GhostNinja"));

		// Leaders
		map.Add(XmasBmoCode, LeaderRedemption("Leader_BMOSweater"));
		map.Add(JakeGhostCode, SeasonalLeaderRedemption("halloween", "Leader_JakeGhost"));
		map.Add(LSPSweaterCode, SeasonalLeaderRedemption("christmas", "Leader_LumpySweater"));
		//  Singleton<CodeRedemptionManager>.Instance.GetCurrentScheme() != null
		map.Add(PajamaFinnCode, LeaderRedemption("Leader_FinnPajama"));
		map.Add(DocFinnCode, LeaderRedemption("Leader_FinnDoctor"));

		return map;
	}

	private Func<bool> CardRedemption(PlayerInfoScript.Tag tag, string cardName)
	{
		return delegate
		{
			DoCardRedeem(tag, cardName);
			return true;
		};
	}

	private Func<bool> LeaderRedemption(string leaderName)
	{
		return delegate
		{
			DoLeaderRedeem(leaderName);
			return true;
		};
	}

	private Func<bool> SeasonalLeaderRedemption(string season, string leaderName)
	{
		return delegate
		{
			if (!IsPremiumChestAvailable(season))
			{
				return false;
			}
			DoLeaderRedeem(leaderName);
			return true;
		};
	}

	private static bool IsPremiumChestAvailable(string season)
	{
		return ScheduleDataManager.Instance.IsItemAvailableAndUnlocked("chest_premium", season, TFUtils.ServerTime.Ticks);
	}

	public void RedeemCode(string input)
	{
		Func<bool> redemption;
		if (Redemptions.TryGetValue(input.ToUpper(), out redemption) && redemption())
		{
			return;
		}
		PlayTween(Showfail);
		NGUITools.PlaySound(failure);
	}

	private void DoCardRedeem(PlayerInfoScript.Tag tag, string cardName)
	{
		PlayerInfoScript instance = PlayerInfoScript.GetInstance();
		if (!instance.HasTag(tag))
		{
			NGUITools.PlaySound(fanfare);
			CardItem cardItem = new CardItem(CardDataManager.Instance.GetCard(cardName));
			if (cardItem != null)
			{
				instance.DeckManager.AddCard(cardItem);
				instance.SetTag(tag);
				ShowRewardFeedback(cardItem.Form.Name);
			}
			instance.Save();
		}
		else
		{
			PlayTween(ShowDupe);
			NGUITools.PlaySound(success);
		}
	}

	private void DoLeaderRedeem(string leaderName)
	{
		PlayerInfoScript instance = PlayerInfoScript.GetInstance();
		NGUITools.PlaySound(fanfare);
		LeaderItem leaderItem = LeaderManager.Instance.AddNewLeaderIfUnique(leaderName);
		if (leaderItem != null)
		{
			instance.Save();
			ShowRewardFeedback(leaderItem.Form.Name);
		}
		else
		{
			PlayTween(ShowDupe);
		}
	}

	// Feedback to the user about the reward they just got 
	private void ShowRewardFeedback(string rewardName)
	{
		if (CreatureName != null)
		{
			CreatureName.text = rewardName;
		}
		PlayTween(ShowSuccess);
		StartCoroutine(CardRewardSfxCoroutine(cerebral));
	}

	private static void PlayTween(UIButtonTween tween)
	{
		if (tween != null)
		{
			tween.Play(true);
		}
	}

	protected IEnumerator CardRewardSfxCoroutine(AudioClip clip)
	{
		yield return new WaitForSeconds(1.5f);
		NGUITools.PlaySound(clip);
	}

	private void OnClick()
	{
		if (Label != null)
		{
			string text = Label.text;
			RedeemCode(text);
			Label.text = string.Empty;
		}
	}
}