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
		}

		#region EVENTS

		public event EventHandler Open;
		public event EventHandler Close;

		#endregion

		#region METHODS (PUBLIC)
		
		public void Add () {
			_logger.Trace("Add");
			if (_button != null) {
				_logger.Debug("Button already added");
				return;
			}

			var texture = new Texture2D(38, 38, TextureFormat.ARGB32, false);
			
			var iconStream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("ScienceChecklist.icon.png").ReadToEnd ();
			
			texture.LoadImage(iconStream);
			texture.Apply();
			
			_logger.Info("Adding button");
			_button = ApplicationLauncher.Instance.AddModApplication(
				OnToggleOn,
				OnToggleOff,
				null,
				null,
				null,
				null,
				ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPACECENTER,
				texture);
		}

		public void Remove () {
			if (_button == null) {
				_logger.Debug("Button already removed");
				return;
			}

			_logger.Info("Removing button");
			ApplicationLauncher.Instance.RemoveApplication(_button);
			_button = null;
		}

		#endregion

		#region METHODS (PRIVATE)

		private void OnToggleOn () {
			_logger.Trace("OnToggleOn");
			OnOpen(EventArgs.Empty);
		}

		private void OnToggleOff () {
			_logger.Trace("OnToggleOff");
			OnClose(EventArgs.Empty);
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