using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	public sealed class ToolbarButton {

		public ToolbarButton () {
			_logger = new Logger(this);
			GameEvents.onGUIApplicationLauncherReady.Add(AddApplication);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveApplication);
		}

		#region EVENTS

		public event EventHandler Open;
		public event EventHandler Close;

		#endregion

		#region METHODS (PUBLIC)

		public void Update () {
		}

		public void OnGUI () {
		}

		#endregion

		#region METHODS (PRIVATE)

		private void AddApplication () {
			_logger.Trace("AddApplication");
			if (_button != null) {
				_logger.Debug("Mod application already added");
				return;
			}

			var texture = new Texture2D(38, 38, TextureFormat.ARGB32, false);
			
			var iconStream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("ScienceChecklist.icon.png").ReadToEnd ();
			
			texture.LoadImage(iconStream);
			texture.Apply();

			_logger.Info("Adding mod application");
			_button = ApplicationLauncher.Instance.AddModApplication(
				OnToggleOn,
				OnToggleOff,
				OnHover,
				OnDeHover,
				OnEnable,
				OnDisable,
				ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPACECENTER,
				texture);
		}

		private void RemoveApplication () {
			if (_button == null) {
				_logger.Debug("Mod application already removed");
				return;
			}

			_logger.Info("Removing mod application");
			ApplicationLauncher.Instance.RemoveApplication(_button);
			_button = null;
		}

		private void OnToggleOn () {
			_logger.Trace("OnToggleOn");
			OnOpen(EventArgs.Empty);
		}

		private void OnToggleOff () {
			_logger.Trace("OnToggleOff");
			OnClose(EventArgs.Empty);
		}

		private void OnHover () {
			_logger.Trace("OnHover");
		}

		private void OnDeHover () {
			_logger.Trace("OnDeHover");
		}

		private void OnEnable () {
			_logger.Trace("OnEnable");
		}

		private void OnDisable () {
			_logger.Trace("OnDisable");
		}

		private void OnOpen (EventArgs e) {
			_logger.Trace("OnOpen");
			if (Open != null) {
				Open(this, e);
			}
		}

		private void OnClose (EventArgs e) {
			_logger.Trace("OnClose");
			if (Close != null) {
				Close(this, e);
			}
		}

		#endregion

		#region FIELDS

		private ApplicationLauncherButton _button;
		private readonly Logger _logger;

		#endregion
	}
}