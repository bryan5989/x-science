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
			if (_addonInitialized == true) {
				// For some reason the addon can be instantiated several times by the KSP addon loader (generally when going to/from the VAB),
				// even though we set onlyOnce to true in the KSPAddon attribute.
				return;
			}

			_addonInitialized = true;
			_active = false;
			_logger = new Logger(this);
			_logger.Trace("Awake");
			_window = new ScienceWindow();
			_button = new ToolbarButton();
			_button.Open += Button_Open;
			_button.Close += Button_Close;
			_nextSituationUpdate = DateTime.Now;
			GameEvents.onGUIApplicationLauncherReady.Add(Load);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(Unload);
			GameEvents.OnScienceRecieved.Add((x, y) => OnScienceReceived());
			GameEvents.onVesselWasModified.Add(x => OnPartsChanged());
			GameEvents.onVesselChange.Add(x => OnPartsChanged());
			GameEvents.onEditorShipModified.Add(x => OnPartsChanged());
			GameEvents.onVesselLoaded.Add(x => OnScienceReceived());
			GameEvents.onGameStateSave.Add(x => OnScienceReceived());
			GameEvents.OnPartPurchased.Add(x => OnScienceReceived());
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
			if (!_active) {
				return;
			}

			if (_nextSituationUpdate > DateTime.Now) {
				return;
			}

			_nextSituationUpdate = DateTime.Now.AddSeconds(0.5);
			_window.RecalculateSituation();
		}

		public void OnGUI () {
			_window.Draw();
		}

		#endregion

		#region METHODS (PRIVATE)

		private void Load () {
			_logger.Trace("Load");
			if (_active) {
				_logger.Info("Already loaded.");
				_rndLoader = WaitForRnDAndPartLoader();
				StartCoroutine(_rndLoader);
				return;
			}
			if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX) {
				_logger.Info("Game type is " + HighLogic.CurrentGame.Mode + ". Deactivating.");
				_active = false;
				return;
			}

			_logger.Info("Game type is " + HighLogic.CurrentGame.Mode + ". Activating.");
			_active = true;

			_button.Add();

			_launcherVisible = true;
			ApplicationLauncher.Instance.AddOnShowCallback(Launcher_Show);
			ApplicationLauncher.Instance.AddOnHideCallback(Launcher_Hide);

			_rndLoader = WaitForRnDAndPartLoader();
			StartCoroutine(_rndLoader);
		}

		private void Unload () {
			_logger.Trace("Unload");
			if (!_active) {
				_logger.Info("Already unloaded.");
				return;
			}
			_active = false;

			_button.Remove();
			
			ApplicationLauncher.Instance.RemoveOnShowCallback(Launcher_Show);
			ApplicationLauncher.Instance.RemoveOnHideCallback(Launcher_Hide);
			_launcherVisible = false;

			if (_rndLoader != null) {
				StopCoroutine(_rndLoader);
			}
		}

		private void OnScienceReceived () {
			if (!_active) {
				return;
			}
			_logger.Trace("OnScienceReceived");
			_window.RefreshScience();
		}

		private void OnPartsChanged () {
			if (!_active) {
				return;
			}
			_logger.Trace("OnPartsChanged");
			_window.RefreshFilter();
		}

		private IEnumerator WaitForRnDAndPartLoader () {
			if (!_active) {
				yield break;
			}

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
			if (!_active) {
				return;
			}
			_logger.Trace("Button_Open");
			_buttonClicked = true;
			UpdateVisibility();
		}

		private void Button_Close (object sender, EventArgs e) {
			if (!_active) {
				return;
			}
			_logger.Trace("Button_Close");
			_buttonClicked = false;
			UpdateVisibility();
		}

		private void Launcher_Show () {
			if (!_active) {
				return;
			}
			_logger.Trace("Open");
			_launcherVisible = true;
			UpdateVisibility();
		}

		private void Launcher_Hide () {
			if (!_active) {
				return;
			}
			_logger.Trace("Close");
			_launcherVisible = false;
			UpdateVisibility();
		}

		private void UpdateVisibility () {
			if (!_active) {
				return;
			}
			_logger.Trace("UpdateVisibility");
			_window.IsVisible = _launcherVisible && _buttonClicked;
		}

		#endregion

		#region FIELDS

		private DateTime _nextSituationUpdate;
		private bool _active;
		private Logger _logger;
		private ToolbarButton _button;
		private bool _launcherVisible;
		private bool _buttonClicked;
		private ScienceWindow _window;
		private IEnumerator _rndLoader;

		private static bool _addonInitialized;

		#endregion
	}
}
