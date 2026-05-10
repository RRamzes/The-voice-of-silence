using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimUI.ModernMenu{
	public class ResetDemo : MonoBehaviour {

		void OnGUI() {
			Event e = Event.current;
			if (e != null && e.type == EventType.KeyDown && e.keyCode == KeyCode.R) {
				SceneManager.LoadScene(0);
			}
		}
	}
}
