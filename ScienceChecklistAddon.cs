using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public sealed class ScienceChecklistAddon : MonoBehaviour {

		#region METHODS (PUBLIC)

		public void Awake () {
			_logger = new Logger(this);
			_logger.Trace("Awake");
			_window = new ScienceWindow();
			_button = new ToolbarButton();
			_button.Open += Button_Open;
			_button.Close += Button_Close;
			GameEvents.onGUIApplicationLauncherReady.Add(Load);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(Unload);
			GameEvents.OnScienceRecieved.Add(OnScienceReceived);
			GameEvents.onVesselWasModified.Add(x => OnPartsChanged());
			GameEvents.onVesselChange.Add(x => OnPartsChanged());
			GameEvents.onEditorShipModified.Add(x => OnPartsChanged());
		}

		public void Start () {
			_logger.Trace("Start");
			DontDestroyOnLoad(this);
		}

		public void OnApplicationQuit () {
			_logger.Trace("OnApplicationQuit");
			_button.Open -= Button_Open;
			_button.Close -= Button_Close;
		}

		public void Update () {
		}

		public void OnGUI () {
			_window.Draw();
		}

		#endregion

		#region METHODS (PRIVATE)

		private void Load () {
			_button.Add();

			_launcherVisible = true;
			ApplicationLauncher.Instance.AddOnShowCallback(Launcher_Show);
			ApplicationLauncher.Instance.AddOnHideCallback(Launcher_Hide);

			_rndLoader = WaitForRnDAndPartLoader();
			StartCoroutine(_rndLoader);
		}

		private void Unload () {
			_button.Remove();
			
			ApplicationLauncher.Instance.RemoveOnShowCallback(Launcher_Show);
			ApplicationLauncher.Instance.RemoveOnHideCallback(Launcher_Hide);
			_launcherVisible = false;

			if (_rndLoader != null) {
				StopCoroutine(_rndLoader);
			}
		}

		private void OnScienceReceived (float scienceAmount, ScienceSubject subject) {
			_window.RefreshScience();
		}

		private void OnPartsChanged () {
			_logger.Trace("OnPartsChanged");
			_window.RefreshFilter();
		}

		private IEnumerator WaitForRnDAndPartLoader () {
			while (ResearchAndDevelopment.Instance == null) {
				yield return 0;
			}

			_logger.Info("Science ready");

			while (PartLoader.Instance == null) {
				yield return 0;
			}

			_logger.Info("PartLoader ready");
			_window.RefreshScience();
			_rndLoader = null;
		}

		private void Button_Open (object sender, EventArgs e) {
			_logger.Trace("Button_Open");
			_buttonClicked = true;
			UpdateVisibility();
		}

		private void Button_Close (object sender, EventArgs e) {
			_logger.Trace("Button_Close");
			_buttonClicked = false;
			UpdateVisibility();
		}

		private void Launcher_Show () {
			_logger.Trace("Open");
			_launcherVisible = true;
			UpdateVisibility();
		}

		private void Launcher_Hide () {
			_logger.Trace("Close");
			_launcherVisible = false;
			UpdateVisibility();
		}

		private void UpdateVisibility () {
			_logger.Trace("UpdateVisibility");
			_window.IsVisible = _launcherVisible && _buttonClicked;
		}

		#endregion

		#region FIELDS

		private Logger _logger;
		private ToolbarButton _button;
		private bool _launcherVisible;
		private bool _buttonClicked;
		private ScienceWindow _window;
		private IEnumerator _rndLoader;

		#endregion
	}
}
