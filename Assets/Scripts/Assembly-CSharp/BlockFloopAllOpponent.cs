using System.Collections.Generic;

public class BlockFloopAllOpponent : CreatureScript
{
	public override bool CanFloop()
	{
		List<Lane> lanes = GameState.Instance.GetLanes(!base.Owner);
		foreach (Lane item in lanes)
		{
			if (item.HasCreature())
			{
				CreatureScript creature = item.GetCreature();
				if (!creature.FloopBlocked)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override int EvaluateAbility()
	{
		int num = base.GameInstance.ScoreBoard();
		int num2 = 0;
		List<Lane> lanes = GameState.Instance.GetLanes(!base.Owner);
		foreach (Lane item in lanes)
		{
			if (item.HasCreature())
			{
				CreatureScript creature = item.GetCreature();
				if (!creature.FloopBlocked && creature.CanFloop())
				{
					num2 += creature.EvaluateAbility() - num;
				}
			}
		}
		return num + num2;
	}

	public override void Floop()
	{
		List<Lane> lanes = base.GameInstance.GetLanes(!base.Owner);
		foreach (Lane item in lanes)
		{
			if (item.HasCreature())
			{
				CreatureScript creature = item.GetCreature();
				if (!creature.FloopBlocked)
				{
					TargetList.Add(creature);
				}
			}
		}
		DoEffect();
	}

	public override bool DoResult(CardScript target)
	{
		CreatureScript creatureScript = target as CreatureScript;
		if (creatureScript != null)
		{
			creatureScript.FloopBlocked = true;
		}
		return true;
	}
}

