using UnityEngine;

namespace SlimUI.ModernMenu{
	[ExecuteInEditMode()]
	[System.Serializable]
	public class ThemedUI : MonoBehaviour {

		public ThemedUIData themeController;

		protected virtual void OnSkinUI(){

		}

		public virtual void Awake(){
			OnSkinUI();
		}

		#if UNITY_EDITOR
		public virtual void OnValidate(){
			OnSkinUI();
		}
		#endif
	}
}
