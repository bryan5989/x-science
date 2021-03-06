﻿using ScienceChecklist.Buttons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	/// <summary>
	/// The main entry point into the addon. Constructed by the KSP addon loader.
	/// </summary>
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public sealed class ScienceChecklistAddon : MonoBehaviour {

		#region METHODS (PUBLIC)

		/// <summary>
		/// Called by Unity once to initialize the class.
		/// </summary>
		public void Awake () {
			if (_addonInitialized == true) {
				// For some reason the addon can be instantiated several times by the KSP addon loader (generally when going to/from the VAB),
				// even though we set onlyOnce to true in the KSPAddon attribute.
				return;
			}

			Config.Load();

			_addonInitialized = true;
			_active = false;
			_logger = new Logger(this);
			_logger.Trace("Awake");
			_window = new ScienceWindow();
			_window.Settings.UseBlizzysToolbarChanged += Settings_UseBlizzysToolbarChanged;
			
			_nextSituationUpdate = DateTime.Now;
			GameEvents.onGUIApplicationLauncherReady.Add(Load);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(Unload);

			GameEvents.onVesselWasModified.Add(x => _filterRefreshPending = true);
			GameEvents.onVesselChange.Add(x => _filterRefreshPending = true);
			GameEvents.onEditorShipModified.Add(x => _filterRefreshPending = true);

			GameEvents.onGameStateSave.Add(x => ScheduleExperimentUpdate());
			GameEvents.OnPartPurchased.Add(x => ScheduleExperimentUpdate());
			GameEvents.OnScienceChanged.Add((x, y) => ScheduleExperimentUpdate());
			GameEvents.OnScienceRecieved.Add((x, y) => ScheduleExperimentUpdate());
		}

		/// <summary>
		/// Called by Unity once to initialize the class, just before Update is called.
		/// </summary>
		public void Start () {
			_logger.Trace("Start");
			DontDestroyOnLoad(this);
		}

		/// <summary>
		/// Called by Unity when the application is destroyed.
		/// </summary>
		public void OnApplicationQuit () {
			_logger.Trace("OnApplicationQuit");
			if (_button != null) {
				_button.Remove();
				_button.Open -= Button_Open;
				_button.Close -= Button_Close;
				_button = null;
			}
		}

		/// <summary>
		/// Called by Unity when this instance is destroyed.
		/// </summary>
		public void OnDestroy () {
			if (_button != null) {
				_button.Remove();
				_button.Open -= Button_Open;
				_button.Close -= Button_Close;
				_button = null;
			}
		}

		/// <summary>
		/// Called by Unity once per frame.
		/// </summary>
		public void Update () {
			if (!_window.IsVisible) {
				return;
			}

			if (_nextSituationUpdate > DateTime.Now) {
				return;
			}

			_nextSituationUpdate = DateTime.Now.AddSeconds(0.5);
			_window.RecalculateSituation();
		}

		/// <summary>
		/// Called by Unity to draw the GUI - can be called many times per frame.
		/// </summary>
		public void OnGUI () {
			_window.Draw();
		}

		#endregion

		#region METHODS (PRIVATE)

		/// <summary>
		/// Initializes the addon if it hasn't already been loaded.
		/// </summary>
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

			InitializeButton();

			_launcherVisible = true;
			ApplicationLauncher.Instance.AddOnShowCallback(Launcher_Show);
			ApplicationLauncher.Instance.AddOnHideCallback(Launcher_Hide);

			_rndLoader = WaitForRnDAndPartLoader();
			StartCoroutine(_rndLoader);

			_experimentUpdater = UpdateExperiments();
			StartCoroutine(_experimentUpdater);

			_filterRefresher = RefreshFilter();
			StartCoroutine(_filterRefresher);
		}

		/// <summary>
		/// Unloads the addon if it has been loaded.
		/// </summary>
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

			if (_experimentUpdater != null) {
				StopCoroutine(_experimentUpdater);
			}

			if (_filterRefresher != null) {
				StopCoroutine(_filterRefresher);
			}
		}

		/// <summary>
		/// Waits for the ResearchAndDevelopment and PartLoader instances to be available.
		/// </summary>
		/// <returns>An IEnumerator that can be used to resume this method.</returns>
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
			_window.RefreshExperimentCache();
			_rndLoader = null;
		}

		/// <summary>
		/// Coroutine to throttle calls to _window.UpdateExperiments.
		/// </summary>
		/// <returns></returns>
		private IEnumerator UpdateExperiments () {
			while (true) {
				if (_window.IsVisible && _nextExperimentUpdate != null && _nextExperimentUpdate.Value < DateTime.Now) {
					_window.UpdateExperiments();
					_nextExperimentUpdate = null;
				}

				yield return 0;
			}
		}

		/// <summary>
		/// Coroutine to throttle calls to _window.RefreshFilter.
		/// </summary>
		/// <returns></returns>
		private IEnumerator RefreshFilter () {
			var nextCheck = DateTime.Now;
			while (true) {
				if (_window.IsVisible && _filterRefreshPending && DateTime.Now > nextCheck) {
					nextCheck = DateTime.Now.AddSeconds(0.5);
					_window.RefreshFilter();
					_filterRefreshPending = false;
				}

				yield return 0;
			}
		}

		/// <summary>
		/// Called when the toolbar button is toggled on.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The EventArgs of the event.</param>
		private void Button_Open (object sender, EventArgs e) {
			if (!_active) {
				return;
			}
			_logger.Trace("Button_Open");
			_buttonClicked = true;
			UpdateVisibility();
		}

		/// <summary>
		/// Called when the toolbar button is toggled off.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The EventArgs of the event.</param>
		private void Button_Close (object sender, EventArgs e) {
			if (!_active) {
				return;
			}
			_logger.Trace("Button_Close");
			_buttonClicked = false;
			UpdateVisibility();
		}

		/// <summary>
		/// Called when the KSP toolbar is shown.
		/// </summary>
		private void Launcher_Show () {
			if (!_active) {
				return;
			}
			_logger.Trace("Open");
			_launcherVisible = true;
			UpdateVisibility();
		}

		/// <summary>
		/// Called when the KSP toolbar is hidden.
		/// </summary>
		private void Launcher_Hide () {
			if (!_active) {
				return;
			}
			_logger.Trace("Close");
			_launcherVisible = false;
			UpdateVisibility();
		}

		/// <summary>
		/// Shows or hides the ScienceWindow iff the KSP toolbar is visible and the toolbar button is toggled on.
		/// </summary>
		private void UpdateVisibility () {
			if (!_active) {
				return;
			}
			_logger.Trace("UpdateVisibility");
			_window.IsVisible = _launcherVisible && _buttonClicked;
			ScheduleExperimentUpdate();
		}

		/// <summary>
		/// Schedules a full experiment update in 1 second.
		/// </summary>
		private void ScheduleExperimentUpdate () {
			_nextExperimentUpdate = DateTime.Now.AddSeconds (1);
		}

		/// <summary>
		/// Handler for the UseBlizzysToolbarChanged event on _window.Settings.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The EventArgs of the event.</param>
		private void Settings_UseBlizzysToolbarChanged (object sender, EventArgs e) {
			InitializeButton();
		}

		/// <summary>
		/// Initializes the toolbar button.
		/// </summary>
		private void InitializeButton () {
			if (_button != null) {
				_button.Open -= Button_Open;
				_button.Close -= Button_Close;
				_button.Remove();
				_button = null;
			}

			if (Config.UseBlizzysToolbar && BlizzysToolbarButton.IsAvailable) {
				_button = new BlizzysToolbarButton();
			} else {
				_button = new AppLauncherButton();
			}
			_button.Open += Button_Open;
			_button.Close += Button_Close;
			_button.Add();
		}

		#endregion

		#region FIELDS

		private DateTime _nextSituationUpdate;
		private bool _active;
		private Logger _logger;
		private IToolbarButton _button;
		private bool _launcherVisible;
		private bool _buttonClicked;
		private ScienceWindow _window;
		private IEnumerator _rndLoader;

		private DateTime? _nextExperimentUpdate;
		private IEnumerator _experimentUpdater;
		private bool _filterRefreshPending;
		private IEnumerator _filterRefresher;

		private static bool _addonInitialized;

		#endregion
	}
}
