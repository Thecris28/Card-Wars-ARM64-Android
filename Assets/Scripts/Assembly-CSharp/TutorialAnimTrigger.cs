using UnityEngine;

public class TutorialAnimTrigger : MonoBehaviour
{
	public TutorialTrigger Trigger;

	public bool triggerOnStart;

	private void Start()
	{
		if (triggerOnStart)
		{
			TriggerTutorial();
		}
	}

	private void TriggerTutorial()
	{
		if (!base.gameObject.activeInHierarchy || !base.enabled)
		{
			return;
		}

		TutorialMonitor tutorialMonitor = TutorialMonitor.Instance;
		if (tutorialMonitor != null)
		{
			tutorialMonitor.TriggerTutorial(Trigger);
		}
	}

	public bool WillTriggerTutorial()
	{
		if (!base.gameObject.activeInHierarchy || !base.enabled)
		{
			return false;
		}

		TutorialMonitor tutorialMonitor = TutorialMonitor.Instance;
		return tutorialMonitor != null && tutorialMonitor.ShouldTriggerTutorial(Trigger);
	}
}
